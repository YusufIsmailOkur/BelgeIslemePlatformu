using StoreApp.Models.Entities;

namespace StoreApp.Services.Abstractions
{
    // Onaylanan bir belgenin JSON çıkarım sonucunu kalıcı ilişkisel tablolara
    // (DocumentField, DocumentLineItem, Customer, Product) aktarır (bkz. Föy 08).
    public interface IDocumentPersistenceService
    {
        Task PersistApprovedDocumentAsync(Document document, CancellationToken cancellationToken = default);
    }
}
