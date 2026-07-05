using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Services;

public static class PayslipRmMapper
{
    public static string MapPaymentTypeLabel(RmPayslipSummaryRecord summary)
    {
        if (summary.HasAdvanceEvent && !summary.HasPayrollEvents)
        {
            return "ADIANTAMENTO";
        }

        if (summary.NroPeriodo > 1 && !summary.HasPayrollEvents)
        {
            return "ADIANTAMENTO";
        }

        if (summary.DeductionAmount <= 0.01m &&
            summary.GrossAmount > 0m &&
            summary.NetAmount >= summary.GrossAmount - 0.01m)
        {
            return "ADIANTAMENTO";
        }

        if (summary.PaymentDate?.Day is int paymentDay &&
            paymentDay <= 20 &&
            summary.DeductionAmount <= 0.01m)
        {
            return "ADIANTAMENTO";
        }

        return "FOLHA";
    }

    public static bool IsAdvanceLine(RmPayslipLineRecord line)
    {
        var code = line.Code.Trim();
        if (code is "401" or "0401")
        {
            return true;
        }

        return !line.IsDeduction &&
               line.Description.Contains("ADIANTAMENTO", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInformationalEarningLine(RmPayslipLineRecord line)
    {
        if (line.IsDeduction)
        {
            return false;
        }

        if (string.Equals(line.ProvisionType, "B", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var code = line.Code.Trim();
        var description = line.Description.Trim();

        if (code is "9999" or "0092")
        {
            return true;
        }

        if (description.StartsWith("BS ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (description.Contains("INSS com Aliquota", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (description.Contains("Requisicao Interna", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (description.Contains("SENAI", StringComparison.OrdinalIgnoreCase)
            || description.Contains("SESI", StringComparison.OrdinalIgnoreCase)
            || description.Contains("Contr Adicional Senai", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (description.Contains("HORAS TRABALHADAS CHEIA", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsDisplayableFolhaEarningLine(RmPayslipLineRecord line) =>
        !line.IsDeduction && !IsAdvanceLine(line) && !IsInformationalEarningLine(line);

    public static IReadOnlyList<RmPayslipLineRecord> FilterFolhaDisplayLines(IReadOnlyList<RmPayslipLineRecord> lines)
    {
        var filtered = lines
            .Where(line => line.IsDeduction || IsDisplayableFolhaEarningLine(line))
            .ToList();

        return filtered.Count > 0 ? filtered : lines;
    }

    public static IReadOnlyList<RmPayslipLineRecord> FilterLinesByPaymentType(
        IReadOnlyList<RmPayslipLineRecord> lines,
        string? paymentTypeHint)
    {
        if (lines.Count == 0)
        {
            return lines;
        }

        if (paymentTypeHint?.Equals("ADIANTAMENTO", StringComparison.OrdinalIgnoreCase) == true)
        {
            var advanceLines = lines.Where(IsAdvanceLine).ToList();
            return advanceLines.Count > 0 ? advanceLines : lines;
        }

        return FilterFolhaDisplayLines(lines);
    }

    public static IEnumerable<RmPayslipSummaryRecord> EnumerateEnvelopeCandidates(
        IReadOnlyList<RmPayslipSummaryRecord> envelopes,
        int? explicitNroPeriodo,
        string? paymentTypeHint)
    {
        var yielded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var envelope in SelectEnvelopeCandidates(envelopes, explicitNroPeriodo, paymentTypeHint))
        {
            var key = $"{envelope.NroPeriodo}:{envelope.PaymentDate:yyyyMMdd}:{envelope.NetAmount}";
            if (yielded.Add(key))
            {
                yield return envelope;
            }
        }
    }

    public static int ResolveNroPeriodo(
        IReadOnlyList<RmPayslipSummaryRecord> envelopes,
        int? explicitNroPeriodo,
        string? paymentTypeHint = null)
    {
        if (!string.IsNullOrWhiteSpace(paymentTypeHint))
        {
            var typedEnvelope = envelopes.FirstOrDefault(item =>
                string.Equals(MapPaymentTypeLabel(item), paymentTypeHint, StringComparison.OrdinalIgnoreCase));
            if (typedEnvelope is not null)
            {
                return typedEnvelope.NroPeriodo;
            }
        }

        if (explicitNroPeriodo.HasValue)
        {
            return explicitNroPeriodo.Value;
        }

        if (envelopes.Count == 0)
        {
            return 1;
        }

        if (envelopes.Count == 1)
        {
            return envelopes[0].NroPeriodo;
        }

        var folha = envelopes.FirstOrDefault(item => MapPaymentTypeLabel(item) == "FOLHA");
        return folha?.NroPeriodo ?? envelopes[0].NroPeriodo;
    }

    public static void NormalizePayslipPeriod(RmPayslipPeriodRecord? period)
    {
        if (period is null)
        {
            return;
        }

        period.FgtsAmount = ResolveFgtsAmount(period.BaseFgts, period.FgtsAmount);
    }

    public static decimal ResolveFgtsAmount(decimal baseFgts, decimal fgtsAmount)
    {
        if (fgtsAmount > 0m)
        {
            return fgtsAmount;
        }

        if (baseFgts <= 0m)
        {
            return 0m;
        }

        return Math.Truncate(baseFgts * 0.08m * 100m) / 100m;
    }

    public static string MaskCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return "—";
        }

        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
        {
            return cpf;
        }

        return $"***.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-**";
    }

    public static string FormatAdmissionDate(DateTime? date) =>
        date?.ToString("dd/MM/yyyy") ?? "—";

    public static string BuildPeriodLabel(int anoComp, int mesComp)
    {
        var monthNames = new[]
        {
            "", "Janeiro", "Fevereiro", "Marco", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        };

        return mesComp is >= 1 and <= 12
            ? $"{monthNames[mesComp]}/{anoComp}"
            : $"{mesComp:D2}/{anoComp}";
    }

    private static IEnumerable<RmPayslipSummaryRecord> SelectEnvelopeCandidates(
        IReadOnlyList<RmPayslipSummaryRecord> envelopes,
        int? explicitNroPeriodo,
        string? paymentTypeHint)
    {
        if (!string.IsNullOrWhiteSpace(paymentTypeHint))
        {
            foreach (var envelope in envelopes.Where(item =>
                         string.Equals(MapPaymentTypeLabel(item), paymentTypeHint, StringComparison.OrdinalIgnoreCase)))
            {
                yield return envelope;
            }

            yield break;
        }

        if (explicitNroPeriodo.HasValue)
        {
            var match = envelopes.FirstOrDefault(item => item.NroPeriodo == explicitNroPeriodo.Value);
            if (match is not null)
            {
                yield return match;
            }

            yield break;
        }

        foreach (var envelope in envelopes)
        {
            yield return envelope;
        }
    }
}
