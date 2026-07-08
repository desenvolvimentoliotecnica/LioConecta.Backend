using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalSystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portal_systems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DestinationType = table.Column<int>(type: "integer", nullable: false),
                    UrlDev = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UrlHml = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UrlPrd = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IconKind = table.Column<int>(type: "integer", nullable: false),
                    IconFaClass = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IconAssetUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AccessNotes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ClickCount = table.Column<long>(type: "bigint", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portal_systems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portal_systems_Category",
                table: "portal_systems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_portal_systems_IsActive",
                table: "portal_systems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_portal_systems_SeedKey",
                table: "portal_systems",
                column: "SeedKey");

            migrationBuilder.CreateIndex(
                name: "IX_portal_systems_Slug",
                table: "portal_systems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portal_systems_SortOrder",
                table: "portal_systems",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portal_systems");
        }
    }
}
