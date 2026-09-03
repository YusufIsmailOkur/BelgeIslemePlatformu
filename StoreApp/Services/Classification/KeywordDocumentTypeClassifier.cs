using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Classification
{
    // Basit anahtar kelime eşleştirmesiyle belge türü tahmini yapar (bkz. Föy 06, madde 3).
    // Kesin sınıflandırma değil; kullanıcının manuel seçimine yardımcı bir öneridir.
    public sealed class KeywordDocumentTypeClassifier : IDocumentTypeClassifier
    {
        // Her eşleşen anahtar kelime güveni artırır; 3 veya daha fazla farklı eşleşme %100'e ulaşır.
        private const double ConfidencePerMatch = 0.35;

        private static readonly IReadOnlyDictionary<DocumentType, string[]> KeywordsByType = new Dictionary<DocumentType, string[]>
        {
            [DocumentType.Invoice] = new[] { "fatura no", "e-fatura", "fatura", "vergi dairesi", "vergi no", "kdv" },
            [DocumentType.Quote] = new[] { "teklif no", "fiyat teklifi", "proforma", "teklif" },
            [DocumentType.Order] = new[] { "sipariş no", "satın alma siparişi", "sipariş formu", "sipariş" },
            [DocumentType.DispatchNote] = new[] { "irsaliye no", "sevk irsaliyesi", "taşıma irsaliyesi", "irsaliye" },
            [DocumentType.Specification] = new[] { "teknik şartname", "idari şartname", "şartname" },
            [DocumentType.ShipmentRequest] = new[] { "sevkiyat talep formu", "sevkiyat talebi", "nakliye talebi" },
            [DocumentType.ContractAppendix] = new[] { "çerçeve sözleşme eki", "sözleşme eki", "çerçeve sözleşme" },
            [DocumentType.TechnicalAppendix] = new[] { "teknik şartname eki", "teknik ek" },
            [DocumentType.BulkOrderList] = new[] { "toplu sipariş listesi", "toplu sipariş", "sipariş listesi" },
        };

        public DocumentTypeSuggestion Classify(string? rawText, IReadOnlyList<ParsedTable> tables)
        {
            var tableText = string.Join(' ', tables.SelectMany(t => t.Headers.Concat(t.Rows.SelectMany(row => row))));
            var combined = Normalize(string.Join(' ', rawText ?? string.Empty, tableText));

            if (string.IsNullOrWhiteSpace(combined))
            {
                return new DocumentTypeSuggestion(DocumentType.Other, 0d);
            }

            var best = DocumentType.Other;
            var bestMatchCount = 0;

            foreach (var (type, keywords) in KeywordsByType)
            {
                var matchCount = keywords.Count(keyword => combined.Contains(keyword, StringComparison.Ordinal));
                if (matchCount > bestMatchCount)
                {
                    best = type;
                    bestMatchCount = matchCount;
                }
            }

            var confidence = Math.Min(1d, bestMatchCount * ConfidencePerMatch);
            return new DocumentTypeSuggestion(best, confidence);
        }

        // Türkçe büyük/küçük harf dönüşümündeki 'İ'/'I' tuzağını (ToLowerInvariant'ın 'İ'yi
        // birleşik noktalı 'i'ye çevirmesi) basitçe aşmak için ASCII 'i'ye normalize edilir.
        private static string Normalize(string text) => text.Replace('İ', 'i').Replace('I', 'i').ToLowerInvariant();
    }
}
