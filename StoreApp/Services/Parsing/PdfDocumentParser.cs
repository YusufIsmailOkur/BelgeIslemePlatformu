using System.Text;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;
using UglyToad.PdfPig;

namespace StoreApp.Services.Parsing
{
    // Dijital (taranmamış) PDF'lerden metin çıkarır. Taranmış PDF'lerde sayfa metni boş
    // dönebilir; bu durumun OCR'a yönlendirilmesi Föy 05 kapsamındadır.
    public class PdfDocumentParser : IDocumentContentParser
    {
        public bool CanParse(string fileExtension) =>
            string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);

        public Task<ParsedDocumentContent> ParseAsync(Stream content, CancellationToken cancellationToken = default)
        {
            using var pdf = PdfDocument.Open(content);

            var textBuilder = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                textBuilder.AppendLine(page.Text);
            }

            var result = new ParsedDocumentContent(
                SourceFormat: DocumentSourceFormat.Pdf,
                RawText: textBuilder.ToString().Trim(),
                Tables: Array.Empty<ParsedTable>(),
                PageCount: pdf.NumberOfPages,
                SheetCount: null,
                RowCount: null,
                Encoding: null,
                Delimiter: null);

            return Task.FromResult(result);
        }
    }
}
