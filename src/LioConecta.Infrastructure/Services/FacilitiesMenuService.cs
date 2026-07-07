using System.Globalization;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class FacilitiesMenuService(
    IFacilitiesMenuRepository menuRepository,
    IAppSettingsProvider settingsProvider,
    ICurrentUserService currentUserService,
    IEmailSendService emailSendService,
    IHostEnvironment hostEnvironment,
    AppDbContext db) : IFacilitiesMenuService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<MenuEditorBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var policy = await GetEditorPolicyAsync(cancellationToken);
        return new MenuEditorBootstrapDto(policy.CanEdit, FacilitiesMenuTemplates.ToTemplatesDto());
    }

    public async Task<FacilitiesMenuEditorPolicyDto> GetEditorPolicyAsync(CancellationToken cancellationToken = default)
    {
        var canEdit = await CanEditAsync(cancellationToken);
        return new FacilitiesMenuEditorPolicyDto(canEdit);
    }

    public async Task<DailyMenuDto?> GetPublishedDailyMenuAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var entity = await menuRepository.GetByDateAsync(date, cancellationToken);
        if (entity is null || !entity.Published)
        {
            return null;
        }

        return FacilitiesMenuMapper.ToDailyDto(entity);
    }

    public async Task<WeeklyMenuDto> GetWeeklyMenuAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        EnsureMonday(weekStart);
        var dates = EnumerateWeekDates(weekStart);
        var entities = await menuRepository.GetByDateRangeAsync(dates.First(), dates.Last(), cancellationToken);
        var byDate = entities.ToDictionary(entity => entity.Date);

        var days = dates
            .Select(date => byDate.TryGetValue(date, out var entity)
                ? FacilitiesMenuMapper.ToDailyDto(entity)
                : CreateEmptyDailyDto(date))
            .ToList();

        return new WeeklyMenuDto(weekStart, days);
    }

    public async Task<DailyMenuDto> SaveDailyMenuAsync(
        DateOnly date,
        SaveDailyMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanEditAsync(cancellationToken);

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payload = FacilitiesMenuMapper.FromSaveRequest(request);

        var entity = new CafeteriaMenu
        {
            Date = date,
            PayloadJson = FacilitiesMenuMapper.SerializePayload(payload),
            ItemsJson = "[]",
            Published = request.Published ?? false,
            UpdatedById = personId,
        };

        var saved = await menuRepository.UpsertAsync(entity, cancellationToken);
        return FacilitiesMenuMapper.ToDailyDto(saved);
    }

    public async Task<DailyMenuDto> CopyDailyMenuAsync(
        DateOnly targetDate,
        DateOnly sourceDate,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanEditAsync(cancellationToken);

        var source = await menuRepository.GetByDateAsync(sourceDate, cancellationToken)
            ?? throw new KeyNotFoundException($"Cardápio de origem não encontrado para {sourceDate:yyyy-MM-dd}.");

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payload = FacilitiesMenuMapper.ClonePayload(source);

        var entity = new CafeteriaMenu
        {
            Date = targetDate,
            PayloadJson = FacilitiesMenuMapper.SerializePayload(payload),
            ItemsJson = "[]",
            Published = false,
            UpdatedById = personId,
        };

        var saved = await menuRepository.UpsertAsync(entity, cancellationToken);
        return FacilitiesMenuMapper.ToDailyDto(saved);
    }

    public async Task<WeeklyMenuDto> CopyWeeklyMenuAsync(
        DateOnly targetWeekStart,
        DateOnly sourceWeekStart,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanEditAsync(cancellationToken);

        EnsureMonday(targetWeekStart);
        EnsureMonday(sourceWeekStart);

        var sourceWeek = await GetWeeklyMenuAsync(sourceWeekStart, cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);

        foreach (var day in sourceWeek.Days)
        {
            if (IsEmptyDay(day))
            {
                continue;
            }

            var dayOffset = day.Date.DayNumber - sourceWeekStart.DayNumber;
            var targetDate = targetWeekStart.AddDays(dayOffset);
            var payload = FacilitiesMenuMapper.FromDailyDto(day);

            await menuRepository.UpsertAsync(new CafeteriaMenu
            {
                Date = targetDate,
                PayloadJson = FacilitiesMenuMapper.SerializePayload(payload),
                ItemsJson = "[]",
                Published = false,
                UpdatedById = personId,
            }, cancellationToken);
        }

        return await GetWeeklyMenuAsync(targetWeekStart, cancellationToken);
    }

    public async Task DeleteDailyMenuAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureCanEditAsync(cancellationToken);
        await menuRepository.DeleteAsync(date, cancellationToken);
    }

    public Task<byte[]> GetWeeklyMenuPdfAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        EnsureMonday(weekStart);
        return GenerateWeeklyPdfBytesAsync(weekStart, cancellationToken);
    }

    public async Task<SendFacilitiesMenuEmailResponse> SendWeeklyEmailAsync(
        SendFacilitiesMenuEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanEditAsync(cancellationToken);

        EnsureMonday(request.WeekStart);
        var week = await GetWeeklyMenuAsync(request.WeekStart, cancellationToken);

        var recipients = ResolveRecipients(request.Recipients);
        if (recipients.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um destinatário ou configure facilities.menu.email_recipients.");
        }

        var subject = BuildEmailSubject(request.WeekStart);
        var bodyHtml = BuildWeeklyEmailHtml(week);

        var senderPersonId = await currentUserService.GetPersonIdAsync(cancellationToken);
        IReadOnlyList<EmailAttachmentRecord>? directAttachments = null;
        if (request.IncludePdf)
        {
            var pdfBytes = FacilitiesMenuPdfGenerator.Generate(week);
            directAttachments =
            [
                await StagePdfAttachmentAsync(
                    pdfBytes,
                    FacilitiesMenuPdfGenerator.BuildFileName(request.WeekStart),
                    cancellationToken),
            ];
        }

        await emailSendService.SendAsync(
            new SendEmailRequest(
                recipients,
                null,
                subject,
                bodyHtml,
                null,
                null,
                null,
                "facilities-menu",
                directAttachments),
            senderPersonId,
            cancellationToken);

        var message = request.IncludePdf
            ? $"Cardápio enviado por e-mail para {recipients.Count} destinatário(s) com anexo PDF."
            : $"Cardápio enviado por e-mail para {recipients.Count} destinatário(s).";

        return new SendFacilitiesMenuEmailResponse(true, message, recipients.Count);
    }

    private async Task<byte[]> GenerateWeeklyPdfBytesAsync(DateOnly weekStart, CancellationToken cancellationToken)
    {
        var week = await GetWeeklyMenuAsync(weekStart, cancellationToken);
        return FacilitiesMenuPdfGenerator.Generate(week);
    }

    private async Task<EmailAttachmentRecord> StagePdfAttachmentAsync(
        byte[] pdfBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        var storageRoot = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "email", "attachments");
        Directory.CreateDirectory(storageRoot);

        var storedName = $"{Guid.NewGuid():N}.pdf";
        var absolutePath = Path.Combine(storageRoot, storedName);
        await File.WriteAllBytesAsync(absolutePath, pdfBytes, cancellationToken);

        return new EmailAttachmentRecord(fileName, "application/pdf", absolutePath, pdfBytes.LongLength);
    }

    private async Task<bool> CanEditAsync(CancellationToken cancellationToken)
    {
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        if (roles.Contains(UserRole.Admin))
        {
            return true;
        }

        var allowedRoles = DeserializeRoles(settingsProvider.GetString(AppSettingKeys.FacilitiesMenuAllowedRoles));
        if (roles.Any(role => allowedRoles.Contains(role)))
        {
            return true;
        }

        var allowedEmails = settingsProvider
            .GetStringArray(AppSettingKeys.FacilitiesMenuAllowedEmails)
            .Select(email => email.Trim().ToLowerInvariant())
            .Where(email => email.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowedEmails.Count == 0)
        {
            return false;
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var email = await db.People
            .AsNoTracking()
            .Where(person => person.Id == personId)
            .Select(person => person.Email)
            .FirstOrDefaultAsync(cancellationToken);

        return email is not null && allowedEmails.Contains(email.Trim().ToLowerInvariant());
    }

    private async Task EnsureCanEditAsync(CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão para editar o cardápio.");
        }
    }

    private IReadOnlyList<string> ResolveRecipients(IReadOnlyList<string>? requested)
    {
        var list = requested?
            .Select(email => email.Trim())
            .Where(email => email.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list is { Count: > 0 })
        {
            return list;
        }

        return settingsProvider
            .GetStringArray(AppSettingKeys.FacilitiesMenuEmailRecipients)
            .Select(email => email.Trim())
            .Where(email => email.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<UserRole> DeserializeRoles(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [UserRole.Facilities, UserRole.Admin];
        }

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            var roles = new List<UserRole>();
            foreach (var value in values)
            {
                if (Enum.TryParse<UserRole>(value, true, out var role))
                {
                    roles.Add(role);
                }
            }

            return roles;
        }
        catch (JsonException)
        {
            return [UserRole.Facilities, UserRole.Admin];
        }
    }

    private static void EnsureMonday(DateOnly weekStart)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException("weekStart deve ser uma segunda-feira.", nameof(weekStart));
        }
    }

    private static List<DateOnly> EnumerateWeekDates(DateOnly weekStart)
        => Enumerable.Range(0, 7).Select(offset => weekStart.AddDays(offset)).ToList();

    private static DailyMenuDto CreateEmptyDailyDto(DateOnly date)
        => new(
            date,
            "normal",
            null,
            [new MealMenuDto("lunch", FacilitiesMenuTemplates.CreateEmptyLunchSections())],
            null,
            false,
            null,
            null);

    private static bool IsEmptyDay(DailyMenuDto day)
    {
        if (day.DayStatus is "holiday" or "closed")
        {
            return false;
        }

        return day.Meals.All(meal =>
            meal.Sections.All(section => string.IsNullOrWhiteSpace(section.Value)));
    }

    private static string BuildEmailSubject(DateOnly weekStart)
    {
        var end = weekStart.AddDays(6);
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        return $"Cardápio Semanal — {weekStart.ToString("dd/MM/yy", culture)} à {end.ToString("dd/MM/yy", culture)}";
    }

    private static string BuildWeeklyEmailHtml(WeeklyMenuDto week)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var builder = new StringBuilder();
        builder.Append("<html><body style=\"font-family:Segoe UI,Arial,sans-serif;color:#0f172a;\">");
        builder.Append($"<h2>Cardápio Semanal — {week.WeekStart.ToString("dd/MM/yyyy", culture)}</h2>");
        builder.Append("<table border=\"1\" cellpadding=\"8\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;font-size:13px;\">");
        builder.Append("<thead><tr><th>Dia</th>");

        foreach (var section in FacilitiesMenuTemplates.LunchSections)
        {
            builder.Append($"<th>{System.Net.WebUtility.HtmlEncode(section.Label)}</th>");
        }

        builder.Append("</tr></thead><tbody>");

        foreach (var day in week.Days)
        {
            var lunch = day.Meals.FirstOrDefault(meal => meal.MealType == "lunch");
            var sections = lunch?.Sections ?? [];
            var sectionMap = sections.ToDictionary(section => section.Key, section => section.Value);
            var dayLabel = day.Date.ToString("dddd dd/MM", culture);
            var rowStyle = day.DayStatus == "holiday" ? " style=\"color:#b91c1c;font-weight:600;\"" : string.Empty;

            builder.Append($"<tr{rowStyle}><td>{System.Net.WebUtility.HtmlEncode(dayLabel)}</td>");
            foreach (var template in FacilitiesMenuTemplates.LunchSections)
            {
                sectionMap.TryGetValue(template.Key, out var value);
                var cell = string.IsNullOrWhiteSpace(value) ? "—" : value;
                if (day.DayStatus == "holiday" && template.Key == "light" && !string.IsNullOrWhiteSpace(day.DayStatusLabel))
                {
                    cell = day.DayStatusLabel!;
                }

                builder.Append($"<td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
            }

            builder.Append("</tr>");
        }

        builder.Append("</tbody></table></body></html>");
        return builder.ToString();
    }
}
