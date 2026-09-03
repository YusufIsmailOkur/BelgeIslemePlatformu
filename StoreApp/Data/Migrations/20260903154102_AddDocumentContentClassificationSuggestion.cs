using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentContentClassificationSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DocumentTypeConfidence",
                table: "DocumentContents",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedDocumentType",
                table: "DocumentContents",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentTypeConfidence",
                table: "DocumentContents");

            migrationBuilder.DropColumn(
                name: "SuggestedDocumentType",
                table: "DocumentContents");
        }
    }
}
