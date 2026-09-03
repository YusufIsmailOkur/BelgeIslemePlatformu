using StoreApp.Services.Ocr;
using StoreApp.Tests.Services.Parsing;

namespace StoreApp.Tests.Services.Ocr
{
    public class PdfiumPageRendererMultiPageTests
    {
        [Fact]
        public async Task RenderPagesAsync_ForTwelvePagePdf_YieldsAllPagesInOrder()
        {
            var texts = Enumerable.Range(1, 12).Select(i => $"PAGE {i}").ToArray();
            var pdfBytes = MultiPagePdfFixture.CreateMultiPagePdf(texts);
            using var stream = new MemoryStream(pdfBytes);
            var renderer = new PdfiumPageRenderer();

            var pageNumbers = new List<int>();
            await foreach (var page in renderer.RenderPagesAsync(stream))
            {
                pageNumbers.Add(page.PageNumber);
            }

            Assert.Equal(Enumerable.Range(1, 12), pageNumbers);
        }
    }
}
