-- Manual apply when AddEmailQueue migration was recorded without creating tables.
-- Migrations: 20260705190911_AddEmailQueue (empty) + 20260705192000_CreateEmailQueueTables

CREATE TABLE IF NOT EXISTS email_configurations (
    "Id" uuid PRIMARY KEY,
    "IsEnabled" boolean NOT NULL,
    "FromAddress" varchar(256) NOT NULL,
    "FromName" varchar(256) NOT NULL,
    "SmtpHost" varchar(256) NOT NULL,
    "SmtpPort" integer NOT NULL,
    "SmtpUsername" varchar(256) NOT NULL,
    "SmtpPasswordProtected" text NULL,
    "UseStartTls" boolean NOT NULL,
    "TimeoutSeconds" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "InitialRetryDelaySeconds" integer NOT NULL,
    "MaxRetryDelaySeconds" integer NOT NULL,
    "DispatchBatchSize" integer NOT NULL,
    "DispatchIntervalSeconds" integer NOT NULL,
    "UpdatedById" uuid NULL REFERENCES people("Id") ON DELETE SET NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_email_configurations_UpdatedById" ON email_configurations ("UpdatedById");

CREATE TABLE IF NOT EXISTS email_messages (
    "Id" uuid PRIMARY KEY,
    "Status" varchar(32) NOT NULL,
    "ToAddressesJson" text NOT NULL,
    "CcAddressesJson" text NULL,
    "BccAddressesJson" text NULL,
    "Subject" varchar(500) NOT NULL,
    "BodyHtml" text NULL,
    "BodyText" text NULL,
    "TemplateKey" varchar(128) NULL,
    "MetadataJson" text NULL,
    "Priority" smallint NOT NULL,
    "IdempotencyKey" varchar(128) NULL,
    "CorrelationId" uuid NULL,
    "AttemptCount" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "LastError" text NULL,
    "ProviderMessageId" varchar(256) NULL,
    "ScheduledAt" timestamptz NOT NULL,
    "NextRetryAt" timestamptz NULL,
    "ProcessingStartedAt" timestamptz NULL,
    "SentAt" timestamptz NULL,
    "CreatedById" uuid NULL REFERENCES people("Id") ON DELETE SET NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_email_messages_CorrelationId" ON email_messages ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_email_messages_CreatedAt" ON email_messages ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_email_messages_CreatedById" ON email_messages ("CreatedById");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_email_messages_IdempotencyKey" ON email_messages ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_email_messages_Status_NextRetryAt_ScheduledAt" ON email_messages ("Status", "NextRetryAt", "ScheduledAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260705192000_CreateEmailQueueTables', '8.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705192000_CreateEmailQueueTables'
);
