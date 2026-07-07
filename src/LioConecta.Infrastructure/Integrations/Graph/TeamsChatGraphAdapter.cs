using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class TeamsChatGraphAdapter(
    HttpClient httpClient,
    ILogger<TeamsChatGraphAdapter> logger) : ITeamsChatAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<TeamsChatSummary>> ListChatsAsync(
        string accessToken,
        bool includeGroupChats,
        CancellationToken cancellationToken = default)
    {
        var chats = new List<TeamsChatSummary>();
        var filter = includeGroupChats ? string.Empty : "&$filter=chatType eq 'oneOnOne'";
        var url =
            $"me/chats?$expand=members,lastMessagePreview&$orderby=lastMessagePreview/createdDateTime desc&$top=50{filter}";

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var request = CreateRequest(HttpMethod.Get, url, accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Graph list chats failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var mapped = MapChatSummary(item);
                    if (mapped is not null)
                    {
                        chats.Add(mapped);
                    }
                }
            }

            url = root.TryGetProperty("@odata.nextLink", out var nextLink)
                ? ToRelativePath(nextLink.GetString())
                : null;
        }

        return chats;
    }

    public async Task<TeamsChatPage<TeamsChatMessage>> ListMessagesAsync(
        string accessToken,
        string conversationId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var top = Math.Clamp(limit, 1, 50);
        var url = string.IsNullOrWhiteSpace(cursor)
            ? $"me/chats/{Uri.EscapeDataString(conversationId)}/messages?$top={top}&$orderby=createdDateTime desc"
            : cursor.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? ToRelativePath(cursor) ?? cursor
                : $"me/chats/{Uri.EscapeDataString(conversationId)}/messages?$top={top}&$skiptoken={Uri.EscapeDataString(cursor)}";

        using var request = CreateRequest(HttpMethod.Get, url!, accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Graph list messages failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var messages = new List<TeamsChatMessage>();

        if (root.TryGetProperty("value", out var valueElement))
        {
            foreach (var item in valueElement.EnumerateArray())
            {
                var mapped = MapMessage(item);
                if (mapped is not null)
                {
                    messages.Add(mapped);
                }
            }
        }

        var nextLink = root.TryGetProperty("@odata.nextLink", out var nextLinkElement)
            ? nextLinkElement.GetString()
            : null;

        return new TeamsChatPage<TeamsChatMessage>(messages, nextLink);
    }

    public async Task<TeamsChatMessage> SendMessageAsync(
        string accessToken,
        string conversationId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            body = new
            {
                contentType = "text",
                content = text
            }
        });

        using var request = CreateRequest(
            HttpMethod.Post,
            $"me/chats/{Uri.EscapeDataString(conversationId)}/messages",
            accessToken,
            payload);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Graph send message failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
        }

        using var document = JsonDocument.Parse(body);
        var mapped = MapMessage(document.RootElement)
            ?? throw new InvalidOperationException("Graph returned an empty message payload.");

        return mapped;
    }

    public async Task<TeamsChatSummary> CreateOneOnOneChatAsync(
        string accessToken,
        string targetUserId,
        CancellationToken cancellationToken = default)
    {
        var payload = $$"""
            {
              "chatType": "oneOnOne",
              "members": [
                {
                  "@odata.type": "#microsoft.graph.aadUserConversationMember",
                  "roles": ["owner"],
                  "user@odata.bind": "https://graph.microsoft.com/v1.0/users('{{targetUserId}}')"
                }
              ]
            }
            """;

        using var request = CreateRequest(HttpMethod.Post, "chats", accessToken, payload);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Graph create chat failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
        }

        using var document = JsonDocument.Parse(body);
        var mapped = MapChatSummary(document.RootElement)
            ?? throw new InvalidOperationException("Graph returned an empty chat payload.");

        return mapped;
    }

    public async Task<TeamsGraphUser?> FindUserByEmailAsync(
        string accessToken,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim();
        var url = $"users/{Uri.EscapeDataString(normalized)}?$select=id,displayName,mail,userPrincipalName";

        using var request = CreateRequest(HttpMethod.Get, url, accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Graph user lookup failed for {Email}: {Status}", normalized, (int)response.StatusCode);
            return null;
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        return new TeamsGraphUser(
            ReadString(root, "id") ?? string.Empty,
            ReadString(root, "displayName") ?? normalized,
            ReadString(root, "mail") ?? ReadString(root, "userPrincipalName"));
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

    private static TeamsChatSummary? MapChatSummary(JsonElement item)
    {
        var id = ReadString(item, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        TeamsChatMessagePreview? preview = null;
        if (item.TryGetProperty("lastMessagePreview", out var previewElement)
            && previewElement.ValueKind == JsonValueKind.Object)
        {
            preview = new TeamsChatMessagePreview(
                ReadString(previewElement, "id") ?? string.Empty,
                ReadDateTimeOffset(previewElement, "createdDateTime") ?? DateTimeOffset.UtcNow,
                ReadMessageText(previewElement),
                MapIdentity(previewElement, "from"));
        }

        var members = new List<TeamsChatMember>();
        if (item.TryGetProperty("members", out var membersElement)
            && membersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var member in membersElement.EnumerateArray())
            {
                var mappedMember = MapMember(member);
                if (mappedMember is not null)
                {
                    members.Add(mappedMember);
                }
            }
        }

        return new TeamsChatSummary(
            id,
            ReadString(item, "topic"),
            ReadString(item, "chatType") ?? "unknown",
            preview,
            members);
    }

    private static TeamsChatMessage? MapMessage(JsonElement item)
    {
        var id = ReadString(item, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return new TeamsChatMessage(
            id,
            ReadDateTimeOffset(item, "createdDateTime") ?? DateTimeOffset.UtcNow,
            ReadMessageText(item) ?? string.Empty,
            MapIdentity(item, "from"));
    }

    private static TeamsChatMember? MapMember(JsonElement member)
    {
        if (member.TryGetProperty("user", out var userElement) && userElement.ValueKind == JsonValueKind.Object)
        {
            var id = ReadString(userElement, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return new TeamsChatMember(
                id,
                ReadString(userElement, "displayName") ?? id,
                ReadString(userElement, "mail") ?? ReadString(userElement, "userPrincipalName"));
        }

        var memberId = ReadString(member, "id");
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return null;
        }

        return new TeamsChatMember(
            memberId,
            ReadString(member, "displayName") ?? memberId,
            ReadString(member, "email"));
    }

    private static TeamsChatIdentity? MapIdentity(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var fromElement) || fromElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (fromElement.TryGetProperty("user", out var userElement) && userElement.ValueKind == JsonValueKind.Object)
        {
            var id = ReadString(userElement, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return new TeamsChatIdentity(
                id,
                ReadString(userElement, "displayName") ?? id,
                ReadString(userElement, "mail") ?? ReadString(userElement, "userPrincipalName"));
        }

        var identityId = ReadString(fromElement, "id");
        if (string.IsNullOrWhiteSpace(identityId))
        {
            return null;
        }

        return new TeamsChatIdentity(
            identityId,
            ReadString(fromElement, "displayName") ?? identityId,
            ReadString(fromElement, "email"));
    }

    private static string? ReadMessageText(JsonElement item)
    {
        if (!item.TryGetProperty("body", out var bodyElement) || bodyElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(bodyElement, "content");
    }

    private static string? ReadString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement item, string propertyName)
    {
        var raw = ReadString(item, propertyName);
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
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
