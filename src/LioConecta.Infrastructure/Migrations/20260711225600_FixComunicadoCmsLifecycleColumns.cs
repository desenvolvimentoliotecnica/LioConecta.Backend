using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations
{
    /// <summary>
    /// Idempotent repair for environments that applied the originally empty
    /// <c>AddComunicadoCmsLifecycle</c> migration without creating the CMS columns.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260711225600_FixComunicadoCmsLifecycleColumns")]
    public partial class FixComunicadoCmsLifecycleColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE comunicados ADD COLUMN IF NOT EXISTS "Status" integer NOT NULL DEFAULT 2;
                ALTER TABLE comunicados ADD COLUMN IF NOT EXISTS "ScheduledAt" timestamp with time zone NULL;
                ALTER TABLE comunicados ADD COLUMN IF NOT EXISTS "AudienceType" integer NOT NULL DEFAULT 0;
                ALTER TABLE comunicados ADD COLUMN IF NOT EXISTS "AudienceDepartmentIdsJson" text NOT NULL DEFAULT '[]';
                CREATE INDEX IF NOT EXISTS "IX_comunicados_Status" ON comunicados ("Status");
                CREATE INDEX IF NOT EXISTS "IX_comunicados_ScheduledAt" ON comunicados ("ScheduledAt");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: columns belong to AddComunicadoCmsLifecycle and may already exist from that migration.
        }
    }
}
