namespace LioConecta.Application.Common.DbExplorer;

public static class DbExplorerCatalog
{
    public const string PostgresConnectionId = "postgres";
    public const string TotvsRmConnectionId = "totvs-rm";

    public static readonly IReadOnlySet<string> DefaultBlockedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "app_settings",
        "public.app_settings",
        "portal_users",
        "public.portal_users",
    };

    public static readonly IReadOnlyList<string> KnownConnectionIds =
    [
        PostgresConnectionId,
        TotvsRmConnectionId,
    ];
}
