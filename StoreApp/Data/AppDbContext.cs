using Microsoft.EntityFrameworkCore;
using StoreApp.Models.Entities;
using StoreApp.Models.Enums;

namespace StoreApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.Code).IsUnique();
                entity.HasData(SeedRoles());
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
                // Soft delete: silinen kullanıcılar varsayılan sorgulardan otomatik hariç tutulur.
                entity.HasQueryFilter(u => u.DeletedAt == null);
                entity.HasData(SeedAdminUser());
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasOne(d => d.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                // Soft delete: silinen belgeler varsayılan sorgulardan otomatik hariç tutulur.
                entity.HasQueryFilter(d => d.DeletedAt == null);
            });
        }

        private static Role[] SeedRoles()
        {
            // Sabit tarih: migration snapshot'ının deterministik kalması için.
            var seedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            return new[]
            {
                new Role
                {
                    Id = 1,
                    Name = "Operatör",
                    Code = nameof(RoleCode.Operator),
                    Description = "Belge yükler, doğrular, düzeltir ve onaya gönderir.",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Role
                {
                    Id = 2,
                    Name = "Yönetici",
                    Code = nameof(RoleCode.Manager),
                    Description = "Operatör yetkilerine ek olarak raporları ve kullanıcıları yönetir.",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Role
                {
                    Id = 3,
                    Name = "Sistem Yöneticisi",
                    Code = nameof(RoleCode.SystemAdmin),
                    Description = "Tüm yetkilere ek olarak entegrasyon ve sistem ayarlarını yönetir.",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Role
                {
                    Id = 4,
                    Name = "Salt Okunur",
                    Code = nameof(RoleCode.ReadOnly),
                    Description = "Belgeleri ve raporları görüntüler, veri değiştiremez.",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            };
        }

        private static User SeedAdminUser()
        {
            var seedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            // Şifre: Admin123! (PasswordHasher<User> ile üretilmiş sabit hash; ilk girişten
            // sonra Sistem Yöneticisi bu şifreyi değiştirmelidir — MVP'de "şifre değiştir"
            // akışı henüz yok).
            return new User
            {
                Id = 1,
                FullName = "Sistem Yöneticisi",
                Email = "admin@storeapp.local",
                PasswordHash = "AQAAAAIAAYagAAAAEJAcU6JdEzf9P2FHs+cXNN7S/u/UboYzgwa91XwAqoJtNN/dg2GxRCAFXX6xCTm05g==",
                RoleId = 3, // SystemAdmin (bkz. SeedRoles: Id 3 = Sistem Yöneticisi)
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            };
        }
    }
}
