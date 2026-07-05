using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObservabilityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsernameSnapshot = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    Permission = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    IpHash = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_access_events_people_UserId",
                        column: x => x.UserId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "observability_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<short>(type: "smallint", nullable: false),
                    Application = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TraceId = table.Column<string>(type: "text", nullable: true),
                    SpanId = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<string>(type: "text", nullable: true),
                    HttpMethod = table.Column<string>(type: "text", nullable: true),
                    Route = table.Column<string>(type: "text", nullable: true),
                    RouteTemplate = table.Column<string>(type: "text", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ResourceType = table.Column<string>(type: "text", nullable: true),
                    ResourceId = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorType = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observability_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_observability_events_people_UserId",
                        column: x => x.UserId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "page_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageName = table.Column<string>(type: "text", nullable: false),
                    RouteTemplate = table.Column<string>(type: "text", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    ReferrerTemplate = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_views_people_UserId",
                        column: x => x.UserId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_events_CorrelationId",
                table: "access_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_access_events_EventType_OccurredAt",
                table: "access_events",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_access_events_OccurredAt",
                table: "access_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_access_events_Result_OccurredAt",
                table: "access_events",
                columns: new[] { "Result", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_access_events_UserId_OccurredAt",
                table: "access_events",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_CorrelationId",
                table: "observability_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_EventName_OccurredAt",
                table: "observability_events",
                columns: new[] { "EventName", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_EventType_OccurredAt",
                table: "observability_events",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_OccurredAt",
                table: "observability_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_Severity_OccurredAt",
                table: "observability_events",
                columns: new[] { "Severity", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_TraceId",
                table: "observability_events",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_observability_events_UserId_OccurredAt",
                table: "observability_events",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_page_views_Module_OccurredAt",
                table: "page_views",
                columns: new[] { "Module", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_page_views_OccurredAt",
                table: "page_views",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_page_views_RouteTemplate_OccurredAt",
                table: "page_views",
                columns: new[] { "RouteTemplate", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_page_views_SessionId",
                table: "page_views",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_page_views_UserId_OccurredAt",
                table: "page_views",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_events");

            migrationBuilder.DropTable(
                name: "observability_events");

            migrationBuilder.DropTable(
                name: "page_views");
        }
    }
}
