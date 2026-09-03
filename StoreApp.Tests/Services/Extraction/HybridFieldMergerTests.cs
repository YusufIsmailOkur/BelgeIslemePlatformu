using StoreApp.Services.Abstractions;
using StoreApp.Services.Extraction;

namespace StoreApp.Tests.Services.Extraction
{
    public class HybridFieldMergerTests
    {
        private static ExtractedField Field(string key, string value, string source = "Rule") =>
            new(key, value, source == "Rule" ? 1d : 0.6d, source);

        [Fact]
        public void IsWeak_MissingRequiredHeaderField_ReturnsTrue()
        {
            var result = new ExtractionResult(new Dictionary<string, ExtractedField>(), new List<ExtractedLineItem>());

            Assert.True(HybridFieldMerger.IsWeak(result, hasTables: false));
        }

        [Fact]
        public void IsWeak_HasRequiredFieldAndNoTables_ReturnsFalse()
        {
            var headerFields = new Dictionary<string, ExtractedField> { ["document_number"] = Field("document_number", "F-1") };
            var result = new ExtractionResult(headerFields, new List<ExtractedLineItem>());

            Assert.False(HybridFieldMerger.IsWeak(result, hasTables: false));
        }

        [Fact]
        public void IsWeak_TablesExistButNoLineItemsExtracted_ReturnsTrue()
        {
            var headerFields = new Dictionary<string, ExtractedField> { ["document_number"] = Field("document_number", "F-1") };
            var result = new ExtractionResult(headerFields, new List<ExtractedLineItem>());

            Assert.True(HybridFieldMerger.IsWeak(result, hasTables: true));
        }

        [Fact]
        public void IsWeak_HasRequiredFieldAndLineItems_ReturnsFalse()
        {
            var headerFields = new Dictionary<string, ExtractedField> { ["document_number"] = Field("document_number", "F-1") };
            var lineItems = new List<ExtractedLineItem>
            {
                new(new Dictionary<string, ExtractedField> { ["item_description"] = Field("item_description", "Kablo") })
            };
            var result = new ExtractionResult(headerFields, lineItems);

            Assert.False(HybridFieldMerger.IsWeak(result, hasTables: true));
        }

        [Fact]
        public void Merge_NullAiResult_ReturnsRuleResultUnchanged()
        {
            var ruleResult = new ExtractionResult(
                new Dictionary<string, ExtractedField> { ["document_number"] = Field("document_number", "F-1") },
                new List<ExtractedLineItem>());

            var merged = HybridFieldMerger.Merge(ruleResult, null);

            Assert.Same(ruleResult, merged);
        }

        [Fact]
        public void Merge_AiFillsMissingHeaderFieldsButNeverOverwritesRuleValues()
        {
            var ruleResult = new ExtractionResult(
                new Dictionary<string, ExtractedField> { ["document_number"] = Field("document_number", "F-1") },
                new List<ExtractedLineItem>());

            var aiResult = new ExtractionResult(
                new Dictionary<string, ExtractedField>
                {
                    ["document_number"] = Field("document_number", "SHOULD-NOT-WIN", "AI"),
                    ["company_name"] = Field("company_name", "Örnek A.Ş.", "AI"),
                },
                new List<ExtractedLineItem>());

            var merged = HybridFieldMerger.Merge(ruleResult, aiResult);

            Assert.Equal("F-1", merged.HeaderFields["document_number"].Value);
            Assert.Equal("Rule", merged.HeaderFields["document_number"].Source);
            Assert.Equal("Örnek A.Ş.", merged.HeaderFields["company_name"].Value);
            Assert.Equal("AI", merged.HeaderFields["company_name"].Source);
        }

        [Fact]
        public void Merge_RuleHasNoLineItems_UsesAiLineItems()
        {
            var ruleResult = new ExtractionResult(new Dictionary<string, ExtractedField>(), new List<ExtractedLineItem>());
            var aiLineItems = new List<ExtractedLineItem>
            {
                new(new Dictionary<string, ExtractedField> { ["item_description"] = Field("item_description", "Kablo", "AI") })
            };
            var aiResult = new ExtractionResult(new Dictionary<string, ExtractedField>(), aiLineItems);

            var merged = HybridFieldMerger.Merge(ruleResult, aiResult);

            Assert.Same(aiLineItems, merged.LineItems);
        }

        [Fact]
        public void Merge_RuleAlreadyHasLineItems_KeepsRuleLineItemsIgnoringAi()
        {
            var ruleLineItems = new List<ExtractedLineItem>
            {
                new(new Dictionary<string, ExtractedField> { ["item_description"] = Field("item_description", "Kablo") })
            };
            var ruleResult = new ExtractionResult(new Dictionary<string, ExtractedField>(), ruleLineItems);

            var aiLineItems = new List<ExtractedLineItem>
            {
                new(new Dictionary<string, ExtractedField> { ["item_description"] = Field("item_description", "Farklı", "AI") })
            };
            var aiResult = new ExtractionResult(new Dictionary<string, ExtractedField>(), aiLineItems);

            var merged = HybridFieldMerger.Merge(ruleResult, aiResult);

            Assert.Same(ruleLineItems, merged.LineItems);
        }
    }
}
