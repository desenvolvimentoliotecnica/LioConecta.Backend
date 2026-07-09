using System.Globalization;
using System.Net;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public interface IUniLioEmailNotifier
{
    Task NotifyCourseSubmittedAsync(
        UniLioCourse course,
        Person submitter,
        IReadOnlyList<Person> approvers,
        CancellationToken cancellationToken = default);
}

public sealed class UniLioEmailNotifier(
    IEmailQueueService emailQueueService,
    IAppSettingsProvider settingsProvider) : IUniLioEmailNotifier
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task NotifyCourseSubmittedAsync(
        UniLioCourse course,
        Person submitter,
        IReadOnlyList<Person> approvers,
        CancellationToken cancellationToken = default)
    {
        if (!settingsProvider.GetBool(AppSettingKeys.UniLioEmailEnabled, true))
        {
            return;
        }

        var realEmails = approvers
            .Select(r => r.Email)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (realEmails.Count == 0)
        {
            return;
        }

        var baseUrl = settingsProvider.GetString(AppSettingKeys.UniLioEmailPublicBaseUrl, "")?.Trim().TrimEnd('/') ?? "";
        var path = $"/unilio/admin/aprovacoes/{course.Id}";
        var deepLink = string.IsNullOrEmpty(baseUrl) ? path : $"{baseUrl}{path}";
        var subject = $"[UniLio] Curso aguardando aprovação: {course.Title}";
        var submitterLabel = WebUtility.HtmlEncode(submitter.Name);
        var title = WebUtility.HtmlEncode(course.Title);
        var excerpt = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(course.Description)
                ? "Sem descrição."
                : course.Description.Trim());

        var bodyText =
            $"{submitter.Name} enviou o curso \"{course.Title}\" para aprovação.\n\n"
            + $"Área: {course.Area}\n"
            + $"Duração: {course.DurationMinutes} min\n"
            + $"Obrigatório: {(course.IsMandatory ? "Sim" : "Não")}\n\n"
            + $"Revisar e aprovar:\n{deepLink}\n";

        var bodyHtml =
            $"<p><strong>{submitterLabel}</strong> enviou o curso <strong>{title}</strong> para aprovação.</p>"
            + "<ul>"
            + $"<li><strong>Área:</strong> {WebUtility.HtmlEncode(course.Area)}</li>"
            + $"<li><strong>Duração:</strong> {course.DurationMinutes} min</li>"
            + $"<li><strong>Obrigatório:</strong> {(course.IsMandatory ? "Sim" : "Não")}</li>"
            + "</ul>"
            + $"<p>{excerpt}</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(deepLink)}\">Revisar e aprovar</a></p>";

        var to = realEmails;
        var overrideEnabled = settingsProvider.GetBool(AppSettingKeys.UniLioEmailDevOverrideEnabled, true);
        var overrideTo = settingsProvider.GetString(
            AppSettingKeys.UniLioEmailDevOverrideTo,
            "leonardo.mendes@liotecnica.com.br")?.Trim();

        if (overrideEnabled && !string.IsNullOrWhiteSpace(overrideTo))
        {
            var originals = string.Join(", ", realEmails);
            subject = $"[DEV OVERRIDE] {subject}";
            bodyText = $"[DEV OVERRIDE] Destinatários originais: {originals}\n\n{bodyText}";
            bodyHtml =
                $"<p><strong>[DEV OVERRIDE]</strong> Destinatários originais: "
                + $"{WebUtility.HtmlEncode(originals)}</p>{bodyHtml}";
            to = [overrideTo];
        }

        var metadata = JsonSerializer.Serialize(new
        {
            source = "unilio.course.submitted",
            courseId = course.Id,
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
                CorrelationId: course.Id,
                CreatedById: submitter.Id),
            cancellationToken);
    }
}
