using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessMode",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "groups",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "groups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_groups_ReviewedById",
                table: "groups",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_groups_Status",
                table: "groups",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_groups_people_ReviewedById",
                table: "groups",
                column: "ReviewedById",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                UPDATE groups
                SET "Icon" = 'fa-users'
                WHERE "Icon" = '';

                UPDATE groups
                SET "Status" = 1
                WHERE "Status" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_groups_people_ReviewedById",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_groups_ReviewedById",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_groups_Status",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "AccessMode",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "groups");
        }
    }
}
