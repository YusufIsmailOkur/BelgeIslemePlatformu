using Microsoft.Extensions.Configuration;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Ocr;
using StoreApp.Tests.Fakes;
using StoreApp.Tests.Services.Parsing;

namespace StoreApp.Tests.Services.Ocr
{
    public class TesseractOcrServiceTests
    {
        private static TesseractOcrService CreateService()
        {
            var environment = new FakeWebHostEnvironment(AppContext.BaseDirectory);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ocr:TessDataPath"] = Path.Combine(AppContext.BaseDirectory, "tessdata"),
                    ["Ocr:Language"] = "eng"
                })
                .Build();

            return new TesseractOcrService(environment, configuration);
        }

        [Fact]
        public async Task RecognizeAsync_ForRenderedPdfPage_ExtractsExpectedWord()
        {
            var pdfBytes = PdfTestFixture.CreateSinglePagePdf("HELLO");
            using var pdfStream = new MemoryStream(pdfBytes);

            var renderer = new PdfiumPageRenderer();
            var pages = new List<byte[]>();
            await foreach (var page in renderer.RenderPagesAsync(pdfStream))
            {
                pages.Add(page.PngBytes);
            }

            var pageImage = new PdfPageImage(1, Assert.Single(pages));

            using var ocrService = CreateService();
            var result = await ocrService.RecognizeAsync(pageImage);

            Assert.Contains("HELLO", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.True(result.Confidence > 0);
        }
    }
}
