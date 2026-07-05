-- Manual apply of 20260705130000_ExtendAuditEvents (if dotnet ef hangs)
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "CorrelationId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "DurationMs" integer NULL;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "HttpMethod" text NULL;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "Path" text NULL;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "Source" integer NOT NULL DEFAULT 0;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "StatusCode" integer NULL;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "TransactionId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

CREATE INDEX IF NOT EXISTS "IX_audit_events_CorrelationId" ON audit_events ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_audit_events_Source" ON audit_events ("Source");
CREATE INDEX IF NOT EXISTS "IX_audit_events_TransactionId" ON audit_events ("TransactionId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260705130000_ExtendAuditEvents', '8.0.11'
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705130000_ExtendAuditEvents'
);
