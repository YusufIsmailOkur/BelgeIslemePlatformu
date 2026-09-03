using System.Runtime.CompilerServices;
using PDFtoImage;
using SkiaSharp;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Ocr
{
    // PDFium (PDFtoImage paketi) ile PDF sayfalarını PNG görsellerine dönüştürür.
    public class PdfiumPageRenderer : IPdfPageRenderer
    {
        public async IAsyncEnumerable<PdfPageImage> RenderPagesAsync(
            Stream pdfContent,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var pageNumber = 0;
            await foreach (var bitmap in Conversion.ToImagesAsync(
                pdfContent, leaveOpen: true, cancellationToken: cancellationToken))
            {
                using (bitmap)
                {
                    pageNumber++;
                    using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    yield return new PdfPageImage(pageNumber, encoded.ToArray());
                }
            }
        }
    }
}
