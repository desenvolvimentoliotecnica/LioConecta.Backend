using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class CalendarGraphAdapter(HttpClient httpClient) : ICalendarGraphAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<GraphCalendarListItem>> ListCalendarsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var calendars = new List<GraphCalendarListItem>();
        var url = "me/calendars?$select=id,name,color,canEdit,isDefaultCalendar&$top=50";

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var request = CreateRequest(HttpMethod.Get, url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Graph list calendars failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    calendars.Add(new GraphCalendarListItem
                    {
                        Id = ReadString(item, "id") ?? string.Empty,
                        Name = ReadString(item, "name") ?? "Calendário",
                        Color = ReadString(item, "color"),
                        CanEdit = item.TryGetProperty("canEdit", out var canEdit) && canEdit.GetBoolean(),
                        IsDefaultCalendar = item.TryGetProperty("isDefaultCalendar", out var isDefault)
                            && isDefault.GetBoolean()
                    });
                }
            }

            url = root.TryGetProperty("@odata.nextLink", out var nextLink)
                ? ToRelativePath(nextLink.GetString())
                : null;
        }

        return calendars;
    }

    public async Task<IReadOnlyList<GraphCalendarEventDetail>> GetCalendarViewAsync(
        string accessToken,
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<string>? calendarIds,
        CancellationToken cancellationToken = default)
    {
        var events = new Dictionary<string, GraphCalendarEventDetail>(StringComparer.OrdinalIgnoreCase);
        var calendars = await ListCalendarsAsync(accessToken, cancellationToken);
        var calendarMap = calendars.ToDictionary(c => c.Id, c => c, StringComparer.OrdinalIgnoreCase);

        var targets = calendarIds is { Count: > 0 }
            ? calendars.Where(c => calendarIds.Contains(c.Id, StringComparer.OrdinalIgnoreCase)).ToList()
            : calendars;

        if (targets.Count == 0)
        {
            targets = calendars;
        }

        var fromParam = Uri.EscapeDataString(from.ToString("o"));
        var toParam = Uri.EscapeDataString(to.ToString("o"));

        foreach (var calendar in targets)
        {
            var path = calendar.IsDefaultCalendar
                ? $"me/calendarView?startDateTime={fromParam}&endDateTime={toParam}&$top=250&$orderby=start/dateTime"
                : $"me/calendars/{Uri.EscapeDataString(calendar.Id)}/calendarView?startDateTime={fromParam}&endDateTime={toParam}&$top=250&$orderby=start/dateTime";

            await FetchCalendarViewPageAsync(accessToken, path, calendar, calendarMap, events, cancellationToken);
        }

        return events.Values.OrderBy(e => e.StartAt).ToList();
    }

    public async Task<GraphCalendarEventDetail> CreateEventAsync(
        string accessToken,
        string calendarId,
        GraphCalendarEventWrite write,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildEventPayload(write);
        var path = string.IsNullOrWhiteSpace(calendarId) || calendarId.Equals("primary", StringComparison.OrdinalIgnoreCase)
            ? "me/events"
            : $"me/calendars/{Uri.EscapeDataString(calendarId)}/events";

        using var request = CreateRequest(HttpMethod.Post, path, accessToken, payload);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Graph create event failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
        }

        using var document = JsonDocument.Parse(body);
        return MapEvent(document.RootElement, calendarId, canEdit: true)
            ?? throw new InvalidOperationException("Graph returned an empty event payload.");
    }

    public async Task<GraphCalendarEventDetail> UpdateEventAsync(
        string accessToken,
        string eventId,
        GraphCalendarEventWrite write,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildEventPayload(write);
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"me/events/{Uri.EscapeDataString(eventId)}",
            accessToken,
            payload);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Graph update event failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
        }

        using var document = JsonDocument.Parse(body);
        return MapEvent(document.RootElement, ReadString(document.RootElement, "calendarId") ?? string.Empty, canEdit: true)
            ?? throw new InvalidOperationException("Graph returned an empty event payload.");
    }

    public async Task DeleteEventAsync(
        string accessToken,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Delete,
            $"me/events/{Uri.EscapeDataString(eventId)}",
            accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Graph delete event failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
    }

    private async Task FetchCalendarViewPageAsync(
        string accessToken,
        string url,
        GraphCalendarListItem calendar,
        IReadOnlyDictionary<string, GraphCalendarListItem> calendarMap,
        Dictionary<string, GraphCalendarEventDetail> events,
        CancellationToken cancellationToken)
    {
        while (!string.IsNullOrWhiteSpace(url))
        {
            using var request = CreateRequest(HttpMethod.Get, url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Graph calendarView failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var mapped = MapEvent(item, calendar.Id, calendar.CanEdit);
                    if (mapped is null || events.ContainsKey(mapped.Id))
                    {
                        continue;
                    }

                    if (calendarMap.TryGetValue(mapped.CalendarId, out var sourceCalendar))
                    {
                        mapped.Color ??= sourceCalendar.Color;
                        mapped.CanEdit = sourceCalendar.CanEdit;
                    }

                    events[mapped.Id] = mapped;
                }
            }

            url = root.TryGetProperty("@odata.nextLink", out var nextLink)
                ? ToRelativePath(nextLink.GetString())
                : null;
        }
    }

    private static GraphCalendarEventDetail? MapEvent(JsonElement item, string calendarId, bool canEdit)
    {
        var id = ReadString(item, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var subject = ReadString(item, "subject") ?? "(Sem título)";
        var isAllDay = item.TryGetProperty("isAllDay", out var allDay) && allDay.GetBoolean();
        var startAt = ReadDateTime(item, "start") ?? DateTimeOffset.UtcNow;
        var endAt = ReadDateTime(item, "end") ?? startAt.AddHours(1);

        string? location = null;
        if (item.TryGetProperty("location", out var locationElement)
            && locationElement.TryGetProperty("displayName", out var displayName))
        {
            location = displayName.GetString();
        }

        string? onlineMeetingUrl = null;
        if (item.TryGetProperty("onlineMeeting", out var meetingElement)
            && meetingElement.TryGetProperty("joinUrl", out var joinUrl))
        {
            onlineMeetingUrl = joinUrl.GetString();
        }

        string? organizerName = null;
        string? organizerEmail = null;
        if (item.TryGetProperty("organizer", out var organizerElement)
            && organizerElement.TryGetProperty("emailAddress", out var emailElement))
        {
            organizerName = ReadString(emailElement, "name");
            organizerEmail = ReadString(emailElement, "address");
        }

        return new GraphCalendarEventDetail
        {
            Id = id,
            CalendarId = ReadString(item, "calendarId") ?? calendarId,
            Title = subject,
            StartAt = startAt,
            EndAt = endAt,
            IsAllDay = isAllDay,
            Location = location,
            Description = ReadBodyContent(item),
            OnlineMeetingUrl = onlineMeetingUrl,
            WebLink = ReadString(item, "webLink"),
            OrganizerName = organizerName,
            OrganizerEmail = organizerEmail,
            CanEdit = canEdit
        };
    }

    private static string BuildEventPayload(GraphCalendarEventWrite write)
    {
        var payload = new Dictionary<string, object?>
        {
            ["subject"] = write.Title,
            ["isAllDay"] = write.IsAllDay,
            ["start"] = BuildDateTimeTimeZone(write.StartAt, write.IsAllDay),
            ["end"] = BuildDateTimeTimeZone(write.EndAt, write.IsAllDay)
        };

        if (!string.IsNullOrWhiteSpace(write.Location))
        {
            payload["location"] = new { displayName = write.Location };
        }

        if (!string.IsNullOrWhiteSpace(write.Description))
        {
            payload["body"] = new { contentType = "text", content = write.Description };
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static object BuildDateTimeTimeZone(DateTimeOffset value, bool isAllDay)
    {
        if (isAllDay)
        {
            return new { dateTime = value.ToString("yyyy-MM-dd"), timeZone = "UTC" };
        }

        return new { dateTime = value.ToString("o"), timeZone = "UTC" };
    }

    private static string? ReadBodyContent(JsonElement item)
    {
        if (!item.TryGetProperty("body", out var body) || body.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(body, "content");
    }

    private static DateTimeOffset? ReadDateTime(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var raw = ReadString(value, "dateTime");
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string? ReadString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string relativeOrAbsoluteUrl,
        string accessToken,
        string? jsonBody = null)
    {
        var request = new HttpRequestMessage(method, relativeOrAbsoluteUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string? ToRelativePath(string? absoluteUrl)
    {
        if (string.IsNullOrWhiteSpace(absoluteUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return absoluteUrl;
        }

        var pathAndQuery = uri.PathAndQuery;
        const string prefix = "/v1.0/";
        var index = pathAndQuery.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? pathAndQuery[(index + prefix.Length)..] : pathAndQuery.TrimStart('/');
    }

    private static string TryReadGraphError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var errorElement)
                && errorElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return body.Length > 400 ? body[..400] + "…" : body;
    }
}
