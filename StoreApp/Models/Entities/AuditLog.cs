namespace StoreApp.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public required string EntityType { get; set; }
        public int EntityId { get; set; }
        public required string Action { get; set; }
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public int? ChangedBy { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
