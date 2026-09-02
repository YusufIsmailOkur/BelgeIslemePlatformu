namespace StoreApp.Models.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? Description { get; set; }
        public string? PermissionsJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
