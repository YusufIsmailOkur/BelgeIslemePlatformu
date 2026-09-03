namespace StoreApp.Tests.Fakes
{
    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;
        private readonly Exception? _exception;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null, Exception? exception = null)
        {
            _responseFactory = responseFactory;
            _exception = exception;
        }

        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;

            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_responseFactory!(request));
        }
    }
}
