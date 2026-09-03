using StoreApp.Services.Abstractions;

namespace StoreApp.Tests.Fakes
{
    internal sealed class FakeOcrService : IOcrService
    {
        private readonly Func<PdfPageImage, OcrPageResult>? _resultFactory;
        private readonly Exception? _exception;
        private readonly bool _hangsUntilCancelled;

        public FakeOcrService(
            Func<PdfPageImage, OcrPageResult>? resultFactory = null,
            Exception? exception = null,
            bool hangsUntilCancelled = false)
        {
            _resultFactory = resultFactory;
            _exception = exception;
            _hangsUntilCancelled = hangsUntilCancelled;
        }

        public List<PdfPageImage> CalledWith { get; } = new();

        public bool WasCalled => CalledWith.Count > 0;

        public async Task<OcrPageResult> RecognizeAsync(PdfPageImage page, CancellationToken cancellationToken = default)
        {
            CalledWith.Add(page);

            if (_hangsUntilCancelled)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            if (_exception is not null)
            {
                throw _exception;
            }

            return _resultFactory!(page);
        }
    }
}
