using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBenefitCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "benefit_catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogKey = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Desc = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Featured = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PortalUrl = table.Column<string>(type: "text", nullable: true),
                    HelpText = table.Column<string>(type: "text", nullable: false),
                    DefaultMonthlyValue = table.Column<decimal>(type: "numeric", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_benefit_catalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_benefit_catalog_CatalogKey",
                table: "benefit_catalog",
                column: "CatalogKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_benefit_catalog_Category",
                table: "benefit_catalog",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_benefit_catalog_IsActive",
                table: "benefit_catalog",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benefit_catalog");
        }
    }
}
