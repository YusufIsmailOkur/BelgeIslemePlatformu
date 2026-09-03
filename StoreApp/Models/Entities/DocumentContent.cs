using StoreApp.Models.Enums;

namespace StoreApp.Models.Entities
{
    // Belgeden çıkarılan ham veri; belge başına tek kayıt tutulur (yeniden işlemede üzerine yazılır).
    public class DocumentContent
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public DocumentSourceFormat SourceFormat { get; set; }
        public string? RawText { get; set; }

        // ParsedTable listesinin JSON serileştirmesi (bkz. Services/Abstractions/ParsedDocumentContent.cs).
        public string? RawTablesJson { get; set; }
        public int? PageCount { get; set; }
        public int? SheetCount { get; set; }
        public int? RowCount { get; set; }
        public string? Encoding { get; set; }
        public string? Delimiter { get; set; }
        public long ParseDurationMs { get; set; }
        public DateTime ParsedAt { get; set; }

        // RawText doğrudan çıkarım yerine OCR'dan geldiyse true olur (bkz. Services/DocumentProcessingService).
        public bool IsOcrProcessed { get; set; }
        public double? OcrConfidence { get; set; }

        // OcrPageResult listesinin JSON serileştirmesi (bkz. Services/Abstractions/IOcrService.cs).
        // Tek bir ortalama güven skoru hangi sayfaların aslında boş/başarısız kaldığını gizleyebildiğinden
        // (ör. bir sayfada sadece filigran metni yüksek güvenle okunup asıl içerik hiç bulunamayabilir),
        // sayfa bazlı kırılım burada saklanır.
        public string? PageResultsJson { get; set; }
    }
}
