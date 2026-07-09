using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniLioModuleQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "uni_lio_module_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    InstructorReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LearnerReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_module_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_questions_people_AuthorPersonId",
                        column: x => x.AuthorPersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_questions_uni_lio_course_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "uni_lio_course_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_questions_uni_lio_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "uni_lio_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uni_lio_module_question_replies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsInstructorReply = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uni_lio_module_question_replies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_question_replies_people_AuthorPersonId",
                        column: x => x.AuthorPersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_uni_lio_module_question_replies_uni_lio_module_questions_Qu~",
                        column: x => x.QuestionId,
                        principalTable: "uni_lio_module_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_question_replies_AuthorPersonId",
                table: "uni_lio_module_question_replies",
                column: "AuthorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_question_replies_QuestionId",
                table: "uni_lio_module_question_replies",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_questions_AuthorPersonId",
                table: "uni_lio_module_questions",
                column: "AuthorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_questions_CourseId_ModuleId",
                table: "uni_lio_module_questions",
                columns: new[] { "CourseId", "ModuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_questions_CourseId_Visibility_Status",
                table: "uni_lio_module_questions",
                columns: new[] { "CourseId", "Visibility", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_module_questions_ModuleId",
                table: "uni_lio_module_questions",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uni_lio_module_question_replies");

            migrationBuilder.DropTable(
                name: "uni_lio_module_questions");
        }
    }
}
