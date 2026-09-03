using System.Text;

namespace StoreApp.Tests.Services.Parsing
{
    internal static class MultiPagePdfFixture
    {
        public static byte[] CreateMultiPagePdf(params string[] texts)
        {
            var pageCount = texts.Length;
            var objects = new List<string>();

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>"); // 1
            var kids = string.Join(" ", Enumerable.Range(0, pageCount).Select(i => $"{3 + i * 2} 0 R"));
            objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>"); // 2

            var fontObjNum = 3 + pageCount * 2;

            for (var i = 0; i < pageCount; i++)
            {
                var pageObjNum = 3 + i * 2;
                var contentObjNum = pageObjNum + 1;
                objects.Add($"<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 {fontObjNum} 0 R >> >> /MediaBox [0 0 612 792] /Contents {contentObjNum} 0 R >>");

                var streamContent = $"BT /F1 24 Tf 72 720 Td ({EscapePdfText(texts[i])}) Tj ET";
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(streamContent)} >>\nstream\n{streamContent}\nendstream");
            }

            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var sb = new StringBuilder();
            sb.Append("%PDF-1.4\n");

            var offsets = new int[objects.Count + 1];
            for (var i = 0; i < objects.Count; i++)
            {
                offsets[i + 1] = Encoding.ASCII.GetByteCount(sb.ToString());
                sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());

            sb.Append($"xref\n0 {objects.Count + 1}\n");
            sb.Append("0000000000 65535 f \n");
            for (var i = 1; i <= objects.Count; i++)
            {
                sb.Append($"{offsets[i]:D10} 00000 n \n");
            }

            sb.Append("trailer\n");
            sb.Append($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
            sb.Append("startxref\n");
            sb.Append($"{xrefOffset}\n");
            sb.Append("%%EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static string EscapePdfText(string text) =>
            text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
