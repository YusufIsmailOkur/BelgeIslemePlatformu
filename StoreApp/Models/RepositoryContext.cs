using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    // Geçici: Hafta 2'de Data/AppDbContext.cs olarak yeniden adlandırılacak ve
    // users/roles/audit_logs şeması eklenecek (bkz. docs/ARCHITECTURE.md).
    public class RepositoryContext : DbContext
    {
        public RepositoryContext(DbContextOptions<RepositoryContext> options)
        : base(options)
        {
        }
    }
}
