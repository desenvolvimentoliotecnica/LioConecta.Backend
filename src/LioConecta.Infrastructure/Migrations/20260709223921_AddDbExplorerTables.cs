using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDbExplorerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "db_explorer_der_layouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_explorer_der_layouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_db_explorer_der_layouts_people_ActorId",
                        column: x => x.ActorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "db_explorer_query_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SqlText = table.Column<string>(type: "text", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_explorer_query_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_db_explorer_query_logs_people_ActorId",
                        column: x => x.ActorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "db_explorer_saved_queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SqlText = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_explorer_saved_queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_db_explorer_saved_queries_people_ActorId",
                        column: x => x.ActorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_db_explorer_der_layouts_ActorId_ConnectionId",
                table: "db_explorer_der_layouts",
                columns: new[] { "ActorId", "ConnectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_db_explorer_query_logs_ActorId",
                table: "db_explorer_query_logs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_db_explorer_query_logs_ExecutedAt",
                table: "db_explorer_query_logs",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_db_explorer_saved_queries_ActorId_Name",
                table: "db_explorer_saved_queries",
                columns: new[] { "ActorId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "db_explorer_der_layouts");

            migrationBuilder.DropTable(
                name: "db_explorer_query_logs");

            migrationBuilder.DropTable(
                name: "db_explorer_saved_queries");
        }
    }
}
