using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniLioAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletionNotifiedAt",
                table: "uni_lio_enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FeedPostId",
                table: "uni_lio_courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstructorPersonId",
                table: "uni_lio_courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "uni_lio_courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "uni_lio_courses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "uni_lio_courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "uni_lio_courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "uni_lio_courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByPersonId",
                table: "uni_lio_courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "uni_lio_courses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_uni_lio_courses_InstructorPersonId",
                table: "uni_lio_courses",
                column: "InstructorPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_uni_lio_courses_people_InstructorPersonId",
                table: "uni_lio_courses",
                column: "InstructorPersonId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_uni_lio_courses_people_InstructorPersonId",
                table: "uni_lio_courses");

            migrationBuilder.DropIndex(
                name: "IX_uni_lio_courses_InstructorPersonId",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "CompletionNotifiedAt",
                table: "uni_lio_enrollments");

            migrationBuilder.DropColumn(
                name: "FeedPostId",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "InstructorPersonId",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "SubmittedByPersonId",
                table: "uni_lio_courses");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "uni_lio_courses");
        }
    }
}
