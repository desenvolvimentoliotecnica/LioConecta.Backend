using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandGroupsCommunityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApproverId",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResubmitCount",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "group_members",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "group_posts",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE group_members gm
                SET "Role" = 0
                FROM groups g
                WHERE gm."GroupId" = g."Id" AND gm."PersonId" = g."OwnerId";
                """);

            migrationBuilder.CreateTable(
                name: "group_post_reactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_post_reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_post_reactions_group_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "group_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_post_reactions_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_topics_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_topics_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_ownership_transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_ownership_transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_ownership_transfers_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_ownership_transfers_people_FromOwnerId",
                        column: x => x.FromOwnerId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_ownership_transfers_people_ToPersonId",
                        column: x => x.ToPersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_ownership_transfers_people_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "group_topic_replies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_topic_replies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_topic_replies_group_topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "group_topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_topic_replies_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_groups_ApproverId",
                table: "groups",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_ExpiresAt",
                table: "groups",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_group_post_reactions_PersonId",
                table: "group_post_reactions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_group_post_reactions_PostId_PersonId",
                table: "group_post_reactions",
                columns: new[] { "PostId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_topics_AuthorId",
                table: "group_topics",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_group_topics_GroupId",
                table: "group_topics",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_topics_LastActivityAt",
                table: "group_topics",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_group_topic_replies_AuthorId",
                table: "group_topic_replies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_group_topic_replies_TopicId",
                table: "group_topic_replies",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_group_ownership_transfers_ApproverId",
                table: "group_ownership_transfers",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_group_ownership_transfers_FromOwnerId",
                table: "group_ownership_transfers",
                column: "FromOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_group_ownership_transfers_GroupId",
                table: "group_ownership_transfers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_ownership_transfers_Status",
                table: "group_ownership_transfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_group_ownership_transfers_ToPersonId",
                table: "group_ownership_transfers",
                column: "ToPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_groups_people_ApproverId",
                table: "groups",
                column: "ApproverId",
                principalTable: "people",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_groups_people_ApproverId",
                table: "groups");

            migrationBuilder.DropTable(name: "group_post_reactions");
            migrationBuilder.DropTable(name: "group_topic_replies");
            migrationBuilder.DropTable(name: "group_ownership_transfers");
            migrationBuilder.DropTable(name: "group_topics");

            migrationBuilder.DropIndex(name: "IX_groups_ApproverId", table: "groups");
            migrationBuilder.DropIndex(name: "IX_groups_ExpiresAt", table: "groups");

            migrationBuilder.DropColumn(name: "ApproverId", table: "groups");
            migrationBuilder.DropColumn(name: "ExpiresAt", table: "groups");
            migrationBuilder.DropColumn(name: "ResubmitCount", table: "groups");
            migrationBuilder.DropColumn(name: "SubmittedAt", table: "groups");
            migrationBuilder.DropColumn(name: "Role", table: "group_members");
            migrationBuilder.DropColumn(name: "ImageUrl", table: "group_posts");
        }
    }
}
