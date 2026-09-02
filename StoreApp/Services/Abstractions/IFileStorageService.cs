namespace StoreApp.Services.Abstractions
{
    public interface IFileStorageService
    {
        Task<StoredFile> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
