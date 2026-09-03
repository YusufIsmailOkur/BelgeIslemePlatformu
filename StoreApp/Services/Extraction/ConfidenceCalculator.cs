using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Extraction
{
    // Alan bazlı confidence hesaplama (bkz. Föy 06, madde 6). Her alanın taban güveni zaten
    // kaynağına göre belirlenmiştir (kural = tam eşleşme, AI = daha düşük); burada belge OCR ile
    // işlendiyse bu taban, sayfa bazlı OCR güven skoruyla çarpılarak zayıflatılır. Taranmış bir
    // belgede metin zaten güvenilmezse, ondan çıkarılan alanlar/tahminler de aynı oranda
    // güvenilmez sayılır — iki bağımsız skor yerine tek, birleşik bir sonuç üretilir.
    public static class ConfidenceCalculator
    {
        public static double ApplyOcrConfidence(double baseConfidence, double? ocrConfidence) =>
            ocrConfidence is { } factor ? Math.Round(baseConfidence * factor, 4) : baseConfidence;

        public static ExtractionResult ApplyOcrConfidence(ExtractionResult result, double? ocrConfidence)
        {
            if (ocrConfidence is null)
            {
                return result;
            }

            var factor = ocrConfidence.Value;
            var headerFields = result.HeaderFields.ToDictionary(kv => kv.Key, kv => Adjust(kv.Value, factor));
            var lineItems = result.LineItems
                .Select(item => new ExtractedLineItem(item.Fields.ToDictionary(kv => kv.Key, kv => Adjust(kv.Value, factor))))
                .ToList();

            return new ExtractionResult(headerFields, lineItems);
        }

        private static ExtractedField Adjust(ExtractedField field, double factor) =>
            field with { Confidence = Math.Round(field.Confidence * factor, 4) };
    }
}
