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
    public const string AuthProvider = "auth.provider";
    public const string AuthJwtSigningKey = "auth.jwt_signing_key";
    public const string AuthJwtExpiryMinutes = "auth.jwt_expiry_minutes";
    public const string AuthSuperAdminEmails = "auth.super_admin_emails";

    public const string LdapEnabled = "ldap.enabled";
    public const string LdapHost = "ldap.host";
    public const string LdapPort = "ldap.port";
    public const string LdapUseSsl = "ldap.use_ssl";
    public const string LdapBindDn = "ldap.bind_dn";
    public const string LdapBindPassword = "ldap.bind_password";
    public const string LdapSearchBase = "ldap.search_base";
    public const string LdapUserFilter = "ldap.user_filter";
    public const string LdapDomain = "ldap.domain";

    public const string CorsAllowedOrigins = "cors.allowed_origins";

    public const string IntegrationsUseDevAdapters = "integrations.use_dev_adapters";

    public const string TotvsBaseUrl = "totvs.base_url";
    public const string TotvsApiKey = "totvs.api_key";

    public const string GlpiBaseUrl = "glpi.base_url";
    public const string GlpiPortalUrl = "glpi.portal_url";
    public const string GlpiAppToken = "glpi.app_token";
    public const string GlpiUserToken = "glpi.user_token";
    public const string GlpiProfileId = "glpi.profile_id";
    public const string HelpDeskGlpiAreas = "helpdesk.glpi_areas";

    public const string GraphTenantId = "graph.tenant_id";
    public const string GraphClientId = "graph.client_id";
    public const string GraphClientSecret = "graph.client_secret";
    public const string GraphDirectoryLastSyncUtc = "graph.directory_last_sync_utc";

    public const string ChatTeamsEnabled = "chat.teams.enabled";
    public const string ChatTeamsAuthMode = "chat.teams.auth_mode";
    public const string ChatTeamsDelegatedScopes = "chat.teams.delegated_scopes";
    public const string ChatTeamsIncludeGroupChats = "chat.teams.include_group_chats";
    public const string ChatTeamsPollingIntervalSeconds = "chat.teams.polling_interval_seconds";
    public const string ChatTeamsSignalREnabled = "chat.teams.signalr_enabled";
    public const string ChatTeamsTokenEncryptionKey = "chat.teams.token_encryption_key";
    public const string ChatTeamsLastTestUtc = "chat.teams.last_test_utc";
    public const string ChatTeamsLastTestMessage = "chat.teams.last_test_message";

    public const string PlannerEnabled = "planner.enabled";
    public const string PlannerPlanId = "planner.plan_id";
    public const string PlannerDefaultBucketId = "planner.default_bucket_id";
    public const string PlannerPlanTitle = "planner.plan_title";
    public const string PlannerLastSyncUtc = "planner.last_sync_utc";

    public const string CalendarEnabled = "calendar.enabled";
    public const string CalendarDelegatedScopes = "calendar.delegated_scopes";
    public const string CalendarDefaultView = "calendar.default_view";
    public const string CalendarShowBirthdays = "calendar.show_birthdays";
    public const string CalendarShowCafeteriaMenu = "calendar.show_cafeteria_menu";
    public const string CalendarMaxParallelCalendars = "calendar.max_parallel_calendars";
    public const string CalendarTokenEncryptionKey = "calendar.token_encryption_key";
    public const string CalendarLastTestUtc = "calendar.last_test_utc";
    public const string CalendarLastTestMessage = "calendar.last_test_message";

    public const string WorkersTotvsSyncIntervalMinutes = "workers.totvs_sync_interval_minutes";
    public const string WorkersGraphSyncIntervalMinutes = "workers.graph_sync_interval_minutes";
    public const string WorkersGraphDirectorySyncIntervalMinutes = "workers.graph_directory_sync_interval_minutes";
    public const string WorkersTotvsTimesheetSyncIntervalMinutes = "workers.totvs_timesheet_sync_interval_minutes";
    public const string WorkersTotvsTimesheetCacheTtlMinutes = "workers.totvs_timesheet_cache_ttl_minutes";
    public const string WorkersTotvsPayslipSyncIntervalMinutes = "workers.totvs_payslip_sync_interval_minutes";
    public const string WorkersTotvsPayslipCacheTtlMinutes = "workers.totvs_payslip_cache_ttl_minutes";
    public const string WorkersTotvsLeaveSyncIntervalMinutes = "workers.totvs_leave_sync_interval_minutes";
    public const string WorkersTotvsLeaveCacheTtlMinutes = "workers.totvs_leave_cache_ttl_minutes";
    public const string LeaveRmWriteBackEnabled = "leave.rm.writeback.enabled";
    public const string LeaveNotifyEmails = "leave.notify_emails";
    public const string LeaveNotifyRoles = "leave.notify_roles";
    public const string LeaveEmailEnabled = "leave.email.enabled";
    public const string LeaveEmailDevOverrideEnabled = "leave.email.dev_override_enabled";
    public const string LeaveEmailDevOverrideTo = "leave.email.dev_override_to";
    public const string WorkersPollClosureIntervalMinutes = "workers.poll_closure_interval_minutes";

    public const string SerilogDefaultLevel = "serilog.default_level";

    public const string MediaComunicadosRootPath = "media.comunicados.root_path";
    public const string MediaComunicadosMaxSizeBytes = "media.comunicados.max_size_bytes";
    public const string MediaComunicadosAllowedContentTypes = "media.comunicados.allowed_content_types";

    public const string MediaPostsRootPath = "media.posts.root_path";
    public const string MediaPostsMaxSizeBytes = "media.posts.max_size_bytes";
    public const string MediaPostsAllowedContentTypes = "media.posts.allowed_content_types";

    public const string MediaPeopleRootPath = "media.people.root_path";

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

    public const string FacilitiesMenuAllowedRoles = "facilities.menu.allowed_roles";
    public const string FacilitiesMenuAllowedEmails = "facilities.menu.allowed_emails";
    public const string FacilitiesMenuEmailRecipients = "facilities.menu.email_recipients";

    public const string RamaisAllowedRoles = "ramais.allowed_roles";
    public const string RamaisAllowedEmails = "ramais.allowed_emails";

    public const string LoopEnabled = "loop.enabled";
    public const string LoopAllowedRoles = "loop.allowed_roles";
    public const string LoopAllowedEmails = "loop.allowed_emails";

    public const string CompassEnabled = "compass.enabled";
    public const string CompassAllowedRoles = "compass.allowed_roles";
    public const string CompassAllowedEmails = "compass.allowed_emails";

    public const string PortalUiMaturityBadgesEnabled = "portal.ui.maturity_badges_enabled";
    public const string PortalUiMaturityRoadmap = "portal.ui.maturity_roadmap";
}
