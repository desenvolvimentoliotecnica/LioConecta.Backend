namespace LioConecta.Infrastructure.Integrations.Glpi;

internal static class GlpiSearchFields
{
    public const int TicketId = 2;
    public const int TicketTitle = 1;
    public const int TicketStatus = 12;
    public const int TicketPriority = 3;
    public const int TicketDateOpening = 15;
    public const int TicketDateMod = 19;
    public const int TicketContent = 21;
    public const int TicketRequester = 4;

    public const int UserId = 2;
    public const int UserEmail = 5;
    public const int UserLogin = 1;

    public const int UserEmailRecordEmail = 1;
    public const int UserEmailRecordUserId = 2;

    public const int ItilCategoryId = 2;
    public const int ItilCategoryName = 1;
    public const int ItilCategoryCompleteName = 13;
}

internal static class GlpiStatusMapper
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal)
    {
        ["1"] = "Novo",
        ["2"] = "Em atendimento (atribuído)",
        ["3"] = "Em atendimento (planejado)",
        ["4"] = "Pendente",
        ["5"] = "Resolvido",
        ["6"] = "Fechado",
        ["10"] = "Aprovação",
    };

    private static readonly Dictionary<string, string> PriorityLabels = new(StringComparer.Ordinal)
    {
        ["1"] = "Muito baixa",
        ["2"] = "Baixa",
        ["3"] = "Média",
        ["4"] = "Alta",
        ["5"] = "Muito alta",
        ["6"] = "Major",
    };

    public static string StatusLabel(string? statusCode) =>
        statusCode is not null && Labels.TryGetValue(statusCode, out var label) ? label : statusCode ?? "—";

    public static string PriorityLabel(string? priorityCode) =>
        priorityCode is not null && PriorityLabels.TryGetValue(priorityCode, out var label) ? label : priorityCode ?? "—";

    public static bool IsOpenStatus(string? statusCode) =>
        statusCode is "1" or "2" or "3" or "4" or "10";
}
