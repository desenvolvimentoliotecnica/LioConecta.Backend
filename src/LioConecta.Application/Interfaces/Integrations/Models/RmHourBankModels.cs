namespace LioConecta.Application.Interfaces.Integrations.Models;

/// <summary>Saldo sintético do banco de horas (ASALDOBANCOHOR). Valores em minutos.</summary>
public sealed class RmHourBankBalanceRecord
{
    public string Chapa { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int ExtraPreviousMinutes { get; set; }
    public int ExtraCurrentMinutes { get; set; }
    public int DelayPreviousMinutes { get; set; }
    public int DelayCurrentMinutes { get; set; }
    public int AbsencePreviousMinutes { get; set; }
    public int AbsenceCurrentMinutes { get; set; }

    public int BalanceMinutes =>
        ExtraPreviousMinutes + ExtraCurrentMinutes
        - DelayPreviousMinutes - DelayCurrentMinutes
        - AbsencePreviousMinutes - AbsenceCurrentMinutes;
}

/// <summary>Movimento diário (ABANCOHORFUN). Valores em minutos.</summary>
public sealed class RmHourBankDayRecord
{
    public DateTime Date { get; set; }
    public int ExtraMinutes { get; set; }
    public int DelayMinutes { get; set; }
    public int AbsenceMinutes { get; set; }
}
