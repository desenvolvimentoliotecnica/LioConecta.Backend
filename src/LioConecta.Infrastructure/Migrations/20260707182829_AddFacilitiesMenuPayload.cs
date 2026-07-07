using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilitiesMenuPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                table: "cafeteria_menus",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "cafeteria_menus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "cafeteria_menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cafeteria_menus_UpdatedById",
                table: "cafeteria_menus",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_cafeteria_menus_people_UpdatedById",
                table: "cafeteria_menus",
                column: "UpdatedById",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cafeteria_menus_people_UpdatedById",
                table: "cafeteria_menus");

            migrationBuilder.DropIndex(
                name: "IX_cafeteria_menus_UpdatedById",
                table: "cafeteria_menus");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                table: "cafeteria_menus");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "cafeteria_menus");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "cafeteria_menus");
        }
    }
}
