using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoodChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mood_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mood = table.Column<int>(type: "integer", nullable: false),
                    CheckDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mood_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mood_checks_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mood_checks_CheckDate",
                table: "mood_checks",
                column: "CheckDate");

            migrationBuilder.CreateIndex(
                name: "IX_mood_checks_Mood",
                table: "mood_checks",
                column: "Mood");

            migrationBuilder.CreateIndex(
                name: "IX_mood_checks_PersonId_CheckDate",
                table: "mood_checks",
                columns: new[] { "PersonId", "CheckDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mood_checks");
        }
    }
}
