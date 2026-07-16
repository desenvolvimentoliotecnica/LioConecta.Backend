using System.Globalization;
using System.Net;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public interface IPontoEmailNotifier
{
    Task NotifyRequestCreatedAsync(
        PontoAdjustmentRecord record,
        Person requester,
        IReadOnlyList<Person> recipients,
        CancellationToken cancellationToken = default);

    Task NotifyDecisionAsync(
        PontoAdjustmentRecord record,
        Person requester,
        bool approved,
        string? decisionNote,
        CancellationToken cancellationToken = default);
}

public sealed class PontoEmailNotifier(
    IEmailQueueService emailQueueService,
    IAppSettingsProvider settingsProvider) : IPontoEmailNotifier
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task NotifyRequestCreatedAsync(
        PontoAdjustmentRecord record,
        Person requester,
        IReadOnlyList<Person> recipients,
        CancellationToken cancellationToken = default)
    {
        if (!settingsProvider.GetBool(AppSettingKeys.PontoEmailEnabled, true))
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

        var protocol = $"LC-{record.Id.ToString("N")[..8].ToUpperInvariant()}";
        var deepLink = $"/servicos/ponto-eletronico/gestao?requestId={record.Id}";
        var dayLabel = FormatDayLabel(record.DayCount);
        var subject = $"Nova solicitação de ajuste de ponto — {requester.Name}";
        var bodyText =
            $"O colaborador {requester.Name} ({requester.EmployeeId ?? requester.Email}) solicitou ajuste de ponto.\n"
            + $"Dias: {dayLabel}\n"
            + $"Motivo: {record.Reason}\n"
            + $"Chapa/matrícula: {requester.EmployeeId ?? "—"}\n"
            + $"Protocolo: {protocol}\n\n"
            + $"Acompanhe no portal:\n{deepLink}\n";
        var bodyHtml =
            $"<p>O colaborador <strong>{WebUtility.HtmlEncode(requester.Name)}</strong> "
            + $"({WebUtility.HtmlEncode(requester.EmployeeId ?? requester.Email)}) solicitou ajuste de ponto.</p>"
            + $"<ul><li><strong>Dias:</strong> {WebUtility.HtmlEncode(dayLabel)}</li>"
            + $"<li><strong>Motivo:</strong> {WebUtility.HtmlEncode(record.Reason)}</li>"
            + $"<li><strong>Chapa/matrícula:</strong> {WebUtility.HtmlEncode(requester.EmployeeId ?? "—")}</li>"
            + $"<li><strong>Protocolo:</strong> {WebUtility.HtmlEncode(protocol)}</li></ul>"
            + $"<p>Acompanhe no portal: "
            + $"<a href=\"{WebUtility.HtmlEncode(deepLink)}\">{WebUtility.HtmlEncode(deepLink)}</a></p>";

        var to = realEmails;
        var overrideEnabled = settingsProvider.GetBool(AppSettingKeys.PontoEmailDevOverrideEnabled, true);
        var overrideTo = settingsProvider.GetString(
            AppSettingKeys.PontoEmailDevOverrideTo,
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
            source = "ponto.adjustment.created",
            pontoAdjustmentId = record.Id,
            protocol,
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

    public async Task NotifyDecisionAsync(
        PontoAdjustmentRecord record,
        Person requester,
        bool approved,
        string? decisionNote,
        CancellationToken cancellationToken = default)
    {
        if (!settingsProvider.GetBool(AppSettingKeys.PontoEmailEnabled, true))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(requester.Email))
        {
            return;
        }

        var realEmails = new List<string> { requester.Email.Trim() };
        var protocol = $"LC-{record.Id.ToString("N")[..8].ToUpperInvariant()}";
        var deepLink = $"/servicos/ponto-eletronico?requestId={record.Id}";
        var dayLabel = FormatDayLabel(record.DayCount);
        var decisionLabel = approved ? "aprovada" : "rejeitada";
        var subject = approved
            ? $"Ajuste de ponto aprovado — {dayLabel}"
            : $"Ajuste de ponto rejeitado — {dayLabel}";

        var noteLine = string.IsNullOrWhiteSpace(decisionNote)
            ? string.Empty
            : approved
                ? $"Comentário: {decisionNote.Trim()}\n"
                : $"Motivo: {decisionNote.Trim()}\n";
        var noteHtml = string.IsNullOrWhiteSpace(decisionNote)
            ? string.Empty
            : approved
                ? $"<li><strong>Comentário:</strong> {WebUtility.HtmlEncode(decisionNote.Trim())}</li>"
                : $"<li><strong>Motivo:</strong> {WebUtility.HtmlEncode(decisionNote.Trim())}</li>";

        var bodyText =
            $"Solicitação de ajuste de ponto {decisionLabel}.\n"
            + $"Dias: {dayLabel}\n"
            + $"Protocolo: {protocol}\n"
            + noteLine
            + $"\nAcompanhe no portal:\n{deepLink}\n";
        var bodyHtml =
            $"<p><strong>Solicitação de ajuste de ponto</strong> {WebUtility.HtmlEncode(decisionLabel)}.</p>"
            + $"<ul><li><strong>Dias:</strong> {WebUtility.HtmlEncode(dayLabel)}</li>"
            + $"<li><strong>Protocolo:</strong> {WebUtility.HtmlEncode(protocol)}</li>"
            + noteHtml
            + $"</ul><p>Acompanhe no portal: "
            + $"<a href=\"{WebUtility.HtmlEncode(deepLink)}\">{WebUtility.HtmlEncode(deepLink)}</a></p>";

        var to = realEmails;
        var overrideEnabled = settingsProvider.GetBool(AppSettingKeys.PontoEmailDevOverrideEnabled, true);
        var overrideTo = settingsProvider.GetString(
            AppSettingKeys.PontoEmailDevOverrideTo,
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

        var source = approved ? "ponto.adjustment.approved" : "ponto.adjustment.rejected";
        var metadata = JsonSerializer.Serialize(new
        {
            source,
            pontoAdjustmentId = record.Id,
            protocol,
            approved,
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

    private static string FormatDayLabel(int dayCount) =>
        dayCount == 1 ? "1 dia" : $"{dayCount.ToString(PtBr)} dias";
}
