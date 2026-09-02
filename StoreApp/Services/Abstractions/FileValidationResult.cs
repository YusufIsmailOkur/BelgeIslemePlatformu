namespace StoreApp.Services.Abstractions
{
    public sealed record FileValidationResult(bool IsValid, string? ErrorMessage)
    {
        public static FileValidationResult Success() => new(true, null);

        public static FileValidationResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
