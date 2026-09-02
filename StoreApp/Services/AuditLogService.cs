using System.Text.Json;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Services.Abstractions;

namespace StoreApp.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string entityType,
            int entityId,
            string action,
            object? oldValue = null,
            object? newValue = null,
            int? changedBy = null,
            CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValueJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
                NewValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue),
                ChangedBy = changedBy,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
