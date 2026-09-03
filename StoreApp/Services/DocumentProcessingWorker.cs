using StoreApp.Services.Abstractions;

namespace StoreApp.Services
{
    // Kuyruğa alınan belgeleri arka planda işler (bkz. Föy 05 - işlem kuyruğu).
    // Her belge kendi DI scope'unda işlenir (AppDbContext/IDocumentProcessingService scoped).
    public class DocumentProcessingWorker : BackgroundService
    {
        private readonly IDocumentProcessingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DocumentProcessingWorker> _logger;

        public DocumentProcessingWorker(
            IDocumentProcessingQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<DocumentProcessingWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var documentId in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
                    await processingService.ProcessAsync(documentId, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // DocumentProcessingService kendi içindeki hataları zaten FailedRetry'e taşır;
                    // buraya yalnızca beklenmeyen (ör. scope/servis çözümleme) durumlar düşer.
                    // Kapsam burada olduğundan bir belgedeki hata kuyruğun geri kalanını durdurmaz.
                    _logger.LogError(ex, "Belge işleme kuyruğunda beklenmeyen hata (DocumentId: {DocumentId})", documentId);
                }
            }
        }
    }
}
