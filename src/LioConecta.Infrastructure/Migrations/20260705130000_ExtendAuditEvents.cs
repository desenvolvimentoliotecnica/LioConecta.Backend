using System;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260705130000_ExtendAuditEvents")]
    /// <inheritdoc />
    public partial class ExtendAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "audit_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DurationMs",
                table: "audit_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "audit_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "audit_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "audit_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "audit_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "audit_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_CorrelationId",
                table: "audit_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_Source",
                table: "audit_events",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_TransactionId",
                table: "audit_events",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_events_CorrelationId",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_Source",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_TransactionId",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "audit_events");
        }
    }
}
