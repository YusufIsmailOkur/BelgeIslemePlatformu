using Microsoft.AspNetCore.Http;
using StoreApp.Services.Abstractions;

namespace StoreApp.Tests.Fakes
{
    internal sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<StoredFile> SaveAsync(IFormFile file, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Testlerde kullanılmıyor.");

        // Parser'lar içeriği kendileri sağlanan test verisiyle simüle ettiğinden gerçek bir dosya gerekmez.
        public Stream OpenRead(string relativePath) => new MemoryStream(new byte[] { 1 });
    }
}
