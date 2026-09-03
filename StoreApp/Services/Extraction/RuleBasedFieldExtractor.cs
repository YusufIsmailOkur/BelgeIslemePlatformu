using StoreApp.Models.Fields;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Text;

namespace StoreApp.Services.Extraction
{
    // Kural tabanlı alan çıkarımı (bkz. Föy 06, madde 4): RawText'teki "Etiket: Değer" satırlarından
    // başlık alanlarını, Excel/CSV tablolarının başlık satırından da kalem alanlarını çıkarır.
    public sealed class RuleBasedFieldExtractor : IRuleBasedFieldExtractor
    {
        private const string RuleSource = "Rule";
        private const double ExactMatchConfidence = 1d;

        // PDF/CSV serbest metnindeki "Etiket: Değer" satırlarını başlık alanlarına eşleyen bilinen etiketler.
        private static readonly IReadOnlyDictionary<string, string[]> HeaderLabelSynonyms = new Dictionary<string, string[]>
        {
            ["document_number"] = new[] { "belge no", "fatura no", "teklif no", "sipariş no", "irsaliye no", "no" },
            ["document_date"] = new[] { "tarih", "belge tarihi", "düzenleme tarihi" },
            ["company_name"] = new[] { "firma", "firma adı", "satıcı" },
            ["customer_name"] = new[] { "müşteri", "müşteri adı", "alıcı", "cari" },
            ["due_date"] = new[] { "termin", "teslim tarihi", "vade tarihi" },
            ["description"] = new[] { "açıklama" },
        };

        // Excel/CSV tablo başlık satırındaki bilinen sütun adları. "açıklama" burada bilerek
        // item_description yerine note'a bağlanır: kalem tablosunda ürün adı genelde ayrı bir
        // "Ürün" sütununda olur, "Açıklama" ise çoğunlukla satıra dair bir nottur.
        private static readonly IReadOnlyDictionary<string, string[]> TableColumnSynonyms = new Dictionary<string, string[]>
        {
            ["document_number"] = new[] { "belge no", "fatura no", "teklif no", "sipariş no", "irsaliye no" },
            ["document_date"] = new[] { "tarih", "belge tarihi" },
            ["company_name"] = new[] { "firma", "firma adı" },
            ["customer_name"] = new[] { "müşteri", "müşteri adı", "cari" },
            ["due_date"] = new[] { "termin", "teslim tarihi" },
            ["item_description"] = new[] { "ürün", "ürün/hizmet", "hizmet", "ürün adı" },
            ["quantity"] = new[] { "miktar", "adet" },
            ["unit"] = new[] { "birim" },
            ["unit_price"] = new[] { "birim fiyat", "birim fiyatı", "fiyat" },
            ["note"] = new[] { "not", "açıklama" },
        };

        private static readonly IReadOnlyDictionary<string, DocumentFieldScope> ScopeByFieldKey =
            DocumentFieldSchema.All.ToDictionary(f => f.Key, f => f.Scope);

        public ExtractionResult Extract(string? rawText, IReadOnlyList<ParsedTable> tables)
        {
            var headerFields = ExtractHeaderFieldsFromText(rawText);
            var lineItems = new List<ExtractedLineItem>();

            foreach (var table in tables)
            {
                var columnMap = MapColumns(table.Headers, TableColumnSynonyms);
                if (columnMap.Count == 0)
                {
                    continue;
                }

                foreach (var row in table.Rows)
                {
                    var lineItemFields = new Dictionary<string, ExtractedField>();

                    foreach (var (fieldKey, columnIndex) in columnMap)
                    {
                        if (columnIndex >= row.Count)
                        {
                            continue;
                        }

                        var value = row[columnIndex].Trim();
                        if (value.Length == 0)
                        {
                            continue;
                        }

                        if (ScopeByFieldKey[fieldKey] == DocumentFieldScope.LineItem)
                        {
                            lineItemFields[fieldKey] = new ExtractedField(fieldKey, value, ExactMatchConfidence, RuleSource);
                        }
                        else if (!headerFields.ContainsKey(fieldKey))
                        {
                            // Belge no/tarih gibi başlık bilgisi toplu listelerde her satırda tekrar
                            // edebilir; RawText'ten çıkmadıysa ilk satırdaki değer kullanılır.
                            headerFields[fieldKey] = new ExtractedField(fieldKey, value, ExactMatchConfidence, RuleSource);
                        }
                    }

                    if (lineItemFields.Count > 0)
                    {
                        lineItems.Add(new ExtractedLineItem(lineItemFields));
                    }
                }
            }

            return new ExtractionResult(headerFields, lineItems);
        }

        private static Dictionary<string, ExtractedField> ExtractHeaderFieldsFromText(string? rawText)
        {
            var result = new Dictionary<string, ExtractedField>();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return result;
            }

            foreach (var line in rawText.Split('\n'))
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
                {
                    continue;
                }

                var label = TurkishTextNormalizer.Normalize(line[..separatorIndex].Trim());
                var value = line[(separatorIndex + 1)..].Trim();
                if (value.Length == 0)
                {
                    continue;
                }

                foreach (var (fieldKey, synonyms) in HeaderLabelSynonyms)
                {
                    if (result.ContainsKey(fieldKey))
                    {
                        continue;
                    }

                    if (Array.IndexOf(synonyms, label) >= 0)
                    {
                        result[fieldKey] = new ExtractedField(fieldKey, value, ExactMatchConfidence, RuleSource);
                        break;
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, int> MapColumns(
            IReadOnlyList<string> headers, IReadOnlyDictionary<string, string[]> synonymsByField)
        {
            var map = new Dictionary<string, int>();
            for (var i = 0; i < headers.Count; i++)
            {
                var normalizedHeader = TurkishTextNormalizer.Normalize(headers[i].Trim());
                foreach (var (fieldKey, synonyms) in synonymsByField)
                {
                    if (!map.ContainsKey(fieldKey) && Array.IndexOf(synonyms, normalizedHeader) >= 0)
                    {
                        map[fieldKey] = i;
                        break;
                    }
                }
            }

            return map;
        }
    }
}
