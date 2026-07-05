using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LioConecta.Infrastructure.Integrations.Totvs;

public sealed class TotvsOptions
{
    public const string SectionName = "Totvs";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

public sealed class TotvsAdapter(
    HttpClient httpClient,
    IOptions<TotvsOptions> options,
    ILogger<TotvsAdapter> logger) : ITotvsAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<TotvsEmployee>> SyncEmployeesAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            logger.LogWarning("Totvs BaseUrl is not configured.");
            return [];
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "employees");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var employees = await response.Content.ReadFromJsonAsync<List<TotvsEmployee>>(JsonOptions, cancellationToken);
        return employees ?? [];
    }

    public async Task<byte[]> GetPayslipAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"employees/{personId}/payslip/{year}/{month:D2}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<decimal> GetVacationBalanceAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"employees/{personId}/vacation-balance",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, decimal>>(JsonOptions, cancellationToken);
        return payload?.GetValueOrDefault("balance") ?? 0m;
    }

    public async Task<string> SubmitVacationRequestAsync(
        Guid personId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var body = new { startDate, endDate };
        using var response = await httpClient.PostAsJsonAsync(
            $"employees/{personId}/vacation-requests",
            body,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOptions, cancellationToken);
        return payload?.GetValueOrDefault("requestId") ?? string.Empty;
    }

    public async Task<IReadOnlyDictionary<string, object?>> GetBenefitsAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"employees/{personId}/benefits", cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>(JsonOptions, cancellationToken);
        return payload ?? new Dictionary<string, object?>();
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetTimeClockAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"employees/{personId}/time-clock?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object?>>>(JsonOptions, cancellationToken);
        return payload?.Select(d => (IReadOnlyDictionary<string, object?>)d).ToList()
            ?? [];
    }
}
