using System.Globalization;
using System.Net;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public interface ILeaveEmailNotifier
{
    Task NotifyRequestCreatedAsync(
        LeaveRecord record,
        Person requester,
        IReadOnlyList<Person> recipients,
        CancellationToken cancellationToken = default);
}

public sealed class LeaveEmailNotifier(
    IEmailQueueService emailQueueService,
    IAppSettingsProvider settingsProvider) : ILeaveEmailNotifier
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task NotifyRequestCreatedAsync(
        LeaveRecord record,
        Person requester,
        IReadOnlyList<Person> recipients,
        CancellationToken cancellationToken = default)
    {
        if (!settingsProvider.GetBool(AppSettingKeys.LeaveEmailEnabled, true))
        {
            return;
        }

        var realEmails = recipients
            .Select(r => r.Email)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (realEmails.Count == 0)
        {
            return;
        }

        var deepLink = $"/servicos/ferias-ausencias/gestao?requestId={record.Id}";
        var period = FormatPeriod(record.StartDate, record.EndDate);
        var days = record.Days?.ToString(PtBr) ?? "—";
        var subject = $"Nova solicitação de férias — {requester.Name}";
        var bodyText =
            $"O colaborador {requester.Name} ({requester.EmployeeId ?? requester.Email}) solicitou férias.\n"
            + $"Período: {period}\n"
            + $"Dias: {days}\n"
            + $"Chapa/matrícula: {requester.EmployeeId ?? "—"}\n"
            + $"Protocolo portal: {record.Id}\n\n"
            + $"Acompanhe no portal (aprovação formal no RM Labore):\n{deepLink}\n";
        var bodyHtml =
            $"<p>O colaborador <strong>{WebUtility.HtmlEncode(requester.Name)}</strong> "
            + $"({WebUtility.HtmlEncode(requester.EmployeeId ?? requester.Email)}) solicitou férias.</p>"
            + $"<ul><li><strong>Período:</strong> {WebUtility.HtmlEncode(period)}</li>"
            + $"<li><strong>Dias:</strong> {WebUtility.HtmlEncode(days)}</li>"
            + $"<li><strong>Chapa/matrícula:</strong> {WebUtility.HtmlEncode(requester.EmployeeId ?? "—")}</li>"
            + $"<li><strong>Protocolo portal:</strong> {record.Id}</li></ul>"
            + $"<p>Acompanhe no portal (aprovação formal no RM Labore): "
            + $"<a href=\"{WebUtility.HtmlEncode(deepLink)}\">{WebUtility.HtmlEncode(deepLink)}</a></p>";

        var to = realEmails;
        var overrideEnabled = settingsProvider.GetBool(AppSettingKeys.LeaveEmailDevOverrideEnabled, true);
        var overrideTo = settingsProvider.GetString(
            AppSettingKeys.LeaveEmailDevOverrideTo,
            "leonardo.mendes@liotecnica.com.br")?.Trim();

        if (overrideEnabled && !string.IsNullOrWhiteSpace(overrideTo))
        {
            var originals = string.Join(", ", realEmails);
            subject = $"[DEV OVERRIDE] {subject}";
            bodyText =
                $"[DEV OVERRIDE] Destinatários originais: {originals}\n\n{bodyText}";
            bodyHtml =
                $"<p><strong>[DEV OVERRIDE]</strong> Destinatários originais: "
                + $"{WebUtility.HtmlEncode(originals)}</p>{bodyHtml}";
            to = [overrideTo];
        }

        var metadata = JsonSerializer.Serialize(new
        {
            source = "leave.request.created",
            leaveRecordId = record.Id,
            originalRecipients = realEmails,
            overrideEnabled,
        });

        await emailQueueService.EnqueueAsync(
            new EmailEnqueueRequest(
                to,
                subject,
                BodyHtml: bodyHtml,
                BodyText: bodyText,
                MetadataJson: metadata,
                CorrelationId: record.Id,
                CreatedById: record.PersonId),
            cancellationToken);
    }

    private static string FormatPeriod(DateOnly? start, DateOnly? end)
    {
        if (start is null)
        {
            return "—";
        }

        var startLabel = start.Value.ToString("dd/MM/yyyy", PtBr);
        if (end is null)
        {
            return startLabel;
        }

        return $"{startLabel} – {end.Value.ToString("dd/MM/yyyy", PtBr)}";
    }
}
