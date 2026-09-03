using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Text;

namespace StoreApp.Services.Persistence
{
    public class DocumentPersistenceService : IDocumentPersistenceService
    {
        private readonly AppDbContext _db;

        public DocumentPersistenceService(AppDbContext db)
        {
            _db = db;
        }

        public async Task PersistApprovedDocumentAsync(Document document, CancellationToken cancellationToken = default)
        {
            var content = document.Content
                ?? await _db.DocumentContents.FirstOrDefaultAsync(c => c.DocumentId == document.Id, cancellationToken);

            var extraction = content?.ExtractedFieldsJson is { } extractedFieldsJson
                ? JsonSerializer.Deserialize<ExtractionResult>(extractedFieldsJson)
                : null;

            // Belge daha önce onaylanıp kaydedildiyse (ör. yeniden işlenip tekrar onaylandıysa)
            // eski ilişkisel kayıtlar önce temizlenir; aksi halde eski ve yeni veriler karışır.
            var existingFields = await _db.DocumentFields
                .Where(f => f.DocumentId == document.Id).ToListAsync(cancellationToken);
            _db.DocumentFields.RemoveRange(existingFields);
            var existingLineItems = await _db.DocumentLineItems
                .Where(li => li.DocumentId == document.Id).ToListAsync(cancellationToken);
            _db.DocumentLineItems.RemoveRange(existingLineItems);

            if (extraction is not null)
            {
                var customerCache = new Dictionary<string, Customer>();
                var productCache = new Dictionary<string, Product>();

                var customerNameField = extraction.HeaderFields.GetValueOrDefault("customer_name")
                    ?? extraction.HeaderFields.GetValueOrDefault("company_name");
                if (customerNameField is not null && !string.IsNullOrWhiteSpace(customerNameField.Value))
                {
                    document.Customer = await ResolveByNormalizedNameAsync(
                        customerCache, customerNameField.Value, cancellationToken);
                }
                else
                {
                    document.Customer = null;
                }

                foreach (var (key, field) in extraction.HeaderFields)
                {
                    document.Fields.Add(new DocumentField
                    {
                        DocumentId = document.Id,
                        FieldKey = key,
                        Value = field.Value,
                        Confidence = field.Confidence,
                        Source = field.Source
                    });
                }

                var lineNumber = 1;
                foreach (var lineItem in extraction.LineItems)
                {
                    var lineItemEntity = new DocumentLineItem
                    {
                        DocumentId = document.Id,
                        LineNumber = lineNumber++
                    };

                    var productNameField = lineItem.Fields.GetValueOrDefault("item_description");
                    if (productNameField is not null && !string.IsNullOrWhiteSpace(productNameField.Value))
                    {
                        lineItemEntity.Product = await ResolveByNormalizedNameAsync(
                            productCache, productNameField.Value, cancellationToken);
                    }

                    foreach (var (key, field) in lineItem.Fields)
                    {
                        lineItemEntity.Fields.Add(new DocumentField
                        {
                            DocumentId = document.Id,
                            FieldKey = key,
                            Value = field.Value,
                            Confidence = field.Confidence,
                            Source = field.Source
                        });
                    }

                    document.LineItems.Add(lineItemEntity);
                }
            }

            document.Status = DocumentStatus.SavedToSql;
            document.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        // TEntity hem Customer hem Product için aynı normalize-et/bul-veya-oluştur mantığını
        // paylaşır; cache aynı belge içinde tekrar eden isimlerin (ör. iki kalemde aynı ürün)
        // benzersiz indeksi ihlal eden ayrı satırlar olarak eklenmesini önler.
        private async Task<Customer> ResolveByNormalizedNameAsync(
            Dictionary<string, Customer> cache, string name, CancellationToken cancellationToken)
        {
            var trimmedName = name.Trim();
            var normalizedName = TurkishTextNormalizer.Normalize(trimmedName);

            if (cache.TryGetValue(normalizedName, out var cached))
            {
                return cached;
            }

            var existing = await _db.Customers
                .FirstOrDefaultAsync(c => c.NormalizedName == normalizedName, cancellationToken);
            var customer = existing ?? new Customer
            {
                Name = trimmedName,
                NormalizedName = normalizedName,
                CreatedAt = DateTime.UtcNow
            };

            cache[normalizedName] = customer;
            return customer;
        }

        private async Task<Product> ResolveByNormalizedNameAsync(
            Dictionary<string, Product> cache, string name, CancellationToken cancellationToken)
        {
            var trimmedName = name.Trim();
            var normalizedName = TurkishTextNormalizer.Normalize(trimmedName);

            if (cache.TryGetValue(normalizedName, out var cached))
            {
                return cached;
            }

            var existing = await _db.Products
                .FirstOrDefaultAsync(p => p.NormalizedName == normalizedName, cancellationToken);
            var product = existing ?? new Product
            {
                Name = trimmedName,
                NormalizedName = normalizedName,
                CreatedAt = DateTime.UtcNow
            };

            cache[normalizedName] = product;
            return product;
        }
    }
}
