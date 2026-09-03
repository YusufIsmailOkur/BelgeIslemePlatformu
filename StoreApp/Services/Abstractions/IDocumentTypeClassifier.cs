using StoreApp.Models.Enums;

namespace StoreApp.Services.Abstractions
{
    public sealed record DocumentTypeSuggestion(DocumentType Type, double Confidence);

    // Belge türünü kesin belirlemez; kullanıcının manuel seçimine yardımcı bir öneri üretir (bkz. Föy 06).
    public interface IDocumentTypeClassifier
    {
        DocumentTypeSuggestion Classify(string? rawText, IReadOnlyList<ParsedTable> tables);
    }
}
