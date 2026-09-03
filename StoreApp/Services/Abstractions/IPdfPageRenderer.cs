namespace StoreApp.Services.Abstractions
{
    public sealed record PdfPageImage(int PageNumber, byte[] PngBytes);

    // Dijital metni çıkarılamayan (taranmış) PDF sayfalarını OCR'a gönderilebilir PNG
    // görsellerine dönüştürür (bkz. Föy 05 - PDF sayfa görseli üretimi).
    public interface IPdfPageRenderer
    {
        IAsyncEnumerable<PdfPageImage> RenderPagesAsync(Stream pdfContent, CancellationToken cancellationToken = default);
    }
}
