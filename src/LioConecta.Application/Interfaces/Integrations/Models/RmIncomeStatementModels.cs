namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class RmIncomeStatementLineRecord
{
    public int MesComp { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalWithheld { get; set; }
}

public sealed class RmFgtsDepositRecord
{
    public int AnoComp { get; set; }

    public int MesComp { get; set; }

    public decimal FgtsAmount { get; set; }

    public decimal BaseFgts { get; set; }
}
