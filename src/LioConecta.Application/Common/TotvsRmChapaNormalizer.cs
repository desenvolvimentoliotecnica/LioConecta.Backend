namespace LioConecta.Application.Common;

public static class TotvsRmConstants
{
    public const short CodColigada = 1;
    public const int ChapaLength = 8;
    public const string PunchTableName = "ABATFUN";
    public const string ProcessedDayTableName = "AAFHTFUN";
    public const string PayrollFinanceTableName = "PFFINANC";
    public const string PayrollEventTableName = "PEVENTO";
    public const string PayrollPeriodTableName = "PFPERFF";

    /// <summary>Histórico de função (Corpore padrão). Algumas bases usam PFHSTFCO.</summary>
    public const string FunctionHistoryTableName = "PFHSTFUN";

    public const string FunctionHistoryFallbackTableName = "PFHSTFCO";

    public const string SectionHistoryTableName = "PFHSTSEC";

    public const string SalaryHistoryTableName = "PFHSTSAL";
}

public static class TotvsRmChapaNormalizer
{
    public static string? Normalize(string? employeeId)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return null;
        }

        return employeeId.Trim().PadLeft(TotvsRmConstants.ChapaLength, '0');
    }
}

public abstract class TotvsRmIntegrationException : Exception
{
    protected TotvsRmIntegrationException(string message)
        : base(message)
    {
    }
}

public sealed class TotvsRmIntegrationDisabledException : TotvsRmIntegrationException
{
    public TotvsRmIntegrationDisabledException()
        : base("Integracao TOTVS RM desabilitada.")
    {
    }
}

public sealed class TotvsRmIntegrationMisconfiguredException : TotvsRmIntegrationException
{
    public TotvsRmIntegrationMisconfiguredException(string message)
        : base(message)
    {
    }
}

public sealed class TotvsRmIntegrationUnavailableException : TotvsRmIntegrationException
{
    public TotvsRmIntegrationUnavailableException()
        : base("Integracao TOTVS RM indisponivel.")
    {
    }
}
