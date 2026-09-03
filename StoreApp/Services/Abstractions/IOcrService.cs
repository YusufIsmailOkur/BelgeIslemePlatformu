namespace StoreApp.Services.Abstractions
{
    public sealed record OcrPageResult(int PageNumber, string Text, double Confidence);

    public sealed record OcrResult(IReadOnlyList<OcrPageResult> Pages, string CombinedText, double AverageConfidence);

    // Tek bir PDF sayfa görselinden (bkz. IPdfPageRenderer) metin çıkarır. Gerçek sağlayıcı
    // (Google Vision/Tesseract) entegrasyonu Föy 05'in sonraki adımındadır.
    public interface IOcrService
    {
        Task<OcrPageResult> RecognizeAsync(PdfPageImage page, CancellationToken cancellationToken = default);
    }
}
