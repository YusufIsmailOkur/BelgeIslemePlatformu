using ClosedXML.Excel;
using StoreApp.Services.Parsing;

namespace StoreApp.Tests.Services.Parsing
{
    public class ExcelDocumentParserTests
    {
        private readonly ExcelDocumentParser _parser = new();

        [Fact]
        public async Task ParseAsync_ReadsMultiSheetWorkbook()
        {
            using var stream = BuildWorkbook(
                ("Faturalar", new[] { "BelgeNo", "Tutar" }, new[] { new[] { "F-001", "100" }, new[] { "F-002", "250" } }),
                ("Musteriler", new[] { "Ad" }, new[] { new[] { "Ayse" } }));

            var result = await _parser.ParseAsync(stream);

            Assert.Equal(2, result.SheetCount);
            Assert.Equal(3, result.RowCount);
            Assert.Equal(2, result.Tables.Count);

            var faturalar = result.Tables.Single(t => t.Name == "Faturalar");
            Assert.Equal(new[] { "BelgeNo", "Tutar" }, faturalar.Headers);
            Assert.Equal(new[] { "F-001", "100" }, faturalar.Rows[0]);
            Assert.Equal(new[] { "F-002", "250" }, faturalar.Rows[1]);

            var musteriler = result.Tables.Single(t => t.Name == "Musteriler");
            Assert.Equal(new[] { "Ad" }, musteriler.Headers);
            Assert.Single(musteriler.Rows);
        }

        private static MemoryStream BuildWorkbook(params (string SheetName, string[] Headers, string[][] Rows)[] sheets)
        {
            using var workbook = new XLWorkbook();

            foreach (var (sheetName, headers, rows) in sheets)
            {
                var worksheet = workbook.Worksheets.Add(sheetName);
                for (var col = 0; col < headers.Length; col++)
                {
                    worksheet.Cell(1, col + 1).Value = headers[col];
                }

                for (var row = 0; row < rows.Length; row++)
                {
                    for (var col = 0; col < rows[row].Length; col++)
                    {
                        worksheet.Cell(row + 2, col + 1).Value = rows[row][col];
                    }
                }
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
