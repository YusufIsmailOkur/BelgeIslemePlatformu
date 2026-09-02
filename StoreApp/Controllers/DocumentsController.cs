using System.Security.Claims;
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

        public DocumentsController(
            AppDbContext db,
            IFileValidationService fileValidationService,
            IFileStorageService fileStorageService,
            IAuditLogService auditLogService)
        {
            _db = db;
            _fileValidationService = fileValidationService;
            _fileStorageService = fileStorageService;
            _auditLogService = auditLogService;
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

                results.Add(new DocumentUploadResult(file.FileName, true, null));
            }

            return Json(results);
        }
    }
}
