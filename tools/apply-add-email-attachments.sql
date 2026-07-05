-- Add email attachments support
ALTER TABLE email_messages ADD COLUMN IF NOT EXISTS "AttachmentsJson" text NULL;

CREATE TABLE IF NOT EXISTS email_attachment_staging (
    "Id" uuid NOT NULL,
    "FileName" character varying(256) NOT NULL,
    "ContentType" character varying(128) NOT NULL,
    "StoragePath" character varying(512) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "CreatedById" uuid NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsConsumed" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_email_attachment_staging" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_email_attachment_staging_people_CreatedById" FOREIGN KEY ("CreatedById") REFERENCES people ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_email_attachment_staging_CreatedById_IsConsumed_ExpiresAt"
    ON email_attachment_staging ("CreatedById", "IsConsumed", "ExpiresAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260705194500_AddEmailAttachments', '8.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705194500_AddEmailAttachments'
);
