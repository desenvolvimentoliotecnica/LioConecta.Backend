using System;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260705194500_AddEmailAttachments")]
public partial class AddEmailAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AttachmentsJson",
            table: "email_messages",
            type: "text",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "email_attachment_staging",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                StoragePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsConsumed = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_email_attachment_staging", x => x.Id);
                table.ForeignKey(
                    name: "FK_email_attachment_staging_people_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "people",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_email_attachment_staging_CreatedById_IsConsumed_ExpiresAt",
            table: "email_attachment_staging",
            columns: new[] { "CreatedById", "IsConsumed", "ExpiresAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "email_attachment_staging");

        migrationBuilder.DropColumn(
            name: "AttachmentsJson",
            table: "email_messages");
    }
}
