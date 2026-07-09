using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBenefitCatalogDefaultDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE benefit_catalog
                ADD COLUMN IF NOT EXISTS "DefaultDetailsJson" text NOT NULL DEFAULT '{}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDetailsJson",
                table: "benefit_catalog");
        }
    }
}
