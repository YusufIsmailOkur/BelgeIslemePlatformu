using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Classification;

namespace StoreApp.Tests.Services.Classification
{
    public class KeywordDocumentTypeClassifierTests
    {
        private readonly KeywordDocumentTypeClassifier _classifier = new();

        [Fact]
        public void Classify_InvoiceText_ReturnsInvoiceWithPositiveConfidence()
        {
            var result = _classifier.Classify(
                "Fatura No: 2026-001\nVergi Dairesi: Kadıköy\nKDV Oranı: %20",
                Array.Empty<ParsedTable>());

            Assert.Equal(DocumentType.Invoice, result.Type);
            Assert.True(result.Confidence > 0);
        }

        [Fact]
        public void Classify_DispatchNoteWithUppercaseTurkishI_StillMatches()
        {
            // "İRSALİYE" büyük harfle, noktalı İ ile yazıldığında da eşleşmeli.
            var result = _classifier.Classify("SEVK İRSALİYESİ\nİRSALİYE NO: 100", Array.Empty<ParsedTable>());

            Assert.Equal(DocumentType.DispatchNote, result.Type);
            Assert.True(result.Confidence > 0);
        }

        [Fact]
        public void Classify_TextWithoutKnownKeywords_ReturnsOtherWithZeroConfidence()
        {
            var result = _classifier.Classify("Bugün hava çok güzel.", Array.Empty<ParsedTable>());

            Assert.Equal(DocumentType.Other, result.Type);
            Assert.Equal(0d, result.Confidence);
        }

        [Fact]
        public void Classify_NullRawTextAndNoTables_ReturnsOtherWithZeroConfidence()
        {
            var result = _classifier.Classify(null, Array.Empty<ParsedTable>());

            Assert.Equal(DocumentType.Other, result.Type);
            Assert.Equal(0d, result.Confidence);
        }

        [Fact]
        public void Classify_MatchesKeywordsFoundOnlyInTableHeaders()
        {
            var table = new ParsedTable(
                "Sayfa1",
                new[] { "Teklif No", "Fiyat Teklifi", "Tutar" },
                new List<IReadOnlyList<string>> { new[] { "T-1", "1000", "TL" } });

            var result = _classifier.Classify(rawText: null, tables: new[] { table });

            Assert.Equal(DocumentType.Quote, result.Type);
            Assert.True(result.Confidence > 0);
        }

        [Fact]
        public void Classify_MoreMatchingKeywords_YieldsHigherConfidence()
        {
            var weak = _classifier.Classify("Sipariş", Array.Empty<ParsedTable>());
            var strong = _classifier.Classify("Sipariş No: 5, Sipariş Formu, Satın Alma Siparişi", Array.Empty<ParsedTable>());

            Assert.Equal(DocumentType.Order, weak.Type);
            Assert.Equal(DocumentType.Order, strong.Type);
            Assert.True(strong.Confidence > weak.Confidence);
            Assert.True(strong.Confidence <= 1d);
        }
    }
}
