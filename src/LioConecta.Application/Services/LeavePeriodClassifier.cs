namespace LioConecta.Application.Services;

/// <summary>
/// Classifica períodos de férias do RM para o colaborador leigo:
/// liberado (pode solicitar), em aquisição (ainda não) e vencido.
/// </summary>
public static class LeavePeriodClassifier
{
    public const string StatusLiberado = "liberado";
    public const string StatusEmAquisicao = "em_aquisicao";
    public const string StatusVencido = "vencido";

    public static string Classify(DateOnly? fimAquisitivo, DateOnly? vencimento, DateOnly today)
    {
        if (fimAquisitivo is null || fimAquisitivo.Value > today)
        {
            return StatusEmAquisicao;
        }

        if (vencimento is not null && vencimento.Value < today)
        {
            return StatusVencido;
        }

        return StatusLiberado;
    }

    public static string? BuildContextNote(
        string status,
        int saldoDias,
        DateOnly? fimAquisitivo,
        DateOnly? vencimento)
    {
        if (saldoDias <= 0)
        {
            return null;
        }

        return status switch
        {
            StatusEmAquisicao when fimAquisitivo is not null =>
                $"Você completa o período aquisitivo em {fimAquisitivo.Value:dd/MM/yyyy}. A partir daí poderá solicitar estes {saldoDias} dia(s).",
            StatusEmAquisicao =>
                $"Estes {saldoDias} dia(s) ainda estão em aquisição e não podem ser solicitados agora.",
            StatusVencido when vencimento is not null =>
                $"Saldo de {saldoDias} dia(s) com vencimento em {vencimento.Value:dd/MM/yyyy} — consulte o RH.",
            StatusVencido =>
                $"Saldo de {saldoDias} dia(s) vencido — consulte o RH.",
            StatusLiberado when vencimento is not null =>
                $"{saldoDias} dia(s) liberados para gozo (vencem em {vencimento.Value:dd/MM/yyyy}).",
            StatusLiberado =>
                $"{saldoDias} dia(s) liberados para solicitação.",
            _ => null,
        };
    }
}
