using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComunicadoArchivedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "comunicados",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_comunicados_ArchivedAt",
                table: "comunicados",
                column: "ArchivedAt");

            migrationBuilder.Sql(
                """
                UPDATE comunicados
                SET "ArchivedAt" = COALESCE("PublishedAt", "CreatedAt")
                WHERE "ArchivedAt" IS NULL AND "Kind" = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_comunicados_ArchivedAt",
                table: "comunicados");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "comunicados");
        }
    }
}
