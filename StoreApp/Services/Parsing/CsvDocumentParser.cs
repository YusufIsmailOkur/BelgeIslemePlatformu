using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services.Parsing
{
    // CSV dosyalarını ayraç ve encoding tespitiyle okur. MVP basitleştirmesi: ilk satır
    // her zaman başlık kabul edilir.
    public class CsvDocumentParser : IDocumentContentParser
    {
        private static readonly char[] CandidateDelimiters = { ',', ';', '\t' };
        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

        public bool CanParse(string fileExtension) =>
            string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase);

        public async Task<ParsedDocumentContent> ParseAsync(Stream content, CancellationToken cancellationToken = default)
        {
            var bytes = await ReadAllBytesAsync(content, cancellationToken);
            var (encoding, encodingName) = DetectEncoding(bytes);
            var delimiter = DetectDelimiter(bytes, encoding);

            using var textReader = new StreamReader(new MemoryStream(bytes), encoding);
            using var csv = new CsvReader(textReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(),
                BadDataFound = null,
                MissingFieldFound = null
            });

            var hasHeader = await csv.ReadAsync();
            if (!hasHeader)
            {
                return new ParsedDocumentContent(
                    DocumentSourceFormat.Csv, null, Array.Empty<ParsedTable>(), null, null, 0, encodingName, delimiter.ToString());
            }

            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();

            var rows = new List<IReadOnlyList<string>>();
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = new string[headers.Length];
                for (var i = 0; i < headers.Length; i++)
                {
                    record[i] = csv.GetField(i) ?? string.Empty;
                }
                rows.Add(record);
            }

            var table = new ParsedTable("csv", headers, rows);

            return new ParsedDocumentContent(
                SourceFormat: DocumentSourceFormat.Csv,
                RawText: null,
                Tables: new[] { table },
                PageCount: null,
                SheetCount: null,
                RowCount: rows.Count,
                Encoding: encodingName,
                Delimiter: delimiter.ToString());
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }

        private static (Encoding Encoding, string Name) DetectEncoding(byte[] bytes)
        {
            if (StartsWith(bytes, Utf8Bom))
            {
                return (Encoding.UTF8, "utf-8-bom");
            }

            var strictUtf8 = Encoding.GetEncoding(
                "utf-8",
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);

            try
            {
                strictUtf8.GetString(bytes);
                return (Encoding.UTF8, "utf-8");
            }
            catch (DecoderFallbackException)
            {
                // BOM'suz ve UTF-8 olmayan dosyalarda Türkçe Windows kaynaklı CSV'lerde en
                // yaygın karşılaşılan kod sayfası varsayılır (bkz. Program.cs CodePages kaydı).
                return (Encoding.GetEncoding("windows-1254"), "windows-1254");
            }
        }

        private static char DetectDelimiter(byte[] bytes, Encoding encoding)
        {
            var newlineIndex = Array.IndexOf(bytes, (byte)'\n');
            var sampleLength = newlineIndex > 0 ? newlineIndex : bytes.Length;
            var firstLine = encoding.GetString(bytes, 0, sampleLength);

            return CandidateDelimiters
                .OrderByDescending(d => firstLine.Count(c => c == d))
                .First();
        }

        private static bool StartsWith(byte[] bytes, byte[] prefix)
        {
            if (bytes.Length < prefix.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                if (bytes[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
