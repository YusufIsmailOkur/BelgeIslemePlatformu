using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceFormat = table.Column<int>(type: "INTEGER", nullable: false),
                    RawText = table.Column<string>(type: "TEXT", nullable: true),
                    RawTablesJson = table.Column<string>(type: "TEXT", nullable: true),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    SheetCount = table.Column<int>(type: "INTEGER", nullable: true),
                    RowCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Encoding = table.Column<string>(type: "TEXT", nullable: true),
                    Delimiter = table.Column<string>(type: "TEXT", nullable: true),
                    ParseDurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ParsedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentContents_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentContents_DocumentId",
                table: "DocumentContents",
                column: "DocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentContents");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "Documents");
        }
    }
}
