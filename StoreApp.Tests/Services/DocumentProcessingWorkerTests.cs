using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Services;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Classification;
using StoreApp.Services.Extraction;
using StoreApp.Tests.Fakes;

namespace StoreApp.Tests.Services
{
    public class DocumentProcessingWorkerTests
    {
        private static ServiceProvider BuildProvider(string dbName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
            services.AddSingleton<IFileStorageService, FakeFileStorageService>();
            services.AddSingleton<IAuditLogService, FakeAuditLogService>();
            services.AddSingleton<IDocumentContentParser>(new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(DocumentSourceFormat.Csv, "metin", Array.Empty<ParsedTable>(), null, null, null, null, null)));
            services.AddSingleton<IPdfPageRenderer>(new FakePdfPageRenderer());
            services.AddSingleton<IOcrService, FakeOcrService>();
            services.AddSingleton<IDocumentTypeClassifier, KeywordDocumentTypeClassifier>();
            services.AddSingleton<IRuleBasedFieldExtractor, RuleBasedFieldExtractor>();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
            services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();

            return services.BuildServiceProvider();
        }

        private static async Task<int> InsertDocumentAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var document = new Document
            {
                OriginalFileName = "test.csv",
                StoragePath = "irrelevant.csv",
                MimeType = "text/csv",
                FileSizeBytes = 1,
                DocumentType = DocumentType.Other,
                Status = DocumentStatus.Uploaded,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Documents.Add(document);
            await db.SaveChangesAsync();
            return document.Id;
        }

        private static async Task<Document?> WaitForStatusAsync(
            ServiceProvider provider, int documentId, DocumentStatus targetStatus, int maxAttempts = 100)
        {
            Document? current = null;
            for (var attempt = 0; attempt < maxAttempts && current?.Status != targetStatus; attempt++)
            {
                await Task.Delay(20);
                await using var checkScope = provider.CreateAsyncScope();
                var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
                current = await checkDb.Documents.FindAsync(documentId);
            }

            return current;
        }

        [Fact]
        public async Task Worker_ProcessesDocumentsQueuedWhileRunning()
        {
            await using var provider = BuildProvider(Guid.NewGuid().ToString());
            var documentId = await InsertDocumentAsync(provider);

            var queue = provider.GetRequiredService<IDocumentProcessingQueue>();
            var logger = new CapturingLogger<DocumentProcessingWorker>();
            var worker = new DocumentProcessingWorker(
                queue, provider.GetRequiredService<IServiceScopeFactory>(), logger);

            await worker.StartAsync(CancellationToken.None);
            try
            {
                queue.QueueDocumentProcessing(documentId);

                var current = await WaitForStatusAsync(provider, documentId, DocumentStatus.WaitingValidation);

                Assert.True(
                    current?.Status == DocumentStatus.WaitingValidation,
                    $"Status: {current?.Status}, Error: {current?.ProcessingError}, Logs: {string.Join(" | ", logger.Messages)}");
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public async Task Worker_WhenADocumentFailsUnexpectedly_StillProcessesLaterQueuedDocuments()
        {
            await using var provider = BuildProvider(Guid.NewGuid().ToString());
            var validDocumentId = await InsertDocumentAsync(provider);
            const int missingDocumentId = -1;

            var queue = provider.GetRequiredService<IDocumentProcessingQueue>();
            var logger = new CapturingLogger<DocumentProcessingWorker>();
            var worker = new DocumentProcessingWorker(
                queue, provider.GetRequiredService<IServiceScopeFactory>(), logger);

            await worker.StartAsync(CancellationToken.None);
            try
            {
                // "Belge bulunamadı" hatası DocumentProcessingService.ProcessAsync'in kendi
                // try/catch'inin DIŞINDA fırlatılır; worker'ın bunu yutup kuyruğa devam etmesi
                // gerekir (bkz. DocumentProcessingWorker.ExecuteAsync).
                queue.QueueDocumentProcessing(missingDocumentId);
                queue.QueueDocumentProcessing(validDocumentId);

                var current = await WaitForStatusAsync(provider, validDocumentId, DocumentStatus.WaitingValidation);

                Assert.True(
                    current?.Status == DocumentStatus.WaitingValidation,
                    $"Status: {current?.Status}, Error: {current?.ProcessingError}, Logs: {string.Join(" | ", logger.Messages)}");
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }
    }
}
