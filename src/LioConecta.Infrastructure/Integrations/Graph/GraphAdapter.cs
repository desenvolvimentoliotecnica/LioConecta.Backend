using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string EmailDomain { get; set; } = "liotecnica.com.br";
}

public sealed class GraphAdapter(
    HttpClient httpClient,
    IOptions<GraphOptions> options,
    ILogger<GraphAdapter> logger) : IGraphAdapter
{
    private const string DirectoryUserSelect =
        "id,displayName,userPrincipalName,mail,jobTitle,department,mobilePhone,businessPhones,officeLocation,employeeId,accountEnabled";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<GraphDirectoryUser>> GetDirectoryUsersAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.TenantId))
        {
            logger.LogWarning("Graph TenantId is not configured.");
            return [];
        }

        var domain = options.Value.EmailDomain.Trim().TrimStart('@').ToLowerInvariant();
        var users = new List<GraphDirectoryUser>();
        var url =
            $"users?$select={DirectoryUserSelect}&$expand=manager($select=id)&$top=999&$orderby=displayName";

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Graph directory request failed ({(int)response.StatusCode}): {errorBody}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (root.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var mapped = MapDirectoryUser(item, domain);
                    if (mapped is not null)
                    {
                        users.Add(mapped);
                    }
                }
            }

            url = root.TryGetProperty("@odata.nextLink", out var nextLink)
                ? nextLink.GetString()
                : null;
        }

        return users;
    }

    private static GraphDirectoryUser? MapDirectoryUser(JsonElement item, string domain)
    {
        if (!item.TryGetProperty("id", out var idElement)
            || !Guid.TryParse(idElement.GetString(), out var objectId))
        {
            return null;
        }

        var upn = ReadString(item, "userPrincipalName");
        var mail = ReadString(item, "mail");
        if (!BelongsToDomain(upn, mail, domain))
        {
            return null;
        }

        Guid? managerObjectId = null;
        if (item.TryGetProperty("manager", out var managerElement)
            && managerElement.ValueKind == JsonValueKind.Object
            && managerElement.TryGetProperty("id", out var managerIdElement)
            && Guid.TryParse(managerIdElement.GetString(), out var parsedManagerId))
        {
            managerObjectId = parsedManagerId;
        }

        var businessPhones = new List<string>();
        if (item.TryGetProperty("businessPhones", out var phonesElement)
            && phonesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var phone in phonesElement.EnumerateArray())
            {
                var value = phone.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    businessPhones.Add(value);
                }
            }
        }

        var accountEnabled = !item.TryGetProperty("accountEnabled", out var enabledElement)
                             || enabledElement.ValueKind != JsonValueKind.False;

        return new GraphDirectoryUser
        {
            ObjectId = objectId,
            DisplayName = ReadString(item, "displayName") ?? upn ?? mail ?? objectId.ToString(),
            UserPrincipalName = upn,
            Mail = mail,
            JobTitle = ReadString(item, "jobTitle"),
            Department = ReadString(item, "department"),
            MobilePhone = ReadString(item, "mobilePhone"),
            BusinessPhones = businessPhones,
            OfficeLocation = ReadString(item, "officeLocation"),
            EmployeeId = ReadString(item, "employeeId"),
            AccountEnabled = accountEnabled,
            ManagerObjectId = managerObjectId,
        };
    }

    private static bool BelongsToDomain(string? upn, string? mail, string domain)
    {
        var suffix = "@" + domain;
        return (!string.IsNullOrWhiteSpace(upn) && upn.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
               || (!string.IsNullOrWhiteSpace(mail) && mail.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    public async Task SyncUserPhotosAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.TenantId))
        {
            logger.LogWarning("Graph TenantId is not configured.");
            return;
        }

        using var response = await httpClient.PostAsync("users/photos/sync", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<GraphDocument>> GetDocumentsAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(category)
            ? "documents"
            : $"documents?category={Uri.EscapeDataString(category)}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var documents = await response.Content.ReadFromJsonAsync<List<GraphDocument>>(JsonOptions, cancellationToken);
        return documents ?? [];
    }

    public async Task<IReadOnlyList<GraphCalendarEvent>> GetCalendarEventsAsync(
        Guid personId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"users/{personId}/calendar?from={from:O}&to={to:O}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<GraphCalendarEvent>>(JsonOptions, cancellationToken);
        return events ?? [];
    }

    public async Task<IReadOnlyList<GraphPlannerTask>> GetPlannerTasksAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"users/{personId}/planner/tasks", cancellationToken);
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<GraphPlannerTask>>(JsonOptions, cancellationToken);
        return tasks ?? [];
    }

    public async Task<string?> GetUserPresenceAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"users/{personId}/presence", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOptions, cancellationToken);
        return payload?.GetValueOrDefault("availability");
    }
}
