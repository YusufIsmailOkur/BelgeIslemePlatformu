using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Extraction;

namespace StoreApp.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly AppDbContext _db;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEnumerable<IDocumentContentParser> _parsers;
        private readonly IPdfPageRenderer _pdfPageRenderer;
        private readonly IOcrService _ocrService;
        private readonly IDocumentTypeClassifier _documentTypeClassifier;
        private readonly IRuleBasedFieldExtractor _ruleBasedFieldExtractor;
        private readonly IAiFieldExtractor _aiFieldExtractor;
        private readonly TimeSpan _ocrPageTimeout;

        public DocumentProcessingService(
            AppDbContext db,
            IFileStorageService fileStorageService,
            IAuditLogService auditLogService,
            IEnumerable<IDocumentContentParser> parsers,
            IPdfPageRenderer pdfPageRenderer,
            IOcrService ocrService,
            IDocumentTypeClassifier documentTypeClassifier,
            IRuleBasedFieldExtractor ruleBasedFieldExtractor,
            IAiFieldExtractor aiFieldExtractor,
            IConfiguration configuration)
        {
            _db = db;
            _fileStorageService = fileStorageService;
            _auditLogService = auditLogService;
            _parsers = parsers;
            _pdfPageRenderer = pdfPageRenderer;
            _ocrService = ocrService;
            _documentTypeClassifier = documentTypeClassifier;
            _ruleBasedFieldExtractor = ruleBasedFieldExtractor;
            _aiFieldExtractor = aiFieldExtractor;
            var timeoutSeconds = int.TryParse(configuration["Ocr:TimeoutSeconds"], out var seconds) ? seconds : 30;
            _ocrPageTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public async Task ProcessAsync(int documentId, CancellationToken cancellationToken = default)
        {
            var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
                ?? throw new InvalidOperationException($"Belge bulunamadı: {documentId}");

            document.Status = DocumentStatus.Processing;
            document.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            var extension = Path.GetExtension(document.OriginalFileName);
            var parser = _parsers.FirstOrDefault(p => p.CanParse(extension));

            if (parser is null)
            {
                await FailAsync(document, $"Desteklenmeyen dosya türü: {extension}", cancellationToken);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var fileStream = _fileStorageService.OpenRead(document.StoragePath);
                using var buffer = new MemoryStream();
                await fileStream.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;

                var parsed = await parser.ParseAsync(buffer, cancellationToken);

                var isOcrProcessed = false;
                double? ocrConfidence = null;
                string? pageResultsJson = null;

                // PDF'den metin çıkmıyorsa taranmış kabul edilir ve OCR'a yönlendirilir (bkz. Föy 05).
                if (parsed.SourceFormat == DocumentSourceFormat.Pdf && string.IsNullOrWhiteSpace(parsed.RawText))
                {
                    buffer.Position = 0;
                    var pageResults = new List<OcrPageResult>();
                    await foreach (var page in _pdfPageRenderer.RenderPagesAsync(buffer, cancellationToken))
                    {
                        pageResults.Add(await RecognizePageWithTimeoutAsync(page, cancellationToken));
                    }

                    parsed = parsed with { RawText = string.Join("\n", pageResults.Select(p => p.Text)) };
                    isOcrProcessed = true;
                    ocrConfidence = pageResults.Count > 0 ? pageResults.Average(p => p.Confidence) : 0d;
                    // Ortalama güven skoru, sadece küçük bir bölümü (ör. filigran) okunup asıl içeriği hiç
                    // bulunamayan sayfaları gizleyebilir; sayfa bazlı kırılım bu yüzden ayrıca saklanır.
                    pageResultsJson = JsonSerializer.Serialize(pageResults);
                }

                stopwatch.Stop();

                var contentRecord = await _db.DocumentContents
                    .FirstOrDefaultAsync(c => c.DocumentId == document.Id, cancellationToken);

                if (contentRecord is null)
                {
                    contentRecord = new DocumentContent { DocumentId = document.Id };
                    _db.DocumentContents.Add(contentRecord);
                }

                contentRecord.SourceFormat = parsed.SourceFormat;
                contentRecord.RawText = parsed.RawText;
                contentRecord.RawTablesJson = parsed.Tables.Count > 0 ? JsonSerializer.Serialize(parsed.Tables) : null;
                contentRecord.PageCount = parsed.PageCount;
                contentRecord.SheetCount = parsed.SheetCount;
                contentRecord.RowCount = parsed.RowCount;
                contentRecord.Encoding = parsed.Encoding;
                contentRecord.Delimiter = parsed.Delimiter;
                contentRecord.ParseDurationMs = stopwatch.ElapsedMilliseconds;
                contentRecord.ParsedAt = DateTime.UtcNow;
                contentRecord.IsOcrProcessed = isOcrProcessed;
                contentRecord.OcrConfidence = ocrConfidence;
                contentRecord.PageResultsJson = pageResultsJson;

                var suggestion = _documentTypeClassifier.Classify(parsed.RawText, parsed.Tables);
                contentRecord.SuggestedDocumentType = suggestion.Type;
                contentRecord.DocumentTypeConfidence = ConfidenceCalculator.ApplyOcrConfidence(suggestion.Confidence, ocrConfidence);

                var extraction = _ruleBasedFieldExtractor.Extract(parsed.RawText, parsed.Tables);
                if (HybridFieldMerger.IsWeak(extraction, hasTables: parsed.Tables.Count > 0))
                {
                    var aiExtraction = await _aiFieldExtractor.ExtractAsync(parsed.RawText, parsed.Tables, cancellationToken);
                    extraction = HybridFieldMerger.Merge(extraction, aiExtraction);
                }

                extraction = ConfidenceCalculator.ApplyOcrConfidence(extraction, ocrConfidence);

                contentRecord.ExtractedFieldsJson = extraction.HeaderFields.Count > 0 || extraction.LineItems.Count > 0
                    ? JsonSerializer.Serialize(extraction)
                    : null;

                document.Status = DocumentStatus.WaitingValidation;
                document.ProcessingError = null;
                document.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);
                await _auditLogService.LogAsync(
                    "Document", document.Id, isOcrProcessed ? "ParseSucceededViaOcr" : "ParseSucceeded",
                    newValue: isOcrProcessed ? new { ocrConfidence } : null,
                    changedBy: document.UploadedByUserId, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await FailAsync(document, ex.Message, cancellationToken);
            }
        }

        private async Task<OcrPageResult> RecognizePageWithTimeoutAsync(PdfPageImage page, CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_ocrPageTimeout);

            try
            {
                return await _ocrService.RecognizeAsync(page, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"OCR zaman aşımına uğradı (sayfa {page.PageNumber}).");
            }
        }

        private async Task FailAsync(Document document, string errorMessage, CancellationToken cancellationToken)
        {
            document.Status = DocumentStatus.FailedRetry;
            document.ProcessingError = errorMessage;
            document.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync(
                "Document", document.Id, "ParseFailed",
                newValue: new { error = errorMessage },
                changedBy: document.UploadedByUserId,
                cancellationToken: cancellationToken);
        }
    }
}
