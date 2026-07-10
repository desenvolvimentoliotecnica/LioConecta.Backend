using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookmarkCatalogAndDocumentBibliotecaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "documents",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeedKey",
                table: "documents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bookmark_catalog_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Excerpt = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Href = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Icon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookmark_catalog_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_SeedKey",
                table: "documents",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookmark_catalog_items_IsActive",
                table: "bookmark_catalog_items",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_bookmark_catalog_items_Kind",
                table: "bookmark_catalog_items",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_bookmark_catalog_items_SeedKey",
                table: "bookmark_catalog_items",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookmark_catalog_items_SortOrder",
                table: "bookmark_catalog_items",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookmark_catalog_items");

            migrationBuilder.DropIndex(
                name: "IX_documents_SeedKey",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "SeedKey",
                table: "documents");
        }
    }
}
