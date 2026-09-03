using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly AppDbContext _db;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEnumerable<IDocumentContentParser> _parsers;

        public DocumentProcessingService(
            AppDbContext db,
            IFileStorageService fileStorageService,
            IAuditLogService auditLogService,
            IEnumerable<IDocumentContentParser> parsers)
        {
            _db = db;
            _fileStorageService = fileStorageService;
            _auditLogService = auditLogService;
            _parsers = parsers;
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

                document.Status = DocumentStatus.WaitingValidation;
                document.ProcessingError = null;
                document.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);
                await _auditLogService.LogAsync(
                    "Document", document.Id, "ParseSucceeded",
                    changedBy: document.UploadedByUserId, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await FailAsync(document, ex.Message, cancellationToken);
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
