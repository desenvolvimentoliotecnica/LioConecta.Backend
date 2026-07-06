using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class GraphPlannerAdapter(HttpClient httpClient, ILogger<GraphPlannerAdapter> logger)
    : IPlannerAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<PlannerGraphPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"planner/plans/{planId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new PlannerGraphPlan
        {
            Id = ReadRequiredString(root, "id"),
            Title = ReadString(root, "title") ?? planId,
        };
    }

    public async Task<IReadOnlyList<PlannerGraphBucket>> GetBucketsAsync(
        string planId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"planner/plans/{planId}/buckets", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var buckets = new List<PlannerGraphBucket>();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.TryGetProperty("value", out var valueElement))
        {
            foreach (var item in valueElement.EnumerateArray())
            {
                buckets.Add(new PlannerGraphBucket
                {
                    Id = ReadRequiredString(item, "id"),
                    Name = ReadString(item, "name") ?? "Coluna",
                    OrderHint = ReadString(item, "orderHint"),
                });
            }
        }

        return buckets
            .OrderBy(b => b.OrderHint ?? string.Empty, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<PlannerGraphTask>> GetPlanTasksAsync(
        string planId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"planner/plans/{planId}/tasks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var taskIds = new List<(string Id, JsonElement Element)>();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.TryGetProperty("value", out var valueElement))
        {
            foreach (var item in valueElement.EnumerateArray())
            {
                var id = ReadRequiredString(item, "id");
                taskIds.Add((id, item.Clone()));
            }
        }

        var semaphore = new SemaphoreSlim(8);
        var tasks = new PlannerGraphTask?[taskIds.Count];
        var workers = taskIds.Select(async (entry, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var mapped = MapTask(entry.Element);
                var details = await GetTaskDetailsInternalAsync(entry.Id, cancellationToken);
                mapped.Description = details.Description;
                mapped.Checklist = details.Checklist;
                tasks[index] = mapped;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(workers);
        return tasks.Where(t => t is not null).Cast<PlannerGraphTask>().ToList();
    }

    public async Task<PlannerGraphTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"planner/tasks/{taskId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var mapped = MapTask(document.RootElement);
        var details = await GetTaskDetailsInternalAsync(taskId, cancellationToken);
        mapped.Description = details.Description;
        mapped.Checklist = details.Checklist;
        return mapped;
    }

    public async Task<PlannerGraphTask> CreateTaskAsync(
        string planId,
        string bucketId,
        string title,
        IReadOnlyList<string> assigneeIds,
        DateTimeOffset? startDateTime,
        DateTimeOffset? dueDateTime,
        int percentComplete,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["planId"] = planId,
            ["bucketId"] = bucketId,
            ["title"] = title.Trim(),
        };

        if (startDateTime.HasValue)
        {
            payload["startDateTime"] = startDateTime.Value.ToString("o");
        }

        if (dueDateTime.HasValue)
        {
            payload["dueDateTime"] = dueDateTime.Value.ToString("o");
        }

        if (percentComplete > 0)
        {
            payload["percentComplete"] = percentComplete;
        }

        if (assigneeIds.Count > 0)
        {
            var assignments = new Dictionary<string, object>();
            foreach (var assigneeId in assigneeIds)
            {
                assignments[assigneeId] = new Dictionary<string, object>
                {
                    ["@odata.type"] = "#microsoft.graph.plannerAssignment",
                    ["orderHint"] = " !",
                };
            }

            payload["assignments"] = assignments;
        }

        using var createResponse = await SendJsonAsync(HttpMethod.Post, "planner/tasks", payload, null, cancellationToken);
        await EnsureSuccessAsync(createResponse, cancellationToken);

        await using var stream = await createResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var created = MapTask(document.RootElement);

        if (!string.IsNullOrWhiteSpace(description) || checklist is { Count: > 0 })
        {
            await UpdateTaskDetailsAsync(
                created.Id,
                description,
                checklist,
                cancellationToken);
        }

        return (await GetTaskAsync(created.Id, cancellationToken)) ?? created;
    }

    public async Task<PlannerGraphTask> UpdateTaskAsync(
        string taskId,
        string? title,
        string? bucketId,
        DateTimeOffset? startDateTime,
        DateTimeOffset? dueDateTime,
        int? percentComplete,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken = default)
    {
        var current = await GetTaskAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Planner task {taskId} was not found.");

        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            payload["title"] = title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(bucketId))
        {
            payload["bucketId"] = bucketId;
        }

        if (startDateTime.HasValue)
        {
            payload["startDateTime"] = startDateTime.Value.ToString("o");
        }

        if (dueDateTime.HasValue)
        {
            payload["dueDateTime"] = dueDateTime.Value.ToString("o");
        }

        if (percentComplete.HasValue)
        {
            payload["percentComplete"] = percentComplete.Value;
        }

        if (payload.Count > 0)
        {
            using var patchResponse = await SendJsonAsync(
                HttpMethod.Patch,
                $"planner/tasks/{taskId}",
                payload,
                current.Etag,
                cancellationToken);

            if (patchResponse.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                current = await GetTaskAsync(taskId, cancellationToken)
                    ?? throw new InvalidOperationException($"Planner task {taskId} was not found.");
                using var retryResponse = await SendJsonAsync(
                    HttpMethod.Patch,
                    $"planner/tasks/{taskId}",
                    payload,
                    current.Etag,
                    cancellationToken);
                await EnsureSuccessAsync(retryResponse, cancellationToken);
            }
            else
            {
                await EnsureSuccessAsync(patchResponse, cancellationToken);
            }
        }

        if (description is not null || checklist is not null)
        {
            await UpdateTaskDetailsAsync(taskId, description, checklist, cancellationToken);
        }

        return (await GetTaskAsync(taskId, cancellationToken)) ?? current;
    }

    public async Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var current = await GetTaskAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Planner task {taskId} was not found.");

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"planner/tasks/{taskId}");
        request.Headers.TryAddWithoutValidation("If-Match", current.Etag);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            current = await GetTaskAsync(taskId, cancellationToken)
                ?? throw new InvalidOperationException($"Planner task {taskId} was not found.");
            using var retryRequest = new HttpRequestMessage(HttpMethod.Delete, $"planner/tasks/{taskId}");
            retryRequest.Headers.TryAddWithoutValidation("If-Match", current.Etag);
            using var retryResponse = await httpClient.SendAsync(retryRequest, cancellationToken);
            await EnsureSuccessAsync(retryResponse, cancellationToken);
            return;
        }

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<PlannerGraphUser?> ResolveUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim();
        using var response = await httpClient.GetAsync(
            $"users/{Uri.EscapeDataString(normalized)}?$select=id,displayName,mail,userPrincipalName",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!Guid.TryParse(ReadString(root, "id"), out var userId))
        {
            return null;
        }

        return new PlannerGraphUser
        {
            Id = userId,
            DisplayName = ReadString(root, "displayName") ?? normalized,
            Mail = ReadString(root, "mail"),
            UserPrincipalName = ReadString(root, "userPrincipalName"),
        };
    }

    private async Task UpdateTaskDetailsAsync(
        string taskId,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken)
    {
        var details = await GetTaskDetailsRawAsync(taskId, cancellationToken);
        if (details is null)
        {
            throw new InvalidOperationException($"Planner task details for {taskId} were not found.");
        }

        var payload = new Dictionary<string, object?>();
        if (description is not null)
        {
            payload["description"] = description;
        }

        if (checklist is not null)
        {
            var checklistPayload = new Dictionary<string, object>();
            foreach (var item in checklist)
            {
                checklistPayload[item.Id] = new Dictionary<string, object>
                {
                    ["@odata.type"] = "#microsoft.graph.plannerChecklistItem",
                    ["title"] = item.Title,
                    ["isChecked"] = item.IsChecked,
                };
            }

            payload["checklist"] = checklistPayload;
        }

        if (payload.Count == 0)
        {
            return;
        }

        using var patchResponse = await SendJsonAsync(
            HttpMethod.Patch,
            $"planner/tasks/{taskId}/details",
            payload,
            details.Value.Etag,
            cancellationToken);

        if (patchResponse.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            details = await GetTaskDetailsRawAsync(taskId, cancellationToken)
                ?? throw new InvalidOperationException($"Planner task details for {taskId} were not found.");
            using var retryResponse = await SendJsonAsync(
                HttpMethod.Patch,
                $"planner/tasks/{taskId}/details",
                payload,
                details.Value.Etag,
                cancellationToken);
            await EnsureSuccessAsync(retryResponse, cancellationToken);
            return;
        }

        await EnsureSuccessAsync(patchResponse, cancellationToken);
    }

    private async Task<(string Description, IReadOnlyList<PlannerGraphChecklistItem> Checklist, string Etag)?> GetTaskDetailsRawAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"planner/tasks/{taskId}/details", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogDebug("Planner details unavailable for task {TaskId} ({StatusCode}).", taskId, (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var mapped = MapTaskDetails(document.RootElement);
        var etag = ReadString(document.RootElement, "@odata.etag") ?? string.Empty;
        return (mapped.Description, mapped.Checklist, etag);
    }

    private async Task<(string Description, IReadOnlyList<PlannerGraphChecklistItem> Checklist)> GetTaskDetailsInternalAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        var details = await GetTaskDetailsRawAsync(taskId, cancellationToken);
        return details is null ? (string.Empty, []) : (details.Value.Description, details.Value.Checklist);
    }

    private static PlannerGraphTask MapTask(JsonElement item)
    {
        var assigneeIds = new List<string>();
        if (item.TryGetProperty("assignments", out var assignmentsElement)
            && assignmentsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in assignmentsElement.EnumerateObject())
            {
                assigneeIds.Add(property.Name);
            }
        }

        return new PlannerGraphTask
        {
            Id = ReadRequiredString(item, "id"),
            PlanId = ReadString(item, "planId") ?? string.Empty,
            BucketId = ReadString(item, "bucketId") ?? string.Empty,
            Title = ReadString(item, "title") ?? string.Empty,
            PercentComplete = item.TryGetProperty("percentComplete", out var percentElement)
                && percentElement.TryGetInt32(out var percent)
                ? percent
                : 0,
            StartDateTime = ReadDateTimeOffset(item, "startDateTime"),
            DueDateTime = ReadDateTimeOffset(item, "dueDateTime"),
            CreatedDateTime = ReadDateTimeOffset(item, "createdDateTime") ?? DateTimeOffset.UtcNow,
            CompletedDateTime = ReadDateTimeOffset(item, "completedDateTime"),
            AssigneeIds = assigneeIds,
            Etag = ReadString(item, "@odata.etag") ?? string.Empty,
        };
    }

    private static (string Description, IReadOnlyList<PlannerGraphChecklistItem> Checklist) MapTaskDetails(JsonElement item)
    {
        var description = ReadString(item, "description") ?? string.Empty;
        var checklist = new List<PlannerGraphChecklistItem>();

        if (item.TryGetProperty("checklist", out var checklistElement)
            && checklistElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in checklistElement.EnumerateObject())
            {
                var value = property.Value;
                checklist.Add(new PlannerGraphChecklistItem
                {
                    Id = property.Name,
                    Title = ReadString(value, "title") ?? string.Empty,
                    IsChecked = value.TryGetProperty("isChecked", out var checkedElement)
                                && checkedElement.ValueKind == JsonValueKind.True,
                });
            }
        }

        return (description, checklist);
    }

    private async Task<HttpResponseMessage> SendJsonAsync(
        HttpMethod method,
        string path,
        object payload,
        string? etag,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(etag))
        {
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Microsoft Graph Planner request failed ({(int)response.StatusCode}): {TryReadGraphError(body)}");
    }

    private static string? ReadString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string ReadRequiredString(JsonElement item, string propertyName) =>
        ReadString(item, propertyName) ?? throw new InvalidOperationException($"Missing property '{propertyName}'.");

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement item, string propertyName)
    {
        var value = ReadString(item, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var utc))
        {
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        return null;
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
