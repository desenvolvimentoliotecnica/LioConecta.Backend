-- Aplica tabelas da fila de e-mail (AddEmailQueue)
-- Uso: docker exec -i lioconecta-postgres psql -U lioconecta -d lioconecta < tools/apply-email-queue.sql

CREATE TABLE IF NOT EXISTS email_configurations (
    "Id" uuid NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "FromAddress" character varying(256) NOT NULL,
    "FromName" character varying(256) NOT NULL,
    "SmtpHost" character varying(256) NOT NULL,
    "SmtpPort" integer NOT NULL,
    "SmtpUsername" character varying(256) NOT NULL,
    "SmtpPasswordProtected" text NULL,
    "UseStartTls" boolean NOT NULL,
    "TimeoutSeconds" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "InitialRetryDelaySeconds" integer NOT NULL,
    "MaxRetryDelaySeconds" integer NOT NULL,
    "DispatchBatchSize" integer NOT NULL,
    "DispatchIntervalSeconds" integer NOT NULL,
    "UpdatedById" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_email_configurations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_email_configurations_people_UpdatedById" FOREIGN KEY ("UpdatedById") REFERENCES people ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS email_messages (
    "Id" uuid NOT NULL,
    "Status" character varying(32) NOT NULL,
    "ToAddressesJson" text NOT NULL,
    "CcAddressesJson" text NULL,
    "BccAddressesJson" text NULL,
    "Subject" character varying(500) NOT NULL,
    "BodyHtml" text NULL,
    "BodyText" text NULL,
    "TemplateKey" character varying(128) NULL,
    "MetadataJson" jsonb NULL,
    "Priority" smallint NOT NULL,
    "IdempotencyKey" character varying(128) NULL,
    "CorrelationId" uuid NULL,
    "AttemptCount" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "LastError" text NULL,
    "ProviderMessageId" character varying(256) NULL,
    "ScheduledAt" timestamp with time zone NOT NULL,
    "NextRetryAt" timestamp with time zone NULL,
    "ProcessingStartedAt" timestamp with time zone NULL,
    "SentAt" timestamp with time zone NULL,
    "CreatedById" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_email_messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_email_messages_people_CreatedById" FOREIGN KEY ("CreatedById") REFERENCES people ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_email_messages_Status_NextRetryAt_ScheduledAt" ON email_messages ("Status", "NextRetryAt", "ScheduledAt");
CREATE INDEX IF NOT EXISTS "IX_email_messages_CreatedAt" ON email_messages ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_email_messages_CorrelationId" ON email_messages ("CorrelationId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_email_messages_IdempotencyKey" ON email_messages ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260705193000_AddEmailQueue', '8.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705193000_AddEmailQueue'
);
