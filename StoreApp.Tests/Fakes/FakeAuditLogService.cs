using StoreApp.Services.Abstractions;

namespace StoreApp.Tests.Fakes
{
    internal sealed class FakeAuditLogService : IAuditLogService
    {
        public Task LogAsync(
            string entityType,
            int entityId,
            string action,
            object? oldValue = null,
            object? newValue = null,
            int? changedBy = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
