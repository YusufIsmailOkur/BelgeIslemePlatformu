using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public class DocumentProcessingServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Document CreateDocument(AppDbContext db)
        {
            var document = new Document
            {
                OriginalFileName = "test.csv",
                StoragePath = "2026/09/02/test.csv",
                MimeType = "text/csv",
                FileSizeBytes = 10,
                DocumentType = DocumentType.Other,
                Status = DocumentStatus.Uploaded,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Documents.Add(document);
            db.SaveChanges();
            return document;
        }

        private static IConfiguration CreateConfiguration(int ocrTimeoutSeconds = 30) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ocr:TimeoutSeconds"] = ocrTimeoutSeconds.ToString()
                })
                .Build();

        [Fact]
        public async Task ProcessAsync_OnSuccess_TransitionsToWaitingValidationAndStoresContent()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parsedTable = new ParsedTable("csv", new[] { "A" }, new List<IReadOnlyList<string>> { new[] { "1" } });
            var parser = new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(DocumentSourceFormat.Csv, null, new[] { parsedTable }, null, null, 1, "utf-8", ","));

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser },
                new FakePdfPageRenderer(), new FakeOcrService(), new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.WaitingValidation, updated!.Status);
            Assert.Null(updated.ProcessingError);

            var content = await db.DocumentContents.SingleAsync(c => c.DocumentId == document.Id);
            Assert.Equal(DocumentSourceFormat.Csv, content.SourceFormat);
            Assert.Equal(1, content.RowCount);
            Assert.NotNull(content.RawTablesJson);
            Assert.False(content.IsOcrProcessed);
        }

        [Fact]
        public async Task ProcessAsync_StoresDocumentTypeSuggestion_FromParsedContent()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(
                    DocumentSourceFormat.Pdf, "Fatura No: 2026-001\nKDV Oranı: %20", Array.Empty<ParsedTable>(),
                    1, null, null, null, null));

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser },
                new FakePdfPageRenderer(), new FakeOcrService(), new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            var content = await db.DocumentContents.SingleAsync(c => c.DocumentId == document.Id);
            Assert.Equal(DocumentType.Invoice, content.SuggestedDocumentType);
            Assert.True(content.DocumentTypeConfidence > 0);
        }

        [Fact]
        public async Task ProcessAsync_WhenParserThrows_TransitionsToFailedRetryWithError()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(canParse: true, exception: new InvalidDataException("Bozuk dosya"));

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser },
                new FakePdfPageRenderer(), new FakeOcrService(), new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.FailedRetry, updated!.Status);
            Assert.Equal("Bozuk dosya", updated.ProcessingError);
        }

        [Fact]
        public async Task ProcessAsync_WhenNoParserMatches_TransitionsToFailedRetry()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), Array.Empty<IDocumentContentParser>(),
                new FakePdfPageRenderer(), new FakeOcrService(), new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.FailedRetry, updated!.Status);
            Assert.Contains("Desteklenmeyen", updated.ProcessingError);
        }

        [Fact]
        public async Task ProcessAsync_WhenPdfHasNoExtractableText_RoutesToOcrAndStoresOcrText()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(DocumentSourceFormat.Pdf, "   ", Array.Empty<ParsedTable>(), 2, null, null, null, null));

            var renderer = new FakePdfPageRenderer(
                new PdfPageImage(1, new byte[] { 1 }),
                new PdfPageImage(2, new byte[] { 2 }));

            var ocrService = new FakeOcrService(page => page.PageNumber == 1
                ? new OcrPageResult(1, "sayfa 1", 0.9)
                : new OcrPageResult(2, "sayfa 2", 0.8));

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser }, renderer, ocrService,
                new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            Assert.Equal(2, ocrService.CalledWith.Count);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.WaitingValidation, updated!.Status);

            var content = await db.DocumentContents.SingleAsync(c => c.DocumentId == document.Id);
            Assert.True(content.IsOcrProcessed);
            Assert.Equal(0.85, content.OcrConfidence!.Value, precision: 10);
            Assert.Equal("sayfa 1\nsayfa 2", content.RawText);

            Assert.NotNull(content.PageResultsJson);
            var pageResultsFromJson = JsonSerializer.Deserialize<List<OcrPageResult>>(content.PageResultsJson!);
            Assert.Equal(2, pageResultsFromJson!.Count);
            Assert.Equal("sayfa 1", pageResultsFromJson[0].Text);
            Assert.Equal("sayfa 2", pageResultsFromJson[1].Text);
        }

        [Fact]
        public async Task ProcessAsync_WhenPdfHasExtractableText_DoesNotCallOcr()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(DocumentSourceFormat.Pdf, "dijital metin", Array.Empty<ParsedTable>(), 1, null, null, null, null));

            var ocrService = new FakeOcrService();

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser },
                new FakePdfPageRenderer(), ocrService, new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            Assert.False(ocrService.WasCalled);

            var content = await db.DocumentContents.SingleAsync(c => c.DocumentId == document.Id);
            Assert.False(content.IsOcrProcessed);
            Assert.Equal("dijital metin", content.RawText);
            Assert.Null(content.PageResultsJson);
        }

        [Fact]
        public async Task ProcessAsync_WhenOcrThrows_TransitionsToFailedRetry()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(DocumentSourceFormat.Pdf, "", Array.Empty<ParsedTable>(), 1, null, null, null, null));

            var renderer = new FakePdfPageRenderer(new PdfPageImage(1, new byte[] { 1 }));
            var ocrService = new FakeOcrService(exception: new InvalidOperationException("OCR servis hatası"));

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser }, renderer, ocrService,
                new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration());

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.FailedRetry, updated!.Status);
            Assert.Equal("OCR servis hatası", updated.ProcessingError);
        }

        [Fact]
        public async Task ProcessAsync_WhenOcrTimesOut_TransitionsToFailedRetryWithTimeoutMessage()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(
                canParse: true,
                result: new ParsedDocumentContent(DocumentSourceFormat.Pdf, "", Array.Empty<ParsedTable>(), 1, null, null, null, null));

            var renderer = new FakePdfPageRenderer(new PdfPageImage(1, new byte[] { 1 }));
            var ocrService = new FakeOcrService(hangsUntilCancelled: true);

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser }, renderer, ocrService,
                new KeywordDocumentTypeClassifier(), new RuleBasedFieldExtractor(), CreateConfiguration(ocrTimeoutSeconds: 0));

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.FailedRetry, updated!.Status);
            Assert.Contains("zaman aşımı", updated.ProcessingError, StringComparison.OrdinalIgnoreCase);
        }
    }
}
