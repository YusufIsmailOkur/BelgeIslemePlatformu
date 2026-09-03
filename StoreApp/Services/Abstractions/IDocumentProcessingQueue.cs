namespace StoreApp.Services.Abstractions
{
    // Yüklenen belgelerin arka planda işlenmesi için basit bir kuyruk (bkz. Föy 05 - işlem kuyruğu).
    public interface IDocumentProcessingQueue
    {
        void QueueDocumentProcessing(int documentId);

        IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
