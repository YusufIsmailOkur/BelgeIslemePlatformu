using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApp.Authorization;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
using StoreApp.Models.Fields;
using StoreApp.Services.Abstractions;
using StoreApp.ViewModels;

namespace StoreApp.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IFileValidationService _fileValidationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditLogService _auditLogService;
        private readonly IDocumentProcessingQueue _documentProcessingQueue;

        public DocumentsController(
            AppDbContext db,
            IFileValidationService fileValidationService,
            IFileStorageService fileStorageService,
            IAuditLogService auditLogService,
            IDocumentProcessingQueue documentProcessingQueue)
        {
            _db = db;
            _fileValidationService = fileValidationService;
            _fileStorageService = fileStorageService;
            _auditLogService = auditLogService;
            _documentProcessingQueue = documentProcessingQueue;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var documents = await _db.Documents
                .Include(d => d.UploadedByUser)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(documents);
        }

        [HttpGet]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        public IActionResult Upload() => View();

        [HttpPost]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files, List<DocumentType>? documentTypes, CancellationToken cancellationToken)
        {
            if (files.Count == 0)
            {
                return BadRequest(new[] { new DocumentUploadResult(string.Empty, false, "Yüklenecek dosya seçilmedi.") });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var results = new List<DocumentUploadResult>();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                // Tür listesi, dosya seçimi tarayıcı tarafında bozulursa/eksik gönderilirse
                // kısa kalabilir; bu durumda güvenli varsayılan olarak "Genel Belge" kullanılır.
                var documentType = documentTypes is { } types && i < types.Count ? types[i] : DocumentType.Other;

                var validation = _fileValidationService.Validate(file);
                if (!validation.IsValid)
                {
                    results.Add(new DocumentUploadResult(file.FileName, false, validation.ErrorMessage));
                    continue;
                }

                var stored = await _fileStorageService.SaveAsync(file, cancellationToken);

                var document = new Document
                {
                    OriginalFileName = stored.OriginalFileName,
                    StoragePath = stored.RelativePath,
                    MimeType = file.ContentType,
                    FileSizeBytes = stored.SizeBytes,
                    DocumentType = documentType,
                    Status = DocumentStatus.Uploaded,
                    UploadedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Documents.Add(document);
                await _db.SaveChangesAsync(cancellationToken);
                await _auditLogService.LogAsync("Document", document.Id, "Upload", changedBy: userId, cancellationToken: cancellationToken);

                _documentProcessingQueue.QueueDocumentProcessing(document.Id);

                results.Add(new DocumentUploadResult(
                    file.FileName,
                    true,
                    null,
                    document.Id,
                    document.Status.ToDisplayName(),
                    document.Status.ToBadgeClass()));
            }

            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var document = await _db.Documents
                .Include(d => d.UploadedByUser)
                .Include(d => d.Content)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (document is null)
            {
                return NotFound();
            }

            var tables = document.Content?.RawTablesJson is { } tablesJson
                ? JsonSerializer.Deserialize<List<ParsedTable>>(tablesJson) ?? new List<ParsedTable>()
                : new List<ParsedTable>();

            var ocrPageResults = document.Content?.PageResultsJson is { } pageResultsJson
                ? JsonSerializer.Deserialize<List<OcrPageResult>>(pageResultsJson) ?? new List<OcrPageResult>()
                : new List<OcrPageResult>();

            var extraction = document.Content?.ExtractedFieldsJson is { } extractedFieldsJson
                ? JsonSerializer.Deserialize<ExtractionResult>(extractedFieldsJson)
                : null;

            return View(new DocumentPreviewViewModel { Document = document, Tables = tables, OcrPageResults = ocrPageResults, Extraction = extraction });
        }

        [HttpPost]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFields(int id, Dictionary<string, string> headerFields, CancellationToken cancellationToken)
        {
            var document = await _db.Documents
                .Include(d => d.Content)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (document?.Content is null)
            {
                return NotFound();
            }

            var extraction = document.Content.ExtractedFieldsJson is { } extractedFieldsJson
                ? JsonSerializer.Deserialize<ExtractionResult>(extractedFieldsJson)
                : null;

            var updatedHeaderFields = extraction is not null
                ? new Dictionary<string, ExtractedField>(extraction.HeaderFields)
                : new Dictionary<string, ExtractedField>();

            foreach (var field in DocumentFieldSchema.HeaderFields)
            {
                if (!headerFields.TryGetValue(field.Key, out var value))
                {
                    continue;
                }

                // Kullanıcının elle girdiği/düzelttiği değer kesin kabul edilir (confidence %100,
                // Source="Manual"); kural/AI'nin bulduğu önceki değerin üzerine yazılır.
                if (string.IsNullOrWhiteSpace(value))
                {
                    updatedHeaderFields.Remove(field.Key);
                }
                else
                {
                    updatedHeaderFields[field.Key] = new ExtractedField(field.Key, value.Trim(), 1d, "Manual");
                }
            }

            var lineItems = extraction?.LineItems ?? new List<ExtractedLineItem>();
            var updatedExtraction = new ExtractionResult(updatedHeaderFields, lineItems);

            document.Content.ExtractedFieldsJson = updatedHeaderFields.Count > 0 || lineItems.Count > 0
                ? JsonSerializer.Serialize(updatedExtraction)
                : null;
            document.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLogService.LogAsync(
                "Document", document.Id, "FieldsEdited",
                newValue: updatedHeaderFields, changedBy: userId, cancellationToken: cancellationToken);

            return RedirectToAction(nameof(Preview), new { id });
        }

        [HttpPost]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLineItems(int id, List<Dictionary<string, string>>? lineItems, CancellationToken cancellationToken)
        {
            var document = await _db.Documents
                .Include(d => d.Content)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (document?.Content is null)
            {
                return NotFound();
            }

            var extraction = document.Content.ExtractedFieldsJson is { } extractedFieldsJson
                ? JsonSerializer.Deserialize<ExtractionResult>(extractedFieldsJson)
                : null;

            var headerFields = extraction?.HeaderFields ?? new Dictionary<string, ExtractedField>();

            // Kalem tablosu satır bazlı stabil kimlik taşımaz (kullanıcı serbestçe satır
            // ekleyip/silebilir); bu yüzden alan bazında birleştirme yerine tüm liste
            // gönderilen değerlerle değiştirilir.
            var updatedLineItems = new List<ExtractedLineItem>();
            foreach (var row in lineItems ?? new List<Dictionary<string, string>>())
            {
                var fields = new Dictionary<string, ExtractedField>();
                foreach (var field in DocumentFieldSchema.LineItemFields)
                {
                    if (row.TryGetValue(field.Key, out var value) && !string.IsNullOrWhiteSpace(value))
                    {
                        fields[field.Key] = new ExtractedField(field.Key, value.Trim(), 1d, "Manual");
                    }
                }

                if (fields.Count > 0)
                {
                    updatedLineItems.Add(new ExtractedLineItem(fields));
                }
            }

            var updatedExtraction = new ExtractionResult(headerFields, updatedLineItems);
            document.Content.ExtractedFieldsJson = headerFields.Count > 0 || updatedLineItems.Count > 0
                ? JsonSerializer.Serialize(updatedExtraction)
                : null;
            document.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLogService.LogAsync(
                "Document", document.Id, "LineItemsEdited",
                newValue: updatedLineItems, changedBy: userId, cancellationToken: cancellationToken);

            return RedirectToAction(nameof(Preview), new { id });
        }

        [HttpPost]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            // Sadece doğrulama bekleyen bir belge onaylanabilir/reddedilebilir; zaten karara
            // bağlanmış bir belgenin durumu bu ekrandan tekrar değiştirilemez.
            if (document.Status != DocumentStatus.WaitingValidation)
            {
                return RedirectToAction(nameof(Preview), new { id });
            }

            document.Status = DocumentStatus.Approved;
            document.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLogService.LogAsync(
                "Document", document.Id, "Approved", changedBy: userId, cancellationToken: cancellationToken);

            return RedirectToAction(nameof(Preview), new { id });
        }

        [HttpPost]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
        {
            var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            if (document.Status != DocumentStatus.WaitingValidation)
            {
                return RedirectToAction(nameof(Preview), new { id });
            }

            document.Status = DocumentStatus.Rejected;
            document.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLogService.LogAsync(
                "Document", document.Id, "Rejected", changedBy: userId, cancellationToken: cancellationToken);

            return RedirectToAction(nameof(Preview), new { id });
        }

        [HttpPost]
        [Authorize(Policy = Policies.OperatorOrAbove)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprocess(int id, CancellationToken cancellationToken)
        {
            var exists = await _db.Documents.AnyAsync(d => d.Id == id, cancellationToken);
            if (!exists)
            {
                return NotFound();
            }

            _documentProcessingQueue.QueueDocumentProcessing(id);
            return RedirectToAction(nameof(Preview), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> File(int id, bool download, CancellationToken cancellationToken)
        {
            var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            var stream = _fileStorageService.OpenRead(document.StoragePath);

            // download=false (varsayılan): Content-Disposition gönderilmez, tarayıcı belgeyi
            // (ör. Preview sayfasındaki iframe) satır içi gösterebilir. download=true isteyen
            // kullanıcı için dosya adıyla birlikte "attachment" olarak indirilir.
            return download
                ? File(stream, document.MimeType, document.OriginalFileName, enableRangeProcessing: true)
                : File(stream, document.MimeType, enableRangeProcessing: true);
        }
    }
}
