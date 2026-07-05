using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheetPeriodDaysToTotvsRm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimesheetPeriodEndDay",
                table: "totvs_rm_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimesheetPeriodStartDay",
                table: "totvs_rm_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimesheetPeriodEndDay",
                table: "totvs_rm_configurations");

            migrationBuilder.DropColumn(
                name: "TimesheetPeriodStartDay",
                table: "totvs_rm_configurations");
        }
    }
}
