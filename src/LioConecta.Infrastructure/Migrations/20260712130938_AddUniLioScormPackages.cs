using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniLioScormPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScormPassingScore",
                table: "uni_lio_courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "uni_lio_scorm_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ManifestTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LaunchPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    StorageRoot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScoCount = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_scorm_packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_scorm_packages_uni_lio_course_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "uni_lio_course_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_scorm_packages_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_scorm_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScoreRaw = table.Column<decimal>(type: "numeric", nullable: true),
                    ScoreMin = table.Column<decimal>(type: "numeric", nullable: true),
                    ScoreMax = table.Column<decimal>(type: "numeric", nullable: true),
                    SessionTime = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LessonLocation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SuspendData = table.Column<string>(type: "text", nullable: true),
                    CmiJson = table.Column<string>(type: "text", nullable: true),
                    InitializedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_scorm_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_scorm_attempts_uni_lio_enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "uni_lio_enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_scorm_attempts_uni_lio_scorm_packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "uni_lio_scorm_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_scorm_attempts_CourseId",
                table: "uni_lio_scorm_attempts",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_scorm_attempts_EnrollmentId_PackageId",
                table: "uni_lio_scorm_attempts",
                columns: new[] { "EnrollmentId", "PackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_scorm_attempts_PackageId",
                table: "uni_lio_scorm_attempts",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_scorm_packages_CourseId",
                table: "uni_lio_scorm_packages",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_scorm_packages_ModuleId",
                table: "uni_lio_scorm_packages",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uni_lio_scorm_attempts");

            migrationBuilder.DropTable(
                name: "uni_lio_scorm_packages");

            migrationBuilder.DropColumn(
                name: "ScormPassingScore",
                table: "uni_lio_courses");
        }
    }
}
