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

        // "D" (Programado/deferido) — SITUACAOFERIAS real do PFUFERIASPER (ver docs/spike-writeback-sql-rm.md).
        if (normalized is "A" or "G" or "D" or "APROV" or "APROVADO" or "PROGRAMADO")
        {
            return startDate is not null && startDate.Value > today ? "approved" : "completed";
        }

        // "P" (Pendente/em programação) — write-back Onda 1B insere férias com esse código.
        if (normalized is "P" or "PEND" or "PENDENTE")
        {
            return "pending";
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
