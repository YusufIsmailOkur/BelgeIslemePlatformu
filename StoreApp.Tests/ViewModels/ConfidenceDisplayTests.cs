using StoreApp.ViewModels;

namespace StoreApp.Tests.ViewModels
{
    public class ConfidenceDisplayTests
    {
        [Theory]
        [InlineData(0.1, "text-bg-danger")]
        [InlineData(0.49, "text-bg-danger")]
        [InlineData(0.5, "text-bg-warning")]
        [InlineData(0.79, "text-bg-warning")]
        [InlineData(0.8, "text-bg-success")]
        [InlineData(1.0, "text-bg-success")]
        public void BadgeClass_ReturnsExpectedTierForConfidence(double confidence, string expectedClass)
        {
            Assert.Equal(expectedClass, ConfidenceDisplay.BadgeClass(confidence));
        }

        [Theory]
        [InlineData(0.3, "border-danger border-2")]
        [InlineData(0.6, "border-warning border-2")]
        [InlineData(0.9, "")]
        public void InputBorderClass_ReturnsExpectedTierForConfidence(double confidence, string expectedClass)
        {
            Assert.Equal(expectedClass, ConfidenceDisplay.InputBorderClass(confidence));
        }

        [Fact]
        public void WarningIcon_OnlyShownBelowLowThreshold()
        {
            Assert.NotNull(ConfidenceDisplay.WarningIcon(0.2));
            Assert.Null(ConfidenceDisplay.WarningIcon(0.5));
            Assert.Null(ConfidenceDisplay.WarningIcon(0.9));
        }
    }
}
