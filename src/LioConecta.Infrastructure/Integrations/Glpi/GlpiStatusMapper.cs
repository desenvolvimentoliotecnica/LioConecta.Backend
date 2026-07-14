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
    /// <summary>Técnico atribuído (Ticket_User type=2).</summary>
    public const int TicketTechnician = 5;

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
        ["2"] = "Em atendimento",
        ["3"] = "Em atendimento",
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

    /// <summary>Normaliza código ou rótulo GLPI (ex.: "Pendente") para o código numérico.</summary>
    public static string NormalizeStatusCode(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var raw = status.Trim();
        if (raw.StartsWith('"') && raw.EndsWith('"') && raw.Length >= 2)
        {
            raw = raw[1..^1].Trim();
        }

        if (Labels.ContainsKey(raw))
        {
            return raw;
        }

        // Alguns ambientes retornam o texto do status no search.
        var lower = raw.ToLowerInvariant();
        if (lower is "novo" or "new") return "1";
        if (lower.Contains("atribu") || lower.Contains("assign") || lower is "processing (assigned)") return "2";
        if (lower.Contains("planej") || lower.Contains("planned") || lower is "processing (planned)") return "3";
        if (lower is "pendente" or "pending" || lower.Contains("waiting")) return "4";
        if (lower is "resolvido" or "solved" or "resolved") return "5";
        if (lower is "fechado" or "closed") return "6";
        if (lower.Contains("aprova") || lower.Contains("approval")) return "10";
        if (lower.Contains("atendimento") || lower.Contains("processing")) return "2";

        return raw;
    }

    public static string StatusLabel(string? statusCode)
    {
        var code = NormalizeStatusCode(statusCode);
        return Labels.TryGetValue(code, out var label) ? label : statusCode ?? "—";
    }

    public static string PriorityLabel(string? priorityCode) =>
        priorityCode is not null && PriorityLabels.TryGetValue(priorityCode.Trim(), out var label)
            ? label
            : priorityCode ?? "—";

    public static bool IsOpenStatus(string? statusCode)
    {
        var code = NormalizeStatusCode(statusCode);
        return code is "1" or "2" or "3" or "4" or "10";
    }

    /// <summary>Fila aguardando (Novo, Pendente, Aprovação).</summary>
    public static bool IsPendingQueueStatus(string? statusCode)
    {
        var code = NormalizeStatusCode(statusCode);
        return code is "1" or "4" or "10";
    }

    /// <summary>Em atendimento (atribuído ou planejado).</summary>
    public static bool IsInProgressStatus(string? statusCode)
    {
        var code = NormalizeStatusCode(statusCode);
        return code is "2" or "3";
    }
}
