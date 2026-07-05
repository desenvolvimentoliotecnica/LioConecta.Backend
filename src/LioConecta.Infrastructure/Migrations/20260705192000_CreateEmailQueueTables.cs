using System;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260705192000_CreateEmailQueueTables")]
/// <inheritdoc />
public partial class CreateEmailQueueTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "email_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                FromAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                FromName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SmtpHost = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SmtpPort = table.Column<int>(type: "integer", nullable: false),
                SmtpUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SmtpPasswordProtected = table.Column<string>(type: "text", nullable: true),
                UseStartTls = table.Column<bool>(type: "boolean", nullable: false),
                TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                InitialRetryDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                MaxRetryDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                DispatchBatchSize = table.Column<int>(type: "integer", nullable: false),
                DispatchIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_email_configurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_email_configurations_people_UpdatedById",
                    column: x => x.UpdatedById,
                    principalTable: "people",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "email_messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ToAddressesJson = table.Column<string>(type: "text", nullable: false),
                CcAddressesJson = table.Column<string>(type: "text", nullable: true),
                BccAddressesJson = table.Column<string>(type: "text", nullable: true),
                Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                BodyHtml = table.Column<string>(type: "text", nullable: true),
                BodyText = table.Column<string>(type: "text", nullable: true),
                TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                MetadataJson = table.Column<string>(type: "text", nullable: true),
                Priority = table.Column<short>(type: "smallint", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true),
                ProviderMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_email_messages", x => x.Id);
                table.ForeignKey(
                    name: "FK_email_messages_people_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "people",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_email_configurations_UpdatedById",
            table: "email_configurations",
            column: "UpdatedById");

        migrationBuilder.CreateIndex(
            name: "IX_email_messages_CorrelationId",
            table: "email_messages",
            column: "CorrelationId");

        migrationBuilder.CreateIndex(
            name: "IX_email_messages_CreatedAt",
            table: "email_messages",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_email_messages_CreatedById",
            table: "email_messages",
            column: "CreatedById");

        migrationBuilder.CreateIndex(
            name: "IX_email_messages_IdempotencyKey",
            table: "email_messages",
            column: "IdempotencyKey",
            unique: true,
            filter: "\"IdempotencyKey\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_email_messages_Status_NextRetryAt_ScheduledAt",
            table: "email_messages",
            columns: new[] { "Status", "NextRetryAt", "ScheduledAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "email_messages");
        migrationBuilder.DropTable(name: "email_configurations");
    }
}
