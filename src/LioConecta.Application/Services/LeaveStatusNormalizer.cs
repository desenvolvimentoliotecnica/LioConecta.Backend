namespace LioConecta.Application.Services;

public static class LeaveStatusNormalizer
{
    public static string FromRm(string? rmStatus, DateOnly? startDate, DateOnly? endDate)
    {
        var normalized = (rmStatus ?? string.Empty).Trim().ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (normalized is "C" or "R" or "X" or "CANC" or "REJEITADO" or "CANCELADO")
        {
            return "rejected";
        }

        if (endDate is not null && endDate.Value < today)
        {
            return "completed";
        }

        if (normalized is "A" or "G" or "APROV" or "APROVADO" or "PROGRAMADO")
        {
            return startDate is not null && startDate.Value > today ? "approved" : "completed";
        }

        if (startDate is not null && startDate.Value > today)
        {
            return "approved";
        }

        return "pending";
    }

    public static string Label(string status) => status switch
    {
        "pending" => "Pendente",
        "approved" => "Aprovado",
        "completed" => "Concluído",
        "rejected" => "Rejeitado",
        _ => status,
    };
}
