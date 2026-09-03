using StoreApp.Services.Abstractions;
using StoreApp.Services.Extraction;

namespace StoreApp.Tests.Services.Extraction
{
    public class RuleBasedFieldExtractorTests
    {
        private readonly RuleBasedFieldExtractor _extractor = new();

        [Fact]
        public void Extract_SpecificationLikeFreeText_ExtractsHeaderFields()
        {
            // Şartname gibi serbest metinli belgelerde en azından başlık, firma, tarih ve
            // açıklama alanları çıkarılmalı (bkz. Föy 06 test senaryoları).
            var rawText = string.Join('\n', new[]
            {
                "Belge No: STN-2026-014",
                "Firma: Örnek A.Ş.",
                "Tarih: 03.09.2026",
                "Açıklama: Teknik şartname eki"
            });

            var result = _extractor.Extract(rawText, Array.Empty<ParsedTable>());

            Assert.Equal("STN-2026-014", result.HeaderFields["document_number"].Value);
            Assert.Equal("Örnek A.Ş.", result.HeaderFields["company_name"].Value);
            Assert.Equal("03.09.2026", result.HeaderFields["document_date"].Value);
            Assert.Equal("Teknik şartname eki", result.HeaderFields["description"].Value);
            Assert.All(result.HeaderFields.Values, field => Assert.Equal("Rule", field.Source));
            Assert.Empty(result.LineItems);
        }

        [Fact]
        public void Extract_IgnoresLinesWithUnknownLabelsOrNoSeparator()
        {
            var rawText = "Bu bir açıklama satırı değil, düz metin.\nRastgele Başlık: değer\nTarih";

            var result = _extractor.Extract(rawText, Array.Empty<ParsedTable>());

            Assert.Empty(result.HeaderFields);
        }

        [Fact]
        public void Extract_BulkOrderTable_ExtractsLineItemsPerRow()
        {
            var table = new ParsedTable(
                "Sipariş Kalemleri",
                new[] { "Ürün", "Miktar", "Birim", "Birim Fiyat", "Açıklama" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "Kablo", "100", "metre", "12.50", "Acil" },
                    new[] { "Konnektör", "50", "adet", "3.00", "" }
                });

            var result = _extractor.Extract(rawText: null, tables: new[] { table });

            Assert.Equal(2, result.LineItems.Count);

            var first = result.LineItems[0].Fields;
            Assert.Equal("Kablo", first["item_description"].Value);
            Assert.Equal("100", first["quantity"].Value);
            Assert.Equal("metre", first["unit"].Value);
            Assert.Equal("12.50", first["unit_price"].Value);
            Assert.Equal("Acil", first["note"].Value);

            var second = result.LineItems[1].Fields;
            Assert.Equal("Konnektör", second["item_description"].Value);
            Assert.False(second.ContainsKey("note"));
        }

        [Fact]
        public void Extract_TableWithoutKnownColumns_ProducesNoLineItems()
        {
            var table = new ParsedTable(
                "Sayfa1",
                new[] { "Sütun A", "Sütun B" },
                new List<IReadOnlyList<string>> { new[] { "x", "y" } });

            var result = _extractor.Extract(rawText: null, tables: new[] { table });

            Assert.Empty(result.LineItems);
            Assert.Empty(result.HeaderFields);
        }

        [Fact]
        public void Extract_RepeatedHeaderColumnInBulkTable_UsesFirstRowValue()
        {
            var table = new ParsedTable(
                "Toplu Sipariş",
                new[] { "Sipariş No", "Ürün", "Miktar" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "SIP-1", "Kablo", "10" },
                    new[] { "SIP-1", "Konnektör", "5" }
                });

            var result = _extractor.Extract(rawText: null, tables: new[] { table });

            Assert.Equal("SIP-1", result.HeaderFields["document_number"].Value);
            Assert.Equal(2, result.LineItems.Count);
        }

        [Fact]
        public void Extract_NoRawTextAndNoTables_ReturnsEmptyResult()
        {
            var result = _extractor.Extract(null, Array.Empty<ParsedTable>());

            Assert.Empty(result.HeaderFields);
            Assert.Empty(result.LineItems);
        }
    }
}
