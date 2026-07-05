namespace LioConecta.Application.Common;

public static class AppSettingKeys
{
    public const string DatabaseDefaultConnection = "database.default_connection";
    public const string RedisConnection = "redis.connection";

    public const string AzureAdInstance = "azure_ad.instance";
    public const string AzureAdTenantId = "azure_ad.tenant_id";
    public const string AzureAdClientId = "azure_ad.client_id";
    public const string AzureAdAudience = "azure_ad.audience";

    public const string AuthUseDevAuth = "auth.use_dev_auth";

    public const string CorsAllowedOrigins = "cors.allowed_origins";

    public const string IntegrationsUseDevAdapters = "integrations.use_dev_adapters";

    public const string TotvsBaseUrl = "totvs.base_url";
    public const string TotvsApiKey = "totvs.api_key";

    public const string GlpiBaseUrl = "glpi.base_url";
    public const string GlpiAppToken = "glpi.app_token";
    public const string GlpiUserToken = "glpi.user_token";

    public const string GraphTenantId = "graph.tenant_id";
    public const string GraphClientId = "graph.client_id";
    public const string GraphClientSecret = "graph.client_secret";

    public const string WorkersTotvsSyncIntervalMinutes = "workers.totvs_sync_interval_minutes";
    public const string WorkersGraphSyncIntervalMinutes = "workers.graph_sync_interval_minutes";

    public const string SerilogDefaultLevel = "serilog.default_level";

    public const string MediaComunicadosRootPath = "media.comunicados.root_path";
    public const string MediaComunicadosMaxSizeBytes = "media.comunicados.max_size_bytes";
    public const string MediaComunicadosAllowedContentTypes = "media.comunicados.allowed_content_types";

    public const string ObservabilityOtelEnabled = "observability.otel.enabled";
    public const string ObservabilityOtelServiceName = "observability.otel.service_name";
    public const string ObservabilityOtelOtlpEndpoint = "observability.otel.otlp_endpoint";
    public const string ObservabilityOtelPrometheusEnabled = "observability.otel.prometheus_enabled";
    public const string ObservabilityOtelTraceSampleRatio = "observability.otel.trace_sample_ratio";

    public const string ObservabilityAccessAuditEnabled = "observability.access_audit.enabled";
    public const string ObservabilityAccessAuditRoutePatterns = "observability.access_audit.route_patterns";

    public const string ObservabilityPageViewsEnabled = "observability.page_views.enabled";
    public const string ObservabilityAuthAuditEnabled = "observability.auth_audit.enabled";

    public const string ObservabilityRetentionObservabilityDays = "observability.retention.observability_days";
    public const string ObservabilityRetentionPageViewDays = "observability.retention.page_view_days";
    public const string ObservabilityRetentionAccessEventDays = "observability.retention.access_event_days";
    public const string ObservabilityRetentionAggregatesDays = "observability.retention.aggregates_days";
    public const string ObservabilityRetentionEnabled = "observability.retention.enabled";
    public const string ObservabilityPrivacyIpMode = "observability.privacy.ip_mode";
}
