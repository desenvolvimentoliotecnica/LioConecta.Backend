using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniLio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "uni_lio_courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    Area = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    InstructorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    ExternalUrl = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VisibilityJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstructorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxAttendees = table.Column<int>(type: "integer", nullable: false),
                    MeetingUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_events_people_InstructorPersonId",
                        column: x => x.InstructorPersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_learning_paths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_learning_paths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PassingScore = table.Column<int>(type: "integer", nullable: false),
                    QuestionsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_assessments_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_certificates_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_certificates_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_community_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    LikesCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_community_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_community_posts_people_AuthorPersonId",
                        column: x => x.AuthorPersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_community_posts_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_course_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ContentUrl = table.Column<string>(type: "text", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    QuizJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_course_modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_course_modules_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProgressPct = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_enrollments_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_enrollments_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_integration_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_integration_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_integration_links_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_event_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_event_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_event_registrations_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_event_registrations_uni_lio_events_EventId",
                        column: x => x.EventId,
                        principalTable: "uni_lio_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_path_courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_path_courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_path_courses_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_path_courses_uni_lio_learning_paths_PathId",
                        column: x => x.PathId,
                        principalTable: "uni_lio_learning_paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_course_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_course_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_course_skills_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_course_skills_uni_lio_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "uni_lio_skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_person_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_person_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_person_skills_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_person_skills_uni_lio_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "uni_lio_skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_assessment_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_assessment_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_assessment_attempts_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_assessment_attempts_uni_lio_assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "uni_lio_assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_module_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_module_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_progress_uni_lio_course_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "uni_lio_course_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_progress_uni_lio_enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "uni_lio_enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_assessment_attempts_AssessmentId",
                table: "uni_lio_assessment_attempts",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_assessment_attempts_PersonId_AssessmentId",
                table: "uni_lio_assessment_attempts",
                columns: new[] { "PersonId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_assessments_CourseId",
                table: "uni_lio_assessments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_certificates_CertificateCode",
                table: "uni_lio_certificates",
                column: "CertificateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_certificates_CourseId",
                table: "uni_lio_certificates",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_certificates_PersonId_CourseId",
                table: "uni_lio_certificates",
                columns: new[] { "PersonId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_community_posts_AuthorPersonId",
                table: "uni_lio_community_posts",
                column: "AuthorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_community_posts_CourseId",
                table: "uni_lio_community_posts",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_course_modules_CourseId",
                table: "uni_lio_course_modules",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_course_modules_CourseId_SortOrder",
                table: "uni_lio_course_modules",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_course_skills_CourseId_SkillId",
                table: "uni_lio_course_skills",
                columns: new[] { "CourseId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_course_skills_SkillId",
                table: "uni_lio_course_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_courses_Area",
                table: "uni_lio_courses",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_courses_ContentType",
                table: "uni_lio_courses",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_courses_Department",
                table: "uni_lio_courses",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_courses_IsMandatory",
                table: "uni_lio_courses",
                column: "IsMandatory");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_courses_SeedKey",
                table: "uni_lio_courses",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_enrollments_CourseId",
                table: "uni_lio_enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_enrollments_PersonId_CourseId",
                table: "uni_lio_enrollments",
                columns: new[] { "PersonId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_enrollments_Status",
                table: "uni_lio_enrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_event_registrations_EventId_PersonId",
                table: "uni_lio_event_registrations",
                columns: new[] { "EventId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_event_registrations_PersonId",
                table: "uni_lio_event_registrations",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_events_InstructorPersonId",
                table: "uni_lio_events",
                column: "InstructorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_events_StartsAt",
                table: "uni_lio_events",
                column: "StartsAt");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_integration_links_CourseId",
                table: "uni_lio_integration_links",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_integration_links_SourceType_SourceKey_CourseId",
                table: "uni_lio_integration_links",
                columns: new[] { "SourceType", "SourceKey", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_learning_paths_SeedKey",
                table: "uni_lio_learning_paths",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_progress_EnrollmentId_ModuleId",
                table: "uni_lio_module_progress",
                columns: new[] { "EnrollmentId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_progress_ModuleId",
                table: "uni_lio_module_progress",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_path_courses_CourseId",
                table: "uni_lio_path_courses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_path_courses_PathId",
                table: "uni_lio_path_courses",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_path_courses_PathId_CourseId",
                table: "uni_lio_path_courses",
                columns: new[] { "PathId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_person_skills_PersonId_SkillId",
                table: "uni_lio_person_skills",
                columns: new[] { "PersonId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_person_skills_SkillId",
                table: "uni_lio_person_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_skills_SeedKey",
                table: "uni_lio_skills",
                column: "SeedKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uni_lio_assessment_attempts");

            migrationBuilder.DropTable(
                name: "uni_lio_certificates");

            migrationBuilder.DropTable(
                name: "uni_lio_community_posts");

            migrationBuilder.DropTable(
                name: "uni_lio_course_skills");

            migrationBuilder.DropTable(
                name: "uni_lio_event_registrations");

            migrationBuilder.DropTable(
                name: "uni_lio_integration_links");

            migrationBuilder.DropTable(
                name: "uni_lio_module_progress");

            migrationBuilder.DropTable(
                name: "uni_lio_path_courses");

            migrationBuilder.DropTable(
                name: "uni_lio_person_skills");

            migrationBuilder.DropTable(
                name: "uni_lio_assessments");

            migrationBuilder.DropTable(
                name: "uni_lio_events");

            migrationBuilder.DropTable(
                name: "uni_lio_course_modules");

            migrationBuilder.DropTable(
                name: "uni_lio_enrollments");

            migrationBuilder.DropTable(
                name: "uni_lio_learning_paths");

            migrationBuilder.DropTable(
                name: "uni_lio_skills");

            migrationBuilder.DropTable(
                name: "uni_lio_courses");
        }
    }
}
