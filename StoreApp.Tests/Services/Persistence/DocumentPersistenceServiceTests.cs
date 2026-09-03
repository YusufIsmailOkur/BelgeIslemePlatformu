using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Persistence;

namespace StoreApp.Tests.Services.Persistence
{
    public class DocumentPersistenceServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Document CreateDocument(AppDbContext db, ExtractionResult? extraction)
        {
            var document = new Document
            {
                OriginalFileName = "test.pdf",
                StoragePath = "2026/09/03/test.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 10,
                DocumentType = DocumentType.Other,
                Status = DocumentStatus.WaitingValidation,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Content = new DocumentContent
                {
                    SourceFormat = DocumentSourceFormat.Pdf,
                    ParsedAt = DateTime.UtcNow,
                    ExtractedFieldsJson = extraction is null
                        ? null
                        : System.Text.Json.JsonSerializer.Serialize(extraction)
                }
            };
            db.Documents.Add(document);
            db.SaveChanges();
            return document;
        }

        private static ExtractedField Field(string key, string value, string source = "Rule", double confidence = 1d) =>
            new(key, value, confidence, source);

        [Fact]
        public async Task PersistApprovedDocumentAsync_StoresHeaderFieldsAndTransitionsToSavedToSql()
        {
            using var db = CreateDb();
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>
                {
                    ["document_number"] = Field("document_number", "F-001")
                },
                new List<ExtractedLineItem>());
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            Assert.Equal(DocumentStatus.SavedToSql, document.Status);
            var storedField = await db.DocumentFields.SingleAsync(f => f.DocumentId == document.Id);
            Assert.Equal("document_number", storedField.FieldKey);
            Assert.Equal("F-001", storedField.Value);
            Assert.Null(storedField.LineItemId);
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_StoresLineItemsWithFields()
        {
            using var db = CreateDb();
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>(),
                new List<ExtractedLineItem>
                {
                    new(new Dictionary<string, ExtractedField>
                    {
                        ["item_description"] = Field("item_description", "Kağıt"),
                        ["quantity"] = Field("quantity", "5")
                    })
                });
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            var lineItem = await db.DocumentLineItems
                .Include(li => li.Fields)
                .SingleAsync(li => li.DocumentId == document.Id);
            Assert.Equal(1, lineItem.LineNumber);
            Assert.Equal(2, lineItem.Fields.Count);
            Assert.Contains(lineItem.Fields, f => f.FieldKey == "quantity" && f.Value == "5");
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_ResolvesCustomerFromCustomerNameField()
        {
            using var db = CreateDb();
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>
                {
                    ["customer_name"] = Field("customer_name", "Acme A.Ş.")
                },
                new List<ExtractedLineItem>());
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            Assert.NotNull(document.CustomerId);
            var customer = await db.Customers.SingleAsync();
            Assert.Equal("Acme A.Ş.", customer.Name);
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_FallsBackToCompanyNameWhenCustomerNameMissing()
        {
            using var db = CreateDb();
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>
                {
                    ["company_name"] = Field("company_name", "Beta Ltd.")
                },
                new List<ExtractedLineItem>());
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            var customer = await db.Customers.SingleAsync();
            Assert.Equal("Beta Ltd.", customer.Name);
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_ReusesExistingCustomerByNormalizedName()
        {
            using var db = CreateDb();
            db.Customers.Add(new Customer { Name = "ACME", NormalizedName = "acme", CreatedAt = DateTime.UtcNow });
            db.SaveChanges();

            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>
                {
                    ["customer_name"] = Field("customer_name", "Acme")
                },
                new List<ExtractedLineItem>());
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            Assert.Equal(1, await db.Customers.CountAsync());
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_TwoLineItemsWithSameProduct_CreateSingleProductRow()
        {
            using var db = CreateDb();
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>(),
                new List<ExtractedLineItem>
                {
                    new(new Dictionary<string, ExtractedField>
                    {
                        ["item_description"] = Field("item_description", "Kağıt")
                    }),
                    new(new Dictionary<string, ExtractedField>
                    {
                        ["item_description"] = Field("item_description", "Kağıt")
                    })
                });
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            Assert.Equal(1, await db.Products.CountAsync());
            var lineItems = await db.DocumentLineItems.Where(li => li.DocumentId == document.Id).ToListAsync();
            Assert.Equal(2, lineItems.Count);
            Assert.All(lineItems, li => Assert.NotNull(li.ProductId));
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_CalledTwice_ReplacesPreviousFieldsInsteadOfDuplicating()
        {
            using var db = CreateDb();
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField>
                {
                    ["document_number"] = Field("document_number", "F-001")
                },
                new List<ExtractedLineItem>());
            var document = CreateDocument(db, extraction);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);
            await service.PersistApprovedDocumentAsync(document);

            Assert.Equal(1, await db.DocumentFields.CountAsync(f => f.DocumentId == document.Id));
        }

        [Fact]
        public async Task PersistApprovedDocumentAsync_NoExtraction_StillTransitionsToSavedToSql()
        {
            using var db = CreateDb();
            var document = CreateDocument(db, extraction: null);
            var service = new DocumentPersistenceService(db);

            await service.PersistApprovedDocumentAsync(document);

            Assert.Equal(DocumentStatus.SavedToSql, document.Status);
            Assert.Empty(await db.DocumentFields.Where(f => f.DocumentId == document.Id).ToListAsync());
        }
    }
}
