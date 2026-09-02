namespace StoreApp.ViewModels
{
    public sealed record DocumentUploadResult(string FileName, bool Success, string? ErrorMessage);
}
