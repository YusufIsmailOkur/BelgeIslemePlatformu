using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Services;
using StoreApp.Services.Abstractions;
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
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser });

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.WaitingValidation, updated!.Status);
            Assert.Null(updated.ProcessingError);

            var content = await db.DocumentContents.SingleAsync(c => c.DocumentId == document.Id);
            Assert.Equal(DocumentSourceFormat.Csv, content.SourceFormat);
            Assert.Equal(1, content.RowCount);
            Assert.NotNull(content.RawTablesJson);
        }

        [Fact]
        public async Task ProcessAsync_WhenParserThrows_TransitionsToFailedRetryWithError()
        {
            using var db = CreateDb();
            var document = CreateDocument(db);

            var parser = new FakeDocumentContentParser(canParse: true, exception: new InvalidDataException("Bozuk dosya"));

            var service = new DocumentProcessingService(
                db, new FakeFileStorageService(), new FakeAuditLogService(), new[] { parser });

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
                db, new FakeFileStorageService(), new FakeAuditLogService(), Array.Empty<IDocumentContentParser>());

            await service.ProcessAsync(document.Id);

            var updated = await db.Documents.FindAsync(document.Id);
            Assert.Equal(DocumentStatus.FailedRetry, updated!.Status);
            Assert.Contains("Desteklenmeyen", updated.ProcessingError);
        }
    }
}
