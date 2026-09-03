namespace StoreApp.Models.Entities
{
    // Onaylanan bir belgenin kalem tablosundaki tek bir satırı (bkz. Föy 08 madde 1-2).
    // Alan değerleri burada değil, DocumentField'da (LineItemId bu satıra işaret ederek) tutulur.
    public class DocumentLineItem
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public int LineNumber { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        public ICollection<DocumentField> Fields { get; set; } = new List<DocumentField>();
    }
}
