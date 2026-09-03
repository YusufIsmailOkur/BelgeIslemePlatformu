using System.Data;
using ExcelDataReader;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Parsing
{
    // XLS ve XLSX dosyalarını okur. MVP basitleştirmesi: her sayfanın ilk satırı başlık
    // kabul edilir (bkz. Föy 04 riskler: "ilk anlamlı tabloyu bulma" yaklaşımı sonraki haftalara bırakılır).
    public class ExcelDocumentParser : IDocumentContentParser
    {
        public bool CanParse(string fileExtension) =>
            string.Equals(fileExtension, ".xls", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileExtension, ".xlsx", StringComparison.OrdinalIgnoreCase);

        public Task<ParsedDocumentContent> ParseAsync(Stream content, CancellationToken cancellationToken = default)
        {
            using var reader = ExcelReaderFactory.CreateReader(content);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            var tables = new List<ParsedTable>();
            var totalRows = 0;

            foreach (DataTable sheet in dataSet.Tables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var headers = sheet.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
                var rows = sheet.Rows.Cast<DataRow>()
                    .Select(row => (IReadOnlyList<string>)row.ItemArray.Select(cell => cell?.ToString() ?? string.Empty).ToArray())
                    .ToList();

                totalRows += rows.Count;
                tables.Add(new ParsedTable(sheet.TableName, headers, rows));
            }

            var result = new ParsedDocumentContent(
                SourceFormat: DocumentSourceFormat.Excel,
                RawText: null,
                Tables: tables,
                PageCount: null,
                SheetCount: dataSet.Tables.Count,
                RowCount: totalRows,
                Encoding: null,
                Delimiter: null);

            return Task.FromResult(result);
        }
    }
}
