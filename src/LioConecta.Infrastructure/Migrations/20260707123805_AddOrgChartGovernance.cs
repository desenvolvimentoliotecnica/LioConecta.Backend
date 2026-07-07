using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgChartGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "org_chart_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GovernanceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EditAllowedRolesJson = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    EditAllowedEmailsJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    ViewFullAllowedRolesJson = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    AllowDisplayNameEdit = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReimport = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOverrideBadge = table.Column<bool>(type: "boolean", nullable: false),
                    LastImportAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_chart_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_org_chart_settings_people_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "org_departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ParentDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_org_departments_org_departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "org_departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "org_positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DepartmentName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OrgDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerPositionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    HasManualOverride = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_org_positions_org_departments_OrgDepartmentId",
                        column: x => x.OrgDepartmentId,
                        principalTable: "org_departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_org_positions_org_positions_ManagerPositionId",
                        column: x => x.ManagerPositionId,
                        principalTable: "org_positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_org_positions_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_org_chart_settings_UpdatedById",
                table: "org_chart_settings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_org_departments_Name",
                table: "org_departments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_org_departments_ParentDepartmentId",
                table: "org_departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_org_positions_IsVisible_SortOrder",
                table: "org_positions",
                columns: new[] { "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_org_positions_ManagerPositionId",
                table: "org_positions",
                column: "ManagerPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_org_positions_OrgDepartmentId",
                table: "org_positions",
                column: "OrgDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_org_positions_PersonId",
                table: "org_positions",
                column: "PersonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "org_chart_settings");

            migrationBuilder.DropTable(
                name: "org_positions");

            migrationBuilder.DropTable(
                name: "org_departments");
        }
    }
}
