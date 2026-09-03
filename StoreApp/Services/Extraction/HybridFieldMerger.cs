using StoreApp.Models.Fields;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Extraction
{
    // Kural tabanlı çıkarım sonucunu AI çıkarımıyla birleştirir (bkz. Föy 06, madde 5-6:
    // "hibrit çıkarım"). Kural her zaman önceliklidir; AI yalnızca kuralın bulamadığı
    // alanları/kalemleri doldurur, mevcut bir değeri asla ezmez.
    public static class HybridFieldMerger
    {
        // Kural çıkarımının "zayıf" sayılıp AI'nin devreye girmesi gerektiği durumlar:
        // zorunlu bir başlık alanı bulunamadıysa, veya tablo varken hiç kalem çıkarılamadıysa.
        public static bool IsWeak(ExtractionResult ruleResult, bool hasTables)
        {
            var missingRequiredHeaderField = DocumentFieldSchema.HeaderFields
                .Any(field => field.IsRequired && !ruleResult.HeaderFields.ContainsKey(field.Key));

            var missingLineItemsDespiteTables = hasTables && ruleResult.LineItems.Count == 0;

            return missingRequiredHeaderField || missingLineItemsDespiteTables;
        }

        public static ExtractionResult Merge(ExtractionResult ruleResult, ExtractionResult? aiResult)
        {
            if (aiResult is null)
            {
                return ruleResult;
            }

            var headerFields = new Dictionary<string, ExtractedField>(ruleResult.HeaderFields);
            foreach (var (key, field) in aiResult.HeaderFields)
            {
                headerFields.TryAdd(key, field);
            }

            var lineItems = ruleResult.LineItems.Count > 0 ? ruleResult.LineItems : aiResult.LineItems;

            return new ExtractionResult(headerFields, lineItems);
        }
    }
}
