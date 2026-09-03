using StoreApp.Services.Abstractions;

namespace StoreApp.Tests.Fakes
{
    internal sealed class FakeDocumentContentParser : IDocumentContentParser
    {
        private readonly bool _canParse;
        private readonly ParsedDocumentContent? _result;
        private readonly Exception? _exception;

        public FakeDocumentContentParser(bool canParse, ParsedDocumentContent? result = null, Exception? exception = null)
        {
            _canParse = canParse;
            _result = result;
            _exception = exception;
        }

        public bool CanParse(string fileExtension) => _canParse;

        public Task<ParsedDocumentContent> ParseAsync(Stream content, CancellationToken cancellationToken = default)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_result!);
        }
    }
}
