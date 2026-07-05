using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayslipRmSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payslips_PersonId_Year_Month",
                table: "payslips");

            migrationBuilder.AddColumn<int>(
                name: "NroPeriodo",
                table: "payslips",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "payslips",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FOLHA");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "payslips",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAtUtc",
                table: "payslips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payslips_PersonId_Year_Month_PaymentType",
                table: "payslips",
                columns: new[] { "PersonId", "Year", "Month", "PaymentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payslips_PersonId_Year_Month_PaymentType",
                table: "payslips");

            migrationBuilder.DropColumn(
                name: "NroPeriodo",
                table: "payslips");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "payslips");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "payslips");

            migrationBuilder.DropColumn(
                name: "SyncedAtUtc",
                table: "payslips");

            migrationBuilder.CreateIndex(
                name: "IX_payslips_PersonId_Year_Month",
                table: "payslips",
                columns: new[] { "PersonId", "Year", "Month" },
                unique: true);
        }
    }
}
