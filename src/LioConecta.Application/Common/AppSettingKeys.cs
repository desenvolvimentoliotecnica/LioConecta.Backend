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
}
