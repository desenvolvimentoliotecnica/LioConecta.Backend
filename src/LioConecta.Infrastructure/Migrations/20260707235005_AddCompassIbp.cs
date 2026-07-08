using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompassIbp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compass_ibp_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    VersionAtual = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionAnterior = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compass_ibp_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "compass_ibp_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FamiliaComercial = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SkuCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SkuDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ClienteHyperion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Cliente = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Matriz = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Diretoria = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IbpAtual = table.Column<decimal>(type: "numeric", nullable: false),
                    IbpAnterior = table.Column<decimal>(type: "numeric", nullable: false),
                    Variacao = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compass_ibp_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compass_ibp_rows_compass_ibp_snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "compass_ibp_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_rows_SnapshotId",
                table: "compass_ibp_rows",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_rows_SnapshotId_Diretoria",
                table: "compass_ibp_rows",
                columns: new[] { "SnapshotId", "Diretoria" });

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_rows_SnapshotId_FamiliaComercial",
                table: "compass_ibp_rows",
                columns: new[] { "SnapshotId", "FamiliaComercial" });

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_rows_SnapshotId_Tipo",
                table: "compass_ibp_rows",
                columns: new[] { "SnapshotId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_rows_SnapshotId_Unidade",
                table: "compass_ibp_rows",
                columns: new[] { "SnapshotId", "Unidade" });

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_snapshots_ImportedAt",
                table: "compass_ibp_snapshots",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_compass_ibp_snapshots_IsActive",
                table: "compass_ibp_snapshots",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compass_ibp_rows");

            migrationBuilder.DropTable(
                name: "compass_ibp_snapshots");
        }
    }
}
