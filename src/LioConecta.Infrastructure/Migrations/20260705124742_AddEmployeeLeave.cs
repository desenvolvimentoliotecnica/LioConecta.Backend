using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_leave_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableDays = table.Column<int>(type: "integer", nullable: false),
                    AcquiredDays = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDays = table.Column<int>(type: "integer", nullable: false),
                    ExpiredDays = table.Column<int>(type: "integer", nullable: false),
                    BancoHorasBalanceHours = table.Column<decimal>(type: "numeric", nullable: false),
                    NextScheduledStart = table.Column<DateOnly>(type: "date", nullable: true),
                    NextScheduledEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    BreakdownJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_leave_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_leave_balances_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceKey = table.Column<string>(type: "text", nullable: false),
                    RecordType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Days = table.Column<int>(type: "integer", nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_records_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_balances_PersonId",
                table: "employee_leave_balances",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_records_PersonId_StartDate",
                table: "leave_records",
                columns: new[] { "PersonId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_leave_records_Status",
                table: "leave_records",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_leave_balances");

            migrationBuilder.DropTable(
                name: "leave_records");
        }
    }
}
