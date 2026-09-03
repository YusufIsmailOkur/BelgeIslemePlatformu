using System.Text;
using StoreApp.Services.Parsing;

namespace StoreApp.Tests.Services.Parsing
{
    public class CsvDocumentParserTests
    {
        private readonly CsvDocumentParser _parser = new();

        [Fact]
        public async Task ParseAsync_ReadsCommaDelimitedCsvWithHeaderRow()
        {
            var csv = "Ad,Sehir\r\nAyse,Istanbul\r\nMehmet,Ankara\r\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            var result = await _parser.ParseAsync(stream);

            var table = Assert.Single(result.Tables);
            Assert.Equal(new[] { "Ad", "Sehir" }, table.Headers);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(new[] { "Ayse", "Istanbul" }, table.Rows[0]);
            Assert.Equal(2, result.RowCount);
            Assert.Equal(",", result.Delimiter);
        }

        [Fact]
        public async Task ParseAsync_DetectsSemicolonDelimiter()
        {
            var csv = "Ad;Sehir\r\nAyse;Istanbul\r\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            var result = await _parser.ParseAsync(stream);

            Assert.Equal(";", result.Delimiter);
            Assert.Equal(new[] { "Ayse", "Istanbul" }, result.Tables[0].Rows[0]);
        }

        [Fact]
        public async Task ParseAsync_PreservesTurkishCharacters_Utf8WithBom()
        {
            var csv = "Ad,Sehir\r\nGökçe Şişik,Çorum\r\n";
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            using var stream = new MemoryStream(bytes);

            var result = await _parser.ParseAsync(stream);

            Assert.Equal("utf-8-bom", result.Encoding);
            Assert.Equal(new[] { "Gökçe Şişik", "Çorum" }, result.Tables[0].Rows[0]);
        }

        [Fact]
        public async Task ParseAsync_PreservesTurkishCharacters_Windows1254WithoutBom()
        {
            var csv = "Ad,Sehir\r\nGökçe Şişik,Çorum\r\n";
            var windows1254 = Encoding.GetEncoding("windows-1254");
            using var stream = new MemoryStream(windows1254.GetBytes(csv));

            var result = await _parser.ParseAsync(stream);

            Assert.Equal("windows-1254", result.Encoding);
            Assert.Equal(new[] { "Gökçe Şişik", "Çorum" }, result.Tables[0].Rows[0]);
        }

        [Fact]
        public async Task ParseAsync_EmptyFile_ReturnsNoTables()
        {
            using var stream = new MemoryStream(Array.Empty<byte>());

            var result = await _parser.ParseAsync(stream);

            Assert.Empty(result.Tables);
            Assert.Equal(0, result.RowCount);
        }
    }
}
