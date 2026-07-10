using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRmWriteBackAndWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RmExternalId",
                table: "ponto_adjustment_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RmSyncStatus",
                table: "ponto_adjustment_records",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rm_writeback_journals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PortalRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Marker = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ForwardSql = table.Column<string>(type: "text", nullable: false),
                    ReverseSql = table.Column<string>(type: "text", nullable: false),
                    RmKeysJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RolledBackAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rm_writeback_journals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StepsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    AssigneeRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AssigneePersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_steps_workflow_instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rm_writeback_journals_Domain_PortalRecordId",
                table: "rm_writeback_journals",
                columns: new[] { "Domain", "PortalRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_rm_writeback_journals_Marker",
                table: "rm_writeback_journals",
                column: "Marker");

            migrationBuilder.CreateIndex(
                name: "IX_rm_writeback_journals_SessionId",
                table: "rm_writeback_journals",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_rm_writeback_journals_Status",
                table: "rm_writeback_journals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_Key",
                table: "workflow_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_CreatedByPersonId",
                table: "workflow_instances",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_DefinitionKey",
                table: "workflow_instances",
                column: "DefinitionKey");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_Status",
                table: "workflow_instances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_SubjectType_SubjectId",
                table: "workflow_instances",
                columns: new[] { "SubjectType", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_steps_AssigneePersonId",
                table: "workflow_steps",
                column: "AssigneePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_steps_AssigneeRole",
                table: "workflow_steps",
                column: "AssigneeRole");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_steps_InstanceId",
                table: "workflow_steps",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_steps_Status",
                table: "workflow_steps",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rm_writeback_journals");

            migrationBuilder.DropTable(
                name: "workflow_definitions");

            migrationBuilder.DropTable(
                name: "workflow_steps");

            migrationBuilder.DropTable(
                name: "workflow_instances");

            migrationBuilder.DropColumn(
                name: "RmExternalId",
                table: "ponto_adjustment_records");

            migrationBuilder.DropColumn(
                name: "RmSyncStatus",
                table: "ponto_adjustment_records");
        }
    }
}
