using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "RoleId", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@storeapp.local", "Sistem Yöneticisi", true, null, "AQAAAAIAAYagAAAAEJAcU6JdEzf9P2FHs+cXNN7S/u/UboYzgwa91XwAqoJtNN/dg2GxRCAFXX6xCTm05g==", 3, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
