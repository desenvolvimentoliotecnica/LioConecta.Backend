using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComunicadoHeroImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comunicado_hero_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    PublicUrl = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comunicado_hero_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comunicado_hero_images_people_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comunicado_hero_images_AssetId",
                table: "comunicado_hero_images",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_comunicado_hero_images_AssetId_Version",
                table: "comunicado_hero_images",
                columns: new[] { "AssetId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comunicado_hero_images_CreatedAt",
                table: "comunicado_hero_images",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_comunicado_hero_images_UploadedById",
                table: "comunicado_hero_images",
                column: "UploadedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comunicado_hero_images");
        }
    }
}
