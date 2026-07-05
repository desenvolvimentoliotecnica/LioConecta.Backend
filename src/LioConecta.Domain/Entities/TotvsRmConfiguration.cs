using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class TotvsRmConfiguration : BaseEntity
{
    public bool IsEnabled { get; set; }

    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 1433;

    public string Database { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string? PasswordProtected { get; set; }

    public bool TrustServerCertificate { get; set; } = true;

    public int TimesheetPeriodStartDay { get; set; } = 16;

    public int TimesheetPeriodEndDay { get; set; } = 15;
}
