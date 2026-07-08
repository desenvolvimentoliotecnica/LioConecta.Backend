using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phone_extensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Extension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Mobile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Department = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ManagerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacySourceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_extensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_phone_extensions_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_phone_extensions_Department",
                table: "phone_extensions",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_phone_extensions_Email",
                table: "phone_extensions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_phone_extensions_Extension",
                table: "phone_extensions",
                column: "Extension");

            migrationBuilder.CreateIndex(
                name: "IX_phone_extensions_IsActive",
                table: "phone_extensions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_phone_extensions_LegacySourceId",
                table: "phone_extensions",
                column: "LegacySourceId");

            migrationBuilder.CreateIndex(
                name: "IX_phone_extensions_PersonId",
                table: "phone_extensions",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phone_extensions");
        }
    }
}
