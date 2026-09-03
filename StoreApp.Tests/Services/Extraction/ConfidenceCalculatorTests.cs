using StoreApp.Services.Abstractions;
using StoreApp.Services.Extraction;

namespace StoreApp.Tests.Services.Extraction
{
    public class ConfidenceCalculatorTests
    {
        [Fact]
        public void ApplyOcrConfidence_Scalar_NullOcrConfidence_ReturnsBaseUnchanged()
        {
            var result = ConfidenceCalculator.ApplyOcrConfidence(0.8, null);

            Assert.Equal(0.8, result);
        }

        [Fact]
        public void ApplyOcrConfidence_Scalar_MultipliesByOcrConfidence()
        {
            var result = ConfidenceCalculator.ApplyOcrConfidence(0.8, 0.5);

            Assert.Equal(0.4, result);
        }

        [Fact]
        public void ApplyOcrConfidence_ExtractionResult_NullOcrConfidence_ReturnsSameInstance()
        {
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField> { ["document_number"] = new("document_number", "F-1", 1d, "Rule") },
                new List<ExtractedLineItem>());

            var result = ConfidenceCalculator.ApplyOcrConfidence(extraction, null);

            Assert.Same(extraction, result);
        }

        [Fact]
        public void ApplyOcrConfidence_ExtractionResult_ScalesHeaderAndLineItemFieldConfidence()
        {
            var extraction = new ExtractionResult(
                new Dictionary<string, ExtractedField> { ["document_number"] = new("document_number", "F-1", 1d, "Rule") },
                new List<ExtractedLineItem>
                {
                    new(new Dictionary<string, ExtractedField> { ["item_description"] = new("item_description", "Kablo", 0.6, "AI") })
                });

            var result = ConfidenceCalculator.ApplyOcrConfidence(extraction, 0.5);

            Assert.Equal(0.5, result.HeaderFields["document_number"].Confidence);
            Assert.Equal("F-1", result.HeaderFields["document_number"].Value);
            Assert.Equal(0.3, result.LineItems[0].Fields["item_description"].Confidence);
        }
    }
}
