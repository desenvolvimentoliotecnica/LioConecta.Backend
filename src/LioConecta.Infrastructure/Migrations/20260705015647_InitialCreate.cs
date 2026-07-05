using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cafeteria_menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cafeteria_menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParentDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_departments_departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    SharePointUrl = table.Column<string>(type: "text", nullable: false),
                    SharePointItemId = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    AzureAdObjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrgChartId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Dept = table.Column<string>(type: "text", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    TeamsUpn = table.Column<string>(type: "text", nullable: true),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    TagsJson = table.Column<string>(type: "text", nullable: false),
                    PersonalDataJson = table.Column<string>(type: "text", nullable: true),
                    SkillsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_people", x => x.Id);
                    table.ForeignKey(
                        name: "FK_people_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_people_people_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "analytics_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_analytics_events_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_events_people_ActorId",
                        column: x => x.ActorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chat_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_conversations_people_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comunicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Excerpt = table.Column<string>(type: "text", nullable: true),
                    ContentJson = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeroImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comunicados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comunicados_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feed_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feed_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feed_posts_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_groups_people_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Href = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    AssigneeTeam = table.Column<string>(type: "text", nullable: true),
                    ExternalRef = table.Column<string>(type: "text", nullable: true),
                    TimelineJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_requests_people_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookmarksJson = table.Column<string>(type: "text", nullable: false),
                    FavoritesJson = table.Column<string>(type: "text", nullable: false),
                    ShortcutsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_preferences_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comunicado_reads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComunicadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comunicado_reads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comunicado_reads_comunicados_ComunicadoId",
                        column: x => x.ComunicadoId,
                        principalTable: "comunicados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comunicado_reads_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "celebrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CelebratedPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_celebrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_celebrations_feed_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "feed_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_celebrations_people_CelebratedPersonId",
                        column: x => x.CelebratedPersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comments_feed_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "feed_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comments_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_polls_feed_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "feed_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reactions_feed_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "feed_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reactions_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_members_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_members_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_posts_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_posts_people_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_request_events_people_ActorId",
                        column: x => x.ActorId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_service_request_events_service_requests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_options_polls_PollId",
                        column: x => x.PollId,
                        principalTable: "polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_votes_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_poll_votes_poll_options_PollOptionId",
                        column: x => x.PollOptionId,
                        principalTable: "poll_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_EventType",
                table: "analytics_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_OccurredAt",
                table: "analytics_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_PersonId",
                table: "analytics_events",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_Action",
                table: "audit_events",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_ActorId",
                table: "audit_events",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_CreatedAt",
                table: "audit_events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cafeteria_menus_Date",
                table: "cafeteria_menus",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_ExternalId",
                table: "calendar_events",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_StartAt",
                table: "calendar_events",
                column: "StartAt");

            migrationBuilder.CreateIndex(
                name: "IX_celebrations_CelebratedPersonId",
                table: "celebrations",
                column: "CelebratedPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_celebrations_PostId",
                table: "celebrations",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_CreatedById",
                table: "chat_conversations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_AuthorId",
                table: "chat_messages",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId",
                table: "chat_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_CreatedAt",
                table: "chat_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_comments_AuthorId",
                table: "comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_comments_PostId",
                table: "comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_comunicado_reads_ComunicadoId_PersonId",
                table: "comunicado_reads",
                columns: new[] { "ComunicadoId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comunicado_reads_PersonId",
                table: "comunicado_reads",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_comunicados_AuthorId",
                table: "comunicados",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_comunicados_Kind",
                table: "comunicados",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_comunicados_PublishedAt",
                table: "comunicados",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                table: "departments",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_departments_ParentDepartmentId",
                table: "departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Category",
                table: "documents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_documents_ModifiedAt",
                table: "documents",
                column: "ModifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_documents_SharePointItemId",
                table: "documents",
                column: "SharePointItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_feed_posts_AuthorId",
                table: "feed_posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_feed_posts_CreatedAt",
                table: "feed_posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_feed_posts_Type",
                table: "feed_posts",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_GroupId_PersonId",
                table: "group_members",
                columns: new[] { "GroupId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_members_PersonId",
                table: "group_members",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_group_posts_AuthorId",
                table: "group_posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_group_posts_GroupId",
                table: "group_posts",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_OwnerId",
                table: "groups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAt",
                table: "notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_PersonId",
                table: "notifications",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_PersonId_IsRead",
                table: "notifications",
                columns: new[] { "PersonId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_people_AzureAdObjectId",
                table: "people",
                column: "AzureAdObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_people_DepartmentId",
                table: "people",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_people_Email",
                table: "people",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_people_ManagerId",
                table: "people",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_people_Slug",
                table: "people",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poll_options_PollId",
                table: "poll_options",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_PersonId",
                table: "poll_votes",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_PollOptionId_PersonId",
                table: "poll_votes",
                columns: new[] { "PollOptionId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_polls_PostId",
                table: "polls",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reactions_PersonId",
                table: "reactions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_reactions_PostId_PersonId",
                table: "reactions",
                columns: new[] { "PostId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_request_events_ActorId",
                table: "service_request_events",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_events_ServiceRequestId",
                table: "service_request_events",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Category",
                table: "service_requests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_RequesterId",
                table: "service_requests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status",
                table: "service_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_PersonId",
                table: "user_preferences",
                column: "PersonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytics_events");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "cafeteria_menus");

            migrationBuilder.DropTable(
                name: "calendar_events");

            migrationBuilder.DropTable(
                name: "celebrations");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "comunicado_reads");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "group_posts");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "poll_votes");

            migrationBuilder.DropTable(
                name: "reactions");

            migrationBuilder.DropTable(
                name: "service_request_events");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "chat_conversations");

            migrationBuilder.DropTable(
                name: "comunicados");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "polls");

            migrationBuilder.DropTable(
                name: "feed_posts");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}
