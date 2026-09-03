namespace StoreApp.Models.Entities
{
    // Onaylanan bir belgeden kalıcı hale getirilen tek bir alan (bkz. Föy 08 madde 1-2).
    // LineItemId null ise başlık alanıdır (DocumentFieldSchema.HeaderFields), doluysa ilgili
    // kalem satırına ait bir alandır (DocumentFieldSchema.LineItemFields). FieldKey,
    // DocumentFieldSchema.All içindeki bir alanın Key'ine karşılık gelir.
    public class DocumentField
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public int? LineItemId { get; set; }
        public DocumentLineItem? LineItem { get; set; }
        public required string FieldKey { get; set; }
        public required string Value { get; set; }
        public double Confidence { get; set; }
        public required string Source { get; set; }
    }
}
