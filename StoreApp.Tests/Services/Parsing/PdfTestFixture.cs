using System.Text;

namespace StoreApp.Tests.Services.Parsing
{
    // Testler için elle inşa edilmiş, sıkıştırmasız/tek sayfalık minimal bir PDF üretir
    // (harici bir PDF yazma kütüphanesine ihtiyaç duymadan gerçek bir PdfPig okuma testi yapabilmek için).
    internal static class PdfTestFixture
    {
        public static byte[] CreateSinglePagePdf(string text)
        {
            var streamContent = $"BT /F1 24 Tf 72 720 Td ({EscapePdfText(text)}) Tj ET";

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /MediaBox [0 0 612 792] /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(streamContent)} >>\nstream\n{streamContent}\nendstream"
            };

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
