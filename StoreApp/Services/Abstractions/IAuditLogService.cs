namespace StoreApp.Services.Abstractions
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string entityType,
            int entityId,
            string action,
            object? oldValue = null,
            object? newValue = null,
            int? changedBy = null,
            CancellationToken cancellationToken = default);
    }
}
