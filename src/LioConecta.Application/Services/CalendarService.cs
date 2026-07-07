using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Services;

public sealed class CalendarService(
    IAppSettingsProvider settingsProvider,
    ICurrentUserService currentUserService,
    IUserGraphTokenService graphTokenService,
    ICalendarGraphAdapter calendarGraphAdapter,
    ICalendarRepository calendarRepository) : ICalendarService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<CalendarBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var instance = settingsProvider.GetString(AppSettingKeys.AzureAdInstance, "https://login.microsoftonline.com/");
        var tenantId = settingsProvider.GetString(AppSettingKeys.AzureAdTenantId);
        var clientId = settingsProvider.GetString(AppSettingKeys.AzureAdClientId);
        var authority = BuildAuthority(instance, tenantId);

        var bootstrap = new CalendarBootstrapDto(
            Enabled: settingsProvider.GetBool(AppSettingKeys.CalendarEnabled, false),
            DelegatedScopes: ResolveDelegatedScopes(),
            DefaultView: settingsProvider.GetString(AppSettingKeys.CalendarDefaultView, "dayGridMonth"),
            ShowBirthdays: settingsProvider.GetBool(AppSettingKeys.CalendarShowBirthdays, true),
            ShowCafeteriaMenu: settingsProvider.GetBool(AppSettingKeys.CalendarShowCafeteriaMenu, true),
            MsalClientId: clientId,
            MsalTenantId: tenantId,
            MsalAuthority: authority);

        return Task.FromResult(bootstrap);
    }

    public async Task<CalendarStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var enabled = settingsProvider.GetBool(AppSettingKeys.CalendarEnabled, false);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var linked = await graphTokenService.HasLinkedAccountAsync(personId, cancellationToken);
        var hasCalendarScope = linked
            && await graphTokenService.HasScopeAsync(personId, "Calendars.ReadWrite", cancellationToken);

        return new CalendarStatusDto(
            Enabled: enabled,
            Linked: linked && hasCalendarScope,
            NeedsConsent: enabled && (!linked || !hasCalendarScope));
    }

    public async Task LinkAccountAsync(
        LinkCalendarAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCalendarEnabled();

        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException("Access token e refresh token são obrigatórios.");
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var scopes = request.Scopes?.Count > 0 ? request.Scopes : ResolveDelegatedScopes();

        await graphTokenService.StoreTokensAsync(
            personId,
            request.AccessToken.Trim(),
            request.RefreshToken.Trim(),
            request.ExpiresAt,
            scopes,
            cancellationToken);
    }

    public async Task UnlinkAccountAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await graphTokenService.UnlinkAsync(personId, cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarListItemDto>> GetCalendarsAsync(
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var calendars = await calendarGraphAdapter.ListCalendarsAsync(accessToken, cancellationToken);

        return calendars
            .Select(c => new CalendarListItemDto(c.Id, c.Name, c.Color, c.CanEdit, c.IsDefaultCalendar))
            .ToList();
    }

    public async Task<IReadOnlyList<CalendarEventDto>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<string>? calendarIds,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ArgumentException("End date must be after start date.", nameof(to));
        }

        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var events = await calendarGraphAdapter.GetCalendarViewAsync(
            accessToken,
            from,
            to,
            calendarIds,
            cancellationToken);

        return events.Select(MapEvent).ToList();
    }

    public async Task<CalendarEventDto> CreateEventAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var created = await calendarGraphAdapter.CreateEventAsync(
            accessToken,
            request.CalendarId,
            new GraphCalendarEventWrite
            {
                Title = request.Title,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                IsAllDay = request.IsAllDay,
                Location = request.Location,
                Description = request.Description
            },
            cancellationToken);

        return MapEvent(created);
    }

    public async Task<CalendarEventDto> UpdateEventAsync(
        string eventId,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("Event id is required.", nameof(eventId));
        }

        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var updated = await calendarGraphAdapter.UpdateEventAsync(
            accessToken,
            eventId,
            new GraphCalendarEventWrite
            {
                Title = request.Title ?? string.Empty,
                StartAt = request.StartAt ?? DateTimeOffset.UtcNow,
                EndAt = request.EndAt ?? DateTimeOffset.UtcNow.AddHours(1),
                IsAllDay = request.IsAllDay ?? false,
                Location = request.Location,
                Description = request.Description
            },
            cancellationToken);

        return MapEvent(updated);
    }

    public async Task DeleteEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("Event id is required.", nameof(eventId));
        }

        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        await calendarGraphAdapter.DeleteEventAsync(accessToken, eventId, cancellationToken);
    }

    public async Task<CafeteriaMenuDto?> GetCafeteriaMenuAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var menu = await calendarRepository.GetCafeteriaMenuAsync(date, cancellationToken);
        return menu is null ? null : CalendarMapper.ToDto(menu);
    }

    private async Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken)
    {
        EnsureCalendarEnabled();
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return await graphTokenService.GetValidAccessTokenAsync(personId, cancellationToken);
    }

    private void EnsureCalendarEnabled()
    {
        if (!settingsProvider.GetBool(AppSettingKeys.CalendarEnabled, false))
        {
            throw new InvalidOperationException("Integração de calendário Outlook está desabilitada.");
        }
    }

    private IReadOnlyList<string> ResolveDelegatedScopes()
    {
        var raw = settingsProvider.GetString(AppSettingKeys.CalendarDelegatedScopes);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ["Calendars.ReadWrite", "User.Read", "offline_access"];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [raw];
        }
    }

    private static string BuildAuthority(string instance, string tenantId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(instance)
            ? "https://login.microsoftonline.com/"
            : instance.TrimEnd('/') + "/";
        var tenant = string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId.Trim();
        return $"{baseUrl}{tenant}";
    }

    private static CalendarEventDto MapEvent(GraphCalendarEventDetail detail)
        => new(
            detail.Id,
            detail.CalendarId,
            detail.Title,
            detail.StartAt,
            detail.EndAt,
            detail.IsAllDay,
            detail.Location,
            detail.Description,
            detail.OnlineMeetingUrl,
            detail.WebLink,
            detail.OrganizerName,
            detail.OrganizerEmail,
            "Outlook",
            detail.Color,
            detail.CanEdit);
}
