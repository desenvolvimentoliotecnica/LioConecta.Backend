namespace LioConecta.Application.Common;

public sealed record AppSettingDefinition(
    string Key,
    string Category,
    string Label,
    string? Description,
    string ValueType,
    bool IsSecret,
    string DefaultValue,
    int SortOrder);

public static class AppSettingCatalog
{
    public static IReadOnlyList<AppSettingDefinition> All { get; } =
    [
        new(AppSettingKeys.DatabaseDefaultConnection, "database", "PostgreSQL — connection string",
            "Cadeia de conexão Npgsql usada pela API e workers.", "secret", true,
            "Host=localhost;Port=5433;Database=lioconecta;Username=lioconecta;Password=lioconecta_dev", 1),
        new(AppSettingKeys.RedisConnection, "redis", "Redis — connection string",
            "Backplane SignalR e health check.", "string", false, "localhost:6379", 1),

        new(AppSettingKeys.AzureAdInstance, "azure_ad", "Azure AD — instance URL",
            "Endpoint de login Microsoft.", "string", false, "https://login.microsoftonline.com/", 1),
        new(AppSettingKeys.AzureAdTenantId, "azure_ad", "Azure AD — tenant ID",
            "ID do tenant Entra ID.", "string", false, "", 2),
        new(AppSettingKeys.AzureAdClientId, "azure_ad", "Azure AD — client ID",
            "Application (client) ID. Vazio habilita DevAuth.", "string", false, "", 3),
        new(AppSettingKeys.AzureAdAudience, "azure_ad", "Azure AD — audience",
            "Audience esperada nos tokens JWT.", "string", false, "", 4),

        new(AppSettingKeys.AuthUseDevAuth, "auth", "Permitir DevAuth em desenvolvimento",
            "Quando true, endpoints não exigem autenticação global em Development.", "boolean", false, "true", 1),

        new(AppSettingKeys.CorsAllowedOrigins, "cors", "Origens CORS permitidas",
            "Lista JSON de URLs do front-end em produção.", "json", false,
            "[\"http://localhost:5173\",\"http://localhost:5174\"]", 1),

        new(AppSettingKeys.IntegrationsUseDevAdapters, "integrations", "Usar adaptadores mock",
            "Quando true, TOTVS/GLPI/Graph usam implementações de desenvolvimento.", "boolean", false, "true", 1),

        new(AppSettingKeys.TotvsBaseUrl, "totvs", "TOTVS — base URL",
            "URL base da API REST TOTVS.", "string", false, "", 1),
        new(AppSettingKeys.TotvsApiKey, "totvs", "TOTVS — API key",
            "Chave de autenticação TOTVS.", "secret", true, "", 2),

        new(AppSettingKeys.GlpiBaseUrl, "glpi", "GLPI — base URL",
            "URL base da API REST GLPI.", "string", false, "", 1),
        new(AppSettingKeys.GlpiAppToken, "glpi", "GLPI — app token",
            "App token GLPI.", "secret", true, "", 2),
        new(AppSettingKeys.GlpiUserToken, "glpi", "GLPI — user token",
            "User token GLPI.", "secret", true, "", 3),

        new(AppSettingKeys.GraphTenantId, "graph", "Microsoft Graph — tenant ID",
            "Tenant da app registration Graph.", "string", false, "", 1),
        new(AppSettingKeys.GraphClientId, "graph", "Microsoft Graph — client ID",
            "Client ID da app registration.", "string", false, "", 2),
        new(AppSettingKeys.GraphClientSecret, "graph", "Microsoft Graph — client secret",
            "Secret da app registration.", "secret", true, "", 3),

        new(AppSettingKeys.WorkersTotvsSyncIntervalMinutes, "workers", "Intervalo sync TOTVS (minutos)",
            "Frequência do worker de sincronização TOTVS.", "integer", false, "30", 1),
        new(AppSettingKeys.WorkersGraphSyncIntervalMinutes, "workers", "Intervalo sync Graph (minutos)",
            "Frequência do worker de sincronização Graph.", "integer", false, "60", 2),

        new(AppSettingKeys.SerilogDefaultLevel, "serilog", "Nível mínimo de log",
            "Default Serilog (Information, Debug, Warning…).", "string", false, "Information", 1),

        new(AppSettingKeys.MediaComunicadosRootPath, "media", "Comunicados — pasta de armazenamento",
            "Caminho relativo ao ContentRoot ou absoluto para uploads de imagens de destaque.", "string", false,
            "App_Data/media/comunicados", 1),
        new(AppSettingKeys.MediaComunicadosMaxSizeBytes, "media", "Comunicados — tamanho máximo (bytes)",
            "Limite por arquivo enviado no editor de comunicados.", "integer", false, "5242880", 2),
        new(AppSettingKeys.MediaComunicadosAllowedContentTypes, "media", "Comunicados — tipos MIME permitidos",
            "Lista JSON de content-types aceitos no upload.", "json", false,
            "[\"image/jpeg\",\"image/png\",\"image/webp\"]", 3),

        new(AppSettingKeys.ObservabilityOtelEnabled, "observability", "OpenTelemetry habilitado",
            "Exporta traces/metrics via OTLP e endpoint Prometheus.", "boolean", false, "true", 1),
        new(AppSettingKeys.ObservabilityOtelServiceName, "observability", "OTel — service name",
            "Resource service.name nos spans e métricas.", "string", false, "LioConecta.Api", 2),
        new(AppSettingKeys.ObservabilityOtelOtlpEndpoint, "observability", "OTel — OTLP endpoint",
            "URL gRPC do collector Tempo (ex.: http://localhost:4317).", "string", false, "http://localhost:4317", 3),
        new(AppSettingKeys.ObservabilityOtelPrometheusEnabled, "observability", "OTel — Prometheus /metrics",
            "Expõe endpoint /metrics para scrape.", "boolean", false, "true", 4),
        new(AppSettingKeys.ObservabilityOtelTraceSampleRatio, "observability", "OTel — trace sample ratio",
            "Proporção de traces amostrados (0–1).", "string", false, "1.0", 5),

        new(AppSettingKeys.ObservabilityAccessAuditEnabled, "observability", "Access audit habilitado",
            "Registra GET sensíveis e negações de autorização.", "boolean", false, "true", 10),
        new(AppSettingKeys.ObservabilityAccessAuditRoutePatterns, "observability", "Access audit — rotas GET",
            "JSON array { method, pattern, eventName }.", "json", false,
            "[{\"method\":\"GET\",\"pattern\":\"/api/v1/admin/**\",\"eventName\":\"Resource.Viewed\"},{\"method\":\"GET\",\"pattern\":\"/api/v1/rh/**\",\"eventName\":\"Resource.Viewed\"}]",
            11),

        new(AppSettingKeys.ObservabilityPageViewsEnabled, "observability", "Page views habilitado",
            "Aceita batch POST /telemetry/page-views.", "boolean", false, "true", 20),
        new(AppSettingKeys.ObservabilityAuthAuditEnabled, "observability", "Auth audit habilitado",
            "Registra login/logout/falhas JWT.", "boolean", false, "true", 21),

        new(AppSettingKeys.ObservabilityRetentionObservabilityDays, "observability", "Retenção observability (dias)",
            "Purge de observability_events.", "integer", false, "90", 30),
        new(AppSettingKeys.ObservabilityRetentionPageViewDays, "observability", "Retenção page views (dias)",
            "Purge de page_views.", "integer", false, "180", 31),
        new(AppSettingKeys.ObservabilityRetentionAccessEventDays, "observability", "Retenção access events (dias)",
            "Purge de access_events.", "integer", false, "365", 32),
        new(AppSettingKeys.ObservabilityRetentionAggregatesDays, "observability", "Retenção agregados (dias)",
            "Purge de observability_daily_aggregates.", "integer", false, "730", 33),
        new(AppSettingKeys.ObservabilityRetentionEnabled, "observability", "Retenção automática habilitada",
            "Job diário de purge conforme dias configurados.", "boolean", false, "true", 34),
        new(AppSettingKeys.ObservabilityPrivacyIpMode, "observability", "Modo IP (full/hash/both)",
            "O que persistir em eventos de acesso.", "string", false, "both", 35),

        ..BenefitPortalSettingCatalog.ToAppSettingDefinitions(),
        ..LeavePortalSettingCatalog.ToAppSettingDefinitions(),
    ];
}
