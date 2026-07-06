-- Idempotent fix: audit_events columns from ExtendAuditEvents migration.
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "CorrelationId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "TransactionId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "Source" integer NOT NULL DEFAULT 0;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "HttpMethod" text;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "Path" text;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "StatusCode" integer;
ALTER TABLE audit_events ADD COLUMN IF NOT EXISTS "DurationMs" integer;
CREATE INDEX IF NOT EXISTS "IX_audit_events_CorrelationId" ON audit_events ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_audit_events_TransactionId" ON audit_events ("TransactionId");
CREATE INDEX IF NOT EXISTS "IX_audit_events_Source" ON audit_events ("Source");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260705130000_ExtendAuditEvents', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;