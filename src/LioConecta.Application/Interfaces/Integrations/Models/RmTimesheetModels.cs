namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class RmPunchRecord
{
    public DateTime DataPonto { get; init; }

    public int BatidaMinutos { get; init; }

    public int Natureza { get; init; }

    public string? DescricaoNatureza { get; init; }

    public string? Status { get; init; }

    public string? CodigoRelogio { get; init; }
}

public sealed class RmProcessedDayRecord
{
    public DateTime DataPonto { get; init; }

    public int? WorkedMinutes { get; init; }

    public int? ExpectedMinutes { get; init; }

    public int? BalanceMinutes { get; init; }

    public int? DelayMinutes { get; init; }

    public int? AbsenceMinutes { get; init; }

    public string? StatusCode { get; init; }
}
