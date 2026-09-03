namespace StoreApp.Services.Abstractions
{
    // FieldKey, DocumentFieldSchema.All içindeki bir alanın Key'ine karşılık gelir.
    public sealed record ExtractedField(string FieldKey, string Value, double Confidence, string Source);

    public sealed record ExtractedLineItem(IReadOnlyDictionary<string, ExtractedField> Fields);

    public sealed record ExtractionResult(
        IReadOnlyDictionary<string, ExtractedField> HeaderFields,
        IReadOnlyList<ExtractedLineItem> LineItems);

    // Excel/CSV başlıklarından ve bilinen PDF etiketlerinden ("Fatura No:" gibi) alan çıkarır
    // (bkz. Föy 06, madde 4). Kesin doğru kabul edilmez; AI destekli çıkarımla (madde 5)
    // birlikte hibrit sonucu oluşturur.
    public interface IRuleBasedFieldExtractor
    {
        ExtractionResult Extract(string? rawText, IReadOnlyList<ParsedTable> tables);
    }
}
