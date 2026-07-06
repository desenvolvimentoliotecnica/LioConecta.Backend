using System.Net.Http.Json;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Integrations;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class GlpiAdapter(
    HttpClient httpClient,
    GlpiCredentialsResolver credentialsResolver,
    GlpiSessionManager sessionManager,
    GlpiUserNameResolver userNameResolver,
    ILogger<GlpiAdapter> logger) : IGlpiAdapter
{
    private const int PageSize = 50;
    private const int MaxTickets = 100;
    private static readonly TimeSpan CategoryCacheDuration = TimeSpan.FromMinutes(5);

    private IReadOnlyList<GlpiItilCategory>? _cachedCategories;
    private DateTimeOffset _categoriesCachedAt = DateTimeOffset.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string priority,
        int categoryId,
        string requesterEmail,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            throw new GlpiIntegrationException("GLPI não configurado. Informe glpi.base_url no portal admin.");
        }

        if (categoryId <= 0)
        {
            throw new ArgumentException("Categoria inválida.");
        }

        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (requesterId is null)
        {
            throw new GlpiRequesterNotFoundException(requesterEmail);
        }

        var priorityLevel = MapFormPriority(priority);
        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(credentials.BaseUrl, "Ticket"));
            ApplySessionHeaders(request, credentials, sessionToken);
            request.Content = JsonContent.Create(new
            {
                input = new Dictionary<string, object?>
                {
                    ["name"] = title,
                    ["content"] = description,
                    ["priority"] = priorityLevel,
                    ["urgency"] = priorityLevel,
                    ["type"] = 2,
                    ["itilcategories_id"] = categoryId,
                    ["_users_id_requester"] = requesterId,
                },
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                return await CreateTicketAsync(title, description, priority, categoryId, requesterEmail, cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "GLPI ticket creation failed for {Email}: {Status} {Body}",
                    requesterEmail,
                    (int)response.StatusCode,
                    body);
                throw new GlpiIntegrationException("O GLPI rejeitou a criação do chamado. Verifique categoria e permissões.");
            }

            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
            var ticketId = ReadElement(payload?.GetValueOrDefault("id")).Trim('"');
            if (string.IsNullOrWhiteSpace(ticketId))
            {
                throw new GlpiIntegrationException("O GLPI não retornou o identificador do chamado.");
            }

            return new GlpiTicketResult
            {
                TicketId = ticketId,
                Status = "New",
                Url = BuildTicketUrl(credentials, ticketId),
            };
        }
        catch (GlpiRequesterNotFoundException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (GlpiIntegrationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create GLPI ticket for {Email}", requesterEmail);
            throw new GlpiIntegrationException("Falha ao comunicar com o GLPI.", exception);
        }
    }

    public async Task<IReadOnlyList<GlpiItilCategory>> GetItilCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedCategories is not null &&
            DateTimeOffset.UtcNow - _categoriesCachedAt < CategoryCacheDuration)
        {
            return _cachedCategories;
        }

        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return [];
        }

        var query =
            $"{BuildUrl(credentials.BaseUrl, "search/ITILCategory")}" +
            $"?forcedisplay[0]={GlpiSearchFields.ItilCategoryId}" +
            $"&forcedisplay[1]={GlpiSearchFields.ItilCategoryName}" +
            $"&forcedisplay[2]={GlpiSearchFields.ItilCategoryCompleteName}" +
            "&range=0-499" +
            "&sort=1&order=ASC";

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
            return await GetItilCategoriesAsync(cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("GLPI ITILCategory search failed: {Status} {Body}", (int)response.StatusCode, body);
            return _cachedCategories ?? [];
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("data", out var dataElement))
        {
            return [];
        }

        var categories = new Dictionary<int, GlpiItilCategory>();
        foreach (var row in dataElement.EnumerateArray())
        {
            var idRaw = ReadRowField(row, GlpiSearchFields.ItilCategoryId);
            if (!int.TryParse(idRaw, out var id) || id <= 0)
            {
                continue;
            }

            if (categories.ContainsKey(id))
            {
                continue;
            }

            var name = ReadRowField(row, GlpiSearchFields.ItilCategoryName);
            var fullName = ReadRowField(row, GlpiSearchFields.ItilCategoryCompleteName);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = name;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            categories[id] = new GlpiItilCategory
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? fullName : name,
                FullName = fullName,
            };
        }

        _cachedCategories = categories.Values
            .GroupBy(c => (c.FullName ?? c.Name).Trim().ToLowerInvariant(), StringComparer.Ordinal)
            .Select(group => group.OrderBy(c => c.Id).First())
            .OrderBy(c => c.FullName ?? c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _categoriesCachedAt = DateTimeOffset.UtcNow;
        return _cachedCategories;
    }

    public async Task<GlpiTicketResult> GetTicketStatusAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(credentials.BaseUrl, $"Ticket/{ticketId}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        var status = ReadElement(payload?.GetValueOrDefault("status")).Trim('"');

        return new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = status,
            Url = BuildTicketUrl(credentials, ticketId),
        };
    }

    public async Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsByRequesterAsync(
        string requesterEmail,
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return [];
        }

        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (requesterId is null)
        {
            logger.LogWarning("GLPI user not found for email {Email}", requesterEmail);
            return [];
        }

        return await SearchTicketsAsync(credentials, scope, requesterId, includeRequester: false, cancellationToken);
    }

    public Task<IReadOnlyList<GlpiTicketSummary>> SearchAllTicketsAsync(
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return Task.FromResult<IReadOnlyList<GlpiTicketSummary>>([]);
        }

        return SearchTicketsAsync(credentials, scope, requesterId: null, includeRequester: true, cancellationToken);
    }

    public async Task<GlpiTicketDetail?> GetTicketDetailAsync(
        string ticketId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (!skipOwnershipCheck)
        {
            var ownsTicket = await VerifyTicketOwnershipAsync(credentials, ticketId, requesterId, cancellationToken);
            if (!ownsTicket)
            {
                logger.LogWarning("User {Email} attempted to access GLPI ticket {TicketId}", requesterEmail, ticketId);
                return null;
            }
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(credentials.BaseUrl, $"Ticket/{ticketId}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        var status = ReadElement(payload.GetValueOrDefault("status")).Trim('"');
        var priority = ReadElement(payload.GetValueOrDefault("priority")).Trim('"');
        var createdAt = ParseGlpiDate(ReadElement(payload.GetValueOrDefault("date")).Trim('"'));
        var updatedAt = ParseGlpiDate(ReadElement(payload.GetValueOrDefault("date_mod")).Trim('"'));

        var summary = new GlpiTicketSummary
        {
            TicketId = ticketId,
            Subject = ReadElement(payload.GetValueOrDefault("name")).Trim('"'),
            Status = status,
            StatusLabel = GlpiStatusMapper.StatusLabel(status),
            PriorityLabel = GlpiStatusMapper.PriorityLabel(priority),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Url = BuildTicketUrl(credentials, ticketId),
        };

        return new GlpiTicketDetail
        {
            Summary = summary,
            Description = ReadElement(payload.GetValueOrDefault("content")).Trim('"'),
            Assignee = "TI — Service Desk",
            Followups = [],
        };
    }

    private async Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsAsync(
        GlpiRuntimeCredentials credentials,
        GlpiTicketScope scope,
        string? requesterId,
        bool includeRequester,
        CancellationToken cancellationToken)
    {
        var query = BuildTicketSearchQuery(credentials.BaseUrl, requesterId, scope, includeRequester);
        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        var results = new List<GlpiTicketSummary>();
        var start = 0;

        while (results.Count < MaxTickets)
        {
            var pageUrl = $"{query}&range={start}-{start + PageSize - 1}";
            using var request = new HttpRequestMessage(HttpMethod.Get, pageUrl);
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                return await SearchTicketsAsync(credentials, scope, requesterId, includeRequester, cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("GLPI ticket search failed: {Status} {Body}", (int)response.StatusCode, body);
                break;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (!root.TryGetProperty("data", out var dataElement) || dataElement.GetArrayLength() == 0)
            {
                break;
            }

            foreach (var row in dataElement.EnumerateArray())
            {
                var summary = MapSearchRow(row, credentials, includeRequester);
                if (scope == GlpiTicketScope.Open && !GlpiStatusMapper.IsOpenStatus(summary.Status))
                {
                    continue;
                }

                results.Add(summary);
                if (results.Count >= MaxTickets)
                {
                    break;
                }
            }

            var count = root.TryGetProperty("count", out var countElement) ? countElement.GetInt32() : 0;
            if (count < PageSize)
            {
                break;
            }

            start += PageSize;
        }

        if (includeRequester && results.Count > 0)
        {
            await userNameResolver.EnrichRequesterLabelsAsync(
                httpClient,
                credentials,
                sessionToken,
                results,
                cancellationToken);
        }

        return results
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }

    private async Task<bool> VerifyTicketOwnershipAsync(
        GlpiRuntimeCredentials credentials,
        string ticketId,
        string? requesterId,
        CancellationToken cancellationToken)
    {
        if (requesterId is null)
        {
            return false;
        }

        var query =
            $"{BuildUrl(credentials.BaseUrl, "search/Ticket")}" +
            $"?criteria[0][field]={GlpiSearchFields.TicketId}" +
            $"&criteria[0][searchtype]=equals" +
            $"&criteria[0][value]={Uri.EscapeDataString(ticketId)}" +
            $"&criteria[1][link]=AND" +
            $"&criteria[1][field]={GlpiSearchFields.TicketRequester}" +
            $"&criteria[1][searchtype]=equals" +
            $"&criteria[1][value]={Uri.EscapeDataString(requesterId)}" +
            $"&range=0-0";

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0;
    }

    private async Task<string?> ResolveUserIdAsync(
        GlpiRuntimeCredentials credentials,
        string requesterEmail,
        CancellationToken cancellationToken)
    {
        var email = requesterEmail.Trim();
        var emailLower = email.ToLowerInvariant();
        var login = emailLower.Split('@')[0];

        foreach (var candidate in new[] { email, emailLower })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var byUserEmailRecord = await SearchUserIdByEmailRecordAsync(credentials, candidate, cancellationToken);
            if (byUserEmailRecord is not null)
            {
                return byUserEmailRecord;
            }

            var byEmailContains = await SearchUserIdAsync(
                credentials,
                GlpiSearchFields.UserEmail,
                candidate,
                "contains",
                cancellationToken);
            if (byEmailContains is not null)
            {
                return byEmailContains;
            }

            var byEmailEquals = await SearchUserIdAsync(
                credentials,
                GlpiSearchFields.UserEmail,
                candidate,
                "equals",
                cancellationToken);
            if (byEmailEquals is not null)
            {
                return byEmailEquals;
            }
        }

        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        foreach (var candidate in new[] { login, email, emailLower })
        {
            var byLogin = await SearchUserIdAsync(
                credentials,
                GlpiSearchFields.UserLogin,
                candidate,
                "equals",
                cancellationToken);
            if (byLogin is not null)
            {
                return byLogin;
            }
        }

        return null;
    }

    private async Task<string?> SearchUserIdByEmailRecordAsync(
        GlpiRuntimeCredentials credentials,
        string email,
        CancellationToken cancellationToken)
    {
        foreach (var searchType in new[] { "equals", "contains" })
        {
            var query =
                $"{BuildUrl(credentials.BaseUrl, "search/UserEmail")}" +
                $"?criteria[0][field]={GlpiSearchFields.UserEmailRecordEmail}" +
                $"&criteria[0][searchtype]={searchType}" +
                $"&criteria[0][value]={Uri.EscapeDataString(email)}" +
                $"&forcedisplay[0]={GlpiSearchFields.UserEmailRecordUserId}";

            var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, query);
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
            {
                continue;
            }

            var first = data[0];
            if (first.TryGetProperty(GlpiSearchFields.UserEmailRecordUserId.ToString(), out var idElement))
            {
                return idElement.ToString();
            }
        }

        return null;
    }

    private async Task<string?> SearchUserIdAsync(
        GlpiRuntimeCredentials credentials,
        int field,
        string value,
        string searchType,
        CancellationToken cancellationToken)
    {
        var query =
            $"{BuildUrl(credentials.BaseUrl, "search/User")}" +
            $"?criteria[0][field]={field}" +
            $"&criteria[0][searchtype]={searchType}" +
            $"&criteria[0][value]={Uri.EscapeDataString(value)}" +
            $"&forcedisplay[0]={GlpiSearchFields.UserId}";

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
        {
            return null;
        }

        var first = data[0];
        if (first.TryGetProperty(GlpiSearchFields.UserId.ToString(), out var idElement))
        {
            return idElement.ToString();
        }

        return null;
    }

    private static string BuildTicketSearchQuery(
        string baseUrl,
        string? requesterId,
        GlpiTicketScope scope,
        bool includeRequester)
    {
        var query = $"{BuildUrl(baseUrl, "search/Ticket")}?";
        var criteriaIndex = 0;

        if (!string.IsNullOrWhiteSpace(requesterId))
        {
            query +=
                $"criteria[{criteriaIndex}][field]={GlpiSearchFields.TicketRequester}" +
                $"&criteria[{criteriaIndex}][searchtype]=equals" +
                $"&criteria[{criteriaIndex}][value]={Uri.EscapeDataString(requesterId)}";
            criteriaIndex++;
        }

        if (scope == GlpiTicketScope.Open)
        {
            if (criteriaIndex > 0)
            {
                query += $"&criteria[{criteriaIndex}][link]=AND";
            }

            query +=
                $"&criteria[{criteriaIndex}][field]={GlpiSearchFields.TicketStatus}" +
                $"&criteria[{criteriaIndex}][searchtype]=lessthan" +
                $"&criteria[{criteriaIndex}][value]=5";
            criteriaIndex++;
        }
        else if (scope == GlpiTicketScope.Last90Days)
        {
            var since = DateTime.UtcNow.AddDays(-90).ToString("yyyy-MM-dd HH:mm:ss");
            if (criteriaIndex > 0)
            {
                query += $"&criteria[{criteriaIndex}][link]=AND";
            }

            query +=
                $"&criteria[{criteriaIndex}][field]={GlpiSearchFields.TicketDateOpening}" +
                $"&criteria[{criteriaIndex}][searchtype]=morethan" +
                $"&criteria[{criteriaIndex}][value]={Uri.EscapeDataString(since)}";
            criteriaIndex++;
        }

        query +=
            $"&forcedisplay[0]={GlpiSearchFields.TicketId}" +
            $"&forcedisplay[1]={GlpiSearchFields.TicketTitle}" +
            $"&forcedisplay[2]={GlpiSearchFields.TicketStatus}" +
            $"&forcedisplay[3]={GlpiSearchFields.TicketPriority}" +
            $"&forcedisplay[4]={GlpiSearchFields.TicketDateOpening}";

        if (includeRequester)
        {
            query += $"&forcedisplay[5]={GlpiSearchFields.TicketRequester}";
        }

        query += "&sort=19&order=DESC";
        return query;
    }

    private static GlpiTicketSummary MapSearchRow(
        JsonElement row,
        GlpiRuntimeCredentials credentials,
        bool includeRequester)
    {
        var ticketId = ReadRowField(row, GlpiSearchFields.TicketId);
        var status = ReadRowField(row, GlpiSearchFields.TicketStatus);
        var priority = ReadRowField(row, GlpiSearchFields.TicketPriority);
        var createdRaw = ReadRowField(row, GlpiSearchFields.TicketDateOpening);

        return new GlpiTicketSummary
        {
            TicketId = ticketId,
            Subject = ReadRowField(row, GlpiSearchFields.TicketTitle),
            Status = status,
            StatusLabel = GlpiStatusMapper.StatusLabel(status),
            PriorityLabel = GlpiStatusMapper.PriorityLabel(priority),
            CreatedAt = ParseGlpiDate(createdRaw),
            Url = BuildTicketUrl(credentials, ticketId),
            RequesterLabel = includeRequester ? ReadRowField(row, GlpiSearchFields.TicketRequester) : null,
        };
    }

    private static string ReadRowField(JsonElement row, int fieldId)
    {
        if (row.TryGetProperty(fieldId.ToString(), out var value))
        {
            return value.ToString();
        }

        return string.Empty;
    }

    private static void ApplySessionHeaders(
        HttpRequestMessage request,
        GlpiRuntimeCredentials credentials,
        string sessionToken)
    {
        request.Headers.Add("App-Token", credentials.AppToken);
        request.Headers.Add("Session-Token", sessionToken);
    }

    private static string BuildUrl(string baseUrl, string path) =>
        $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

    private static string BuildTicketUrl(GlpiRuntimeCredentials credentials, string ticketId) =>
        $"{credentials.PortalUrl}/front/ticket.form.php?id={ticketId}";

    private static string ReadElement(JsonElement? element) =>
        element?.ToString() ?? string.Empty;

    private static DateTimeOffset ParseGlpiDate(string raw)
    {
        if (DateTimeOffset.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private static int MapFormPriority(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            "urgente" or "critica" or "crítica" => 5,
            "alta" => 4,
            "media" or "média" => 3,
            "baixa" => 2,
            _ => 3,
        };
    }
}
