using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTeamsTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_teams_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScopesJson = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_teams_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_teams_tokens_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_teams_tokens_ExpiresAt",
                table: "user_teams_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_teams_tokens_PersonId",
                table: "user_teams_tokens",
                column: "PersonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_teams_tokens");
        }
    }
}
