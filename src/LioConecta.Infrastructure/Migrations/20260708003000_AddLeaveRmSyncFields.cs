using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LioConecta.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddLeaveRmSyncFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE employee_leave_balances ADD COLUMN IF NOT EXISTS "DataSource" text;
            ALTER TABLE employee_leave_balances ADD COLUMN IF NOT EXISTS "SyncedAt" timestamp with time zone;

            ALTER TABLE leave_records ADD COLUMN IF NOT EXISTS "ServiceRequestId" uuid;
            ALTER TABLE leave_records ADD COLUMN IF NOT EXISTS "RmExternalId" text;
            ALTER TABLE leave_records ADD COLUMN IF NOT EXISTS "RmSyncStatus" text;
            ALTER TABLE leave_records ADD COLUMN IF NOT EXISTS "DataSource" text;
            ALTER TABLE leave_records ADD COLUMN IF NOT EXISTS "SyncedAt" timestamp with time zone;

            CREATE INDEX IF NOT EXISTS "IX_leave_records_PersonId_RmExternalId"
                ON leave_records ("PersonId", "RmExternalId");
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_leave_records_PersonId_RmExternalId";

            ALTER TABLE leave_records DROP COLUMN IF EXISTS "SyncedAt";
            ALTER TABLE leave_records DROP COLUMN IF EXISTS "DataSource";
            ALTER TABLE leave_records DROP COLUMN IF EXISTS "RmSyncStatus";
            ALTER TABLE leave_records DROP COLUMN IF EXISTS "RmExternalId";
            ALTER TABLE leave_records DROP COLUMN IF EXISTS "ServiceRequestId";

            ALTER TABLE employee_leave_balances DROP COLUMN IF EXISTS "SyncedAt";
            ALTER TABLE employee_leave_balances DROP COLUMN IF EXISTS "DataSource";
            """);
    }
}
