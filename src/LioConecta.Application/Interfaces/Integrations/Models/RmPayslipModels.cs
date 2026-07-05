namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class RmPayslipSummaryRecord
{
    public int AnoComp { get; set; }

    public int MesComp { get; set; }

    public int NroPeriodo { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal NetAmount { get; set; }

    public decimal DeductionAmount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public bool HasAdvanceEvent { get; set; }

    public bool HasPayrollEvents { get; set; }
}

public sealed class RmPayslipLineRecord
{
    public int NroPeriodo { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public bool IsDeduction { get; set; }

    public string ProvisionType { get; set; } = string.Empty;
}

public sealed class RmPayslipPeriodRecord
{
    public int NroPeriodo { get; set; }

    public decimal BaseFgts { get; set; }

    public decimal BaseIrrf { get; set; }

    public decimal BaseIrPlr { get; set; }

    public decimal BaseInss { get; set; }

    public decimal FgtsAmount { get; set; }

    public decimal PensionAlimony { get; set; }

    public decimal BaseSalary { get; set; }
}
