using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260710225516_AddComunicadoCmsLifecycle")]
    public partial class AddComunicadoCmsLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "comunicados",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAt",
                table: "comunicados",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AudienceType",
                table: "comunicados",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AudienceDepartmentIdsJson",
                table: "comunicados",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_comunicados_Status",
                table: "comunicados",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_comunicados_ScheduledAt",
                table: "comunicados",
                column: "ScheduledAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_comunicados_ScheduledAt",
                table: "comunicados");

            migrationBuilder.DropIndex(
                name: "IX_comunicados_Status",
                table: "comunicados");

            migrationBuilder.DropColumn(
                name: "AudienceDepartmentIdsJson",
                table: "comunicados");

            migrationBuilder.DropColumn(
                name: "AudienceType",
                table: "comunicados");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "comunicados");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "comunicados");
        }
    }
}
