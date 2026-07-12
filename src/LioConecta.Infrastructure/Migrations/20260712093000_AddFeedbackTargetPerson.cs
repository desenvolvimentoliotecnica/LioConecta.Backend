using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackTargetPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetPersonId",
                table: "feedback_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_feedback_submissions_TargetPersonId_CreatedAt",
                table: "feedback_submissions",
                columns: new[] { "TargetPersonId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_submissions_TargetPersonId",
                table: "feedback_submissions",
                column: "TargetPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_feedback_submissions_people_TargetPersonId",
                table: "feedback_submissions",
                column: "TargetPersonId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_feedback_submissions_people_TargetPersonId",
                table: "feedback_submissions");

            migrationBuilder.DropIndex(
                name: "IX_feedback_submissions_TargetPersonId_CreatedAt",
                table: "feedback_submissions");

            migrationBuilder.DropIndex(
                name: "IX_feedback_submissions_TargetPersonId",
                table: "feedback_submissions");

            migrationBuilder.DropColumn(
                name: "TargetPersonId",
                table: "feedback_submissions");
        }
    }
}
