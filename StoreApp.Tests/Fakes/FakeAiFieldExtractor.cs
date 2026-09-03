using StoreApp.Services.Abstractions;

namespace StoreApp.Tests.Fakes
{
    internal sealed class FakeAiFieldExtractor : IAiFieldExtractor
    {
        private readonly ExtractionResult? _result;

        public FakeAiFieldExtractor(ExtractionResult? result = null)
        {
            _result = result;
        }

        public bool WasCalled { get; private set; }

        public Task<ExtractionResult?> ExtractAsync(
            string? rawText, IReadOnlyList<ParsedTable> tables, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(_result);
        }
    }
}
