using System.Threading.Channels;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services
{
    public class DocumentProcessingQueue : IDocumentProcessingQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        public void QueueDocumentProcessing(int documentId) => _channel.Writer.TryWrite(documentId);

        public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
