namespace StoreApp.Services.Abstractions
{
    public interface IFileValidationService
    {
        FileValidationResult Validate(IFormFile file);
    }
}
