using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApp.Authorization;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;
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
        private readonly IDocumentProcessingService _documentProcessingService;

        public DocumentsController(
            AppDbContext db,
            IFileValidationService fileValidationService,
            IFileStorageService fileStorageService,
            IAuditLogService auditLogService,
            IDocumentProcessingService documentProcessingService)
        {
            _db = db;
            _fileValidationService = fileValidationService;
            _fileStorageService = fileStorageService;
            _auditLogService = auditLogService;
            _documentProcessingService = documentProcessingService;
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
        public async Task<IActionResult> Upload(List<IFormFile> files, CancellationToken cancellationToken)
        {
            if (files.Count == 0)
            {
                return BadRequest(new[] { new DocumentUploadResult(string.Empty, false, "Yüklenecek dosya seçilmedi.") });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var results = new List<DocumentUploadResult>();

            foreach (var file in files)
            {
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
                    DocumentType = DocumentType.Other,
                    Status = DocumentStatus.Uploaded,
                    UploadedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Documents.Add(document);
                await _db.SaveChangesAsync(cancellationToken);
                await _auditLogService.LogAsync("Document", document.Id, "Upload", changedBy: userId, cancellationToken: cancellationToken);

                await _documentProcessingService.ProcessAsync(document.Id, cancellationToken);

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

            return View(new DocumentPreviewViewModel { Document = document, Tables = tables });
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

            await _documentProcessingService.ProcessAsync(id, cancellationToken);
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
