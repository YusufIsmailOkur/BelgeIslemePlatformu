using StoreApp.Services.Abstractions;

namespace StoreApp.Services
{
    public class FileValidationService : IFileValidationService
    {
        private static readonly Dictionary<string, string[]> AllowedContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new[] { "application/pdf" },
            [".xls"] = new[] { "application/vnd.ms-excel" },
            [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream" },
            [".csv"] = new[] { "text/csv", "application/vnd.ms-excel", "text/plain", "application/csv", "application/octet-stream" }
        };

        private readonly long _maxFileSizeBytes;

        public FileValidationService(IConfiguration configuration)
        {
            var maxFileSizeMb = configuration.GetValue<int?>("Storage:MaxFileSizeMb") ?? 10;
            _maxFileSizeBytes = maxFileSizeMb * 1024L * 1024L;
        }

        public FileValidationResult Validate(IFormFile file)
        {
            if (file.Length == 0)
            {
                return FileValidationResult.Failure("Dosya boş.");
            }

            if (file.Length > _maxFileSizeBytes)
            {
                return FileValidationResult.Failure($"Dosya boyutu {_maxFileSizeBytes / (1024 * 1024)} MB sınırını aşıyor.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedContentTypesByExtension.TryGetValue(extension, out var allowedContentTypes))
            {
                return FileValidationResult.Failure("Desteklenmeyen dosya uzantısı. İzin verilen türler: PDF, XLS, XLSX, CSV.");
            }

            if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return FileValidationResult.Failure("Dosya içerik türü uzantıyla uyuşmuyor.");
            }

            if (!HasValidSignature(file, extension))
            {
                return FileValidationResult.Failure("Dosya içeriği beklenen formatla uyuşmuyor.");
            }

            return FileValidationResult.Success();
        }

        private static bool HasValidSignature(IFormFile file, string extension)
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[8];
            var bytesRead = stream.Read(header);
            if (bytesRead < 4)
            {
                return false;
            }

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // %PDF
                ".xlsx" => header[0] == 0x50 && header[1] == 0x4B, // PK.. (zip/OOXML)
                ".xls" => header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0, // OLE dosya başlığı
                ".csv" => IsLikelyText(header, bytesRead),
                _ => false
            };
        }

        private static bool IsLikelyText(Span<byte> header, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (header[i] == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
