using StoreApp.Models.Enums;
using StoreApp.Services.Parsing;

namespace StoreApp.Tests.Services.Parsing
{
    public class PdfDocumentParserTests
    {
        private readonly PdfDocumentParser _parser = new();

        [Fact]
        public async Task ParseAsync_ExtractsTextFromDigitalPdf()
        {
            var bytes = PdfTestFixture.CreateSinglePagePdf("Test Belgesi 123");
            using var stream = new MemoryStream(bytes);

            var result = await _parser.ParseAsync(stream);

            Assert.Equal(DocumentSourceFormat.Pdf, result.SourceFormat);
            Assert.Equal(1, result.PageCount);
            Assert.Contains("Test Belgesi 123", result.RawText);
            Assert.Empty(result.Tables);
        }

        [Fact]
        public void CanParse_OnlyAcceptsPdfExtension()
        {
            Assert.True(_parser.CanParse(".pdf"));
            Assert.True(_parser.CanParse(".PDF"));
            Assert.False(_parser.CanParse(".xlsx"));
        }
    }
}
