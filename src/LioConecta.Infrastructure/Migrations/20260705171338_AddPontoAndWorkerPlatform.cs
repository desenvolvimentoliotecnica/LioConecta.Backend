using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPontoAndWorkerPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "people",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "timesheet_period_caches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    SummaryJson = table.Column<string>(type: "text", nullable: false),
                    EntriesJson = table.Column<string>(type: "text", nullable: false),
                    SyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_period_caches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timesheet_period_caches_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "totvs_rm_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Server = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Database = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PasswordProtected = table.Column<string>(type: "text", nullable: true),
                    TrustServerCertificate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_totvs_rm_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "worker_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TriggerSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "worker_run_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoggedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_run_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_worker_run_logs_worker_runs_WorkerRunId",
                        column: x => x.WorkerRunId,
                        principalTable: "worker_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_people_EmployeeId",
                table: "people",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_period_caches_PersonId_Year_Month",
                table: "timesheet_period_caches",
                columns: new[] { "PersonId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_run_logs_WorkerRunId",
                table: "worker_run_logs",
                column: "WorkerRunId");

            migrationBuilder.CreateIndex(
                name: "IX_worker_runs_StartedAtUtc",
                table: "worker_runs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_worker_runs_WorkerKey",
                table: "worker_runs",
                column: "WorkerKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timesheet_period_caches");

            migrationBuilder.DropTable(
                name: "totvs_rm_configurations");

            migrationBuilder.DropTable(
                name: "worker_run_logs");

            migrationBuilder.DropTable(
                name: "worker_runs");

            migrationBuilder.DropIndex(
                name: "IX_people_EmployeeId",
                table: "people");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "people");
        }
    }
}
