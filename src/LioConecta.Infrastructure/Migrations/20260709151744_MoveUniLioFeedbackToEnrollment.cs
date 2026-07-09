using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveUniLioFeedbackToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentRating",
                table: "uni_lio_module_progress");

            migrationBuilder.DropColumn(
                name: "FeedbackComment",
                table: "uni_lio_module_progress");

            migrationBuilder.AddColumn<int>(
                name: "CourseContentRating",
                table: "uni_lio_enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseFeedbackComment",
                table: "uni_lio_enrollments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseContentRating",
                table: "uni_lio_enrollments");

            migrationBuilder.DropColumn(
                name: "CourseFeedbackComment",
                table: "uni_lio_enrollments");

            migrationBuilder.AddColumn<int>(
                name: "ContentRating",
                table: "uni_lio_module_progress",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackComment",
                table: "uni_lio_module_progress",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
