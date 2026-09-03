namespace StoreApp.ViewModels
{
    public sealed record DocumentUploadResult(
        string FileName,
        bool Success,
        string? ErrorMessage,
        int? DocumentId = null,
        string? StatusDisplay = null,
        string? StatusBadgeClass = null);
}
