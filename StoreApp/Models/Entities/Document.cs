using StoreApp.Models.Enums;

namespace StoreApp.Models.Entities
{
    public class Document
    {
        public int Id { get; set; }
        public required string OriginalFileName { get; set; }
        public required string StoragePath { get; set; }
        public required string MimeType { get; set; }
        public long FileSizeBytes { get; set; }
        public DocumentType DocumentType { get; set; }
        public DocumentStatus Status { get; set; }
        public int UploadedByUserId { get; set; }
        public User UploadedByUser { get; set; } = null!;
        public string? ProcessingError { get; set; }
        public DocumentContent? Content { get; set; }

        // Föy 08 madde 3: onay sırasında başlıktaki müşteri/firma adından eşleştirilir.
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Föy 08 madde 1-2: onaylanan belgenin kalıcı ilişkisel kaydı.
        public ICollection<DocumentField> Fields { get; set; } = new List<DocumentField>();
        public ICollection<DocumentLineItem> LineItems { get; set; } = new List<DocumentLineItem>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
