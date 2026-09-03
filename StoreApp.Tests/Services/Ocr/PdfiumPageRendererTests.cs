using StoreApp.Services.Ocr;
using StoreApp.Tests.Services.Parsing;

namespace StoreApp.Tests.Services.Ocr
{
    public class PdfiumPageRendererTests
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        [Fact]
        public async Task RenderPagesAsync_ForSinglePagePdf_YieldsOnePngImage()
        {
            var pdfBytes = PdfTestFixture.CreateSinglePagePdf("merhaba");
            using var stream = new MemoryStream(pdfBytes);
            var renderer = new PdfiumPageRenderer();

            var pages = new List<byte[]>();
            await foreach (var page in renderer.RenderPagesAsync(stream))
            {
                pages.Add(page.PngBytes);
            }

            var single = Assert.Single(pages);
            Assert.True(single.Length > PngSignature.Length);
            Assert.Equal(PngSignature, single[..PngSignature.Length]);
        }
    }
}
