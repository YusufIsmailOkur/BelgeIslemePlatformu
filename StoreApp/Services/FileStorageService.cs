using StoreApp.Services.Abstractions;

namespace StoreApp.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _rootPath;

        public FileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var configuredPath = configuration["Storage:RootPath"] ?? "App_Data/uploads";
            _rootPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath);

            Directory.CreateDirectory(_rootPath);
        }

        public async Task<StoredFile> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName);
            var storageFileName = $"{Guid.NewGuid():N}{extension}";

            var now = DateTime.UtcNow;
            var relativePath = $"{now:yyyy}/{now:MM}/{now:dd}/{storageFileName}";
            var physicalPath = Path.Combine(_rootPath, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"), storageFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

            await using (var destination = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(destination, cancellationToken);
            }

            return new StoredFile(relativePath, file.FileName, file.Length);
        }
    }
}
