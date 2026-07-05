-- Manual apply when `dotnet ef database update` is unavailable.
-- Migration: 20260705154034_AddObservabilityTables

CREATE TABLE IF NOT EXISTS access_events (
    "Id" uuid PRIMARY KEY,
    "OccurredAt" timestamptz NOT NULL,
    "EventType" text NOT NULL,
    "EventName" text NOT NULL,
    "UserId" uuid NULL REFERENCES people("Id") ON DELETE SET NULL,
    "UsernameSnapshot" text NULL,
    "SessionId" uuid NULL,
    "CorrelationId" uuid NOT NULL,
    "Resource" text NULL,
    "Action" text NULL,
    "Permission" text NULL,
    "Result" text NOT NULL,
    "ReasonCode" text NULL,
    "IpAddress" text NULL,
    "IpHash" text NULL,
    "UserAgent" text NULL,
    "MetadataJson" text NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS observability_events (
    "Id" uuid PRIMARY KEY,
    "OccurredAt" timestamptz NOT NULL,
    "EventType" text NOT NULL,
    "EventName" text NOT NULL,
    "Severity" smallint NOT NULL,
    "Application" text NOT NULL,
    "Environment" text NOT NULL,
    "UserId" uuid NULL REFERENCES people("Id") ON DELETE SET NULL,
    "SessionId" uuid NULL,
    "CorrelationId" uuid NOT NULL,
    "TraceId" text NULL,
    "SpanId" text NULL,
    "RequestId" text NULL,
    "HttpMethod" text NULL,
    "Route" text NULL,
    "RouteTemplate" text NULL,
    "StatusCode" integer NULL,
    "DurationMs" integer NULL,
    "ResourceType" text NULL,
    "ResourceId" text NULL,
    "Action" text NULL,
    "Success" boolean NOT NULL,
    "ErrorType" text NULL,
    "ErrorCode" text NULL,
    "MetadataJson" text NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS page_views (
    "Id" uuid PRIMARY KEY,
    "OccurredAt" timestamptz NOT NULL,
    "UserId" uuid NULL REFERENCES people("Id") ON DELETE SET NULL,
    "SessionId" uuid NOT NULL,
    "CorrelationId" uuid NOT NULL,
    "PageName" text NOT NULL,
    "RouteTemplate" text NOT NULL,
    "Module" text NOT NULL,
    "ReferrerTemplate" text NULL,
    "DurationMs" integer NULL,
    "MetadataJson" text NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260705154034_AddObservabilityTables', '8.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705154034_AddObservabilityTables'
);
