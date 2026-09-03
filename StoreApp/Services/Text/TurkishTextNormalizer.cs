namespace StoreApp.Services.Text
{
    // Türkçe büyük/küçük harf dönüşümündeki 'İ'/'I' tuzağını (ToLowerInvariant'ın 'İ'yi birleşik
    // noktalı 'i'ye çevirmesi) basitçe aşmak için kullanılan ortak normalizasyon. Anahtar kelime
    // eşleştirmesi yapan classifier ve extractor servislerince paylaşılır.
    public static class TurkishTextNormalizer
    {
        public static string Normalize(string text) => text.Replace('İ', 'i').Replace('I', 'i').ToLowerInvariant();
    }
}
