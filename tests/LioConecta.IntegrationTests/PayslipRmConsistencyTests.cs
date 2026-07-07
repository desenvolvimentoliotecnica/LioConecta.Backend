using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;

namespace LioConecta.IntegrationTests;

/// <summary>
/// Validates payslip API responses against the live backend and RM-synced cache.
/// Requires LIO_DEV_BASE_URL and RM connectivity — no mocks.
/// </summary>
public class PayslipRmConsistencyTests
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("LIO_API_BASE_URL")
        ?? Environment.GetEnvironmentVariable("LIO_DEV_BASE_URL")
        ?? "http://localhost:5148";

    private static string Email =>
        Environment.GetEnvironmentVariable("LIO_DEV_LOGIN_EMAIL") ?? "leonardo.mendes@liotecnica.com.br";

    private static string Password =>
        Environment.GetEnvironmentVariable("LIO_DEV_LOGIN_PASSWORD") ?? "ChangeMe@2026";

    [Fact]
    public async Task Payslip_summary_has_sync_metadata_and_history()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var summary = await client.GetFromJsonAsync<PayslipSummaryDto>("/api/v1/rh/payslips/summary");
        Assert.NotNull(summary);
        Assert.False(string.IsNullOrWhiteSpace(summary!.LatestCompetence));
        Assert.NotNull(summary.SyncedAt);
        Assert.False(string.IsNullOrWhiteSpace(summary.DataSource));

        var history = await client.GetFromJsonAsync<List<PayslipListItemDto>>("/api/v1/rh/payslips?limit=12");
        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task Payslip_detail_and_pdf_match_latest_competence()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var history = await client.GetFromJsonAsync<List<PayslipListItemDto>>("/api/v1/rh/payslips?limit=12");
        Assert.NotNull(history);
        Assert.NotEmpty(history);

        var latest = history![0];
        var detail = await client.GetFromJsonAsync<PayslipDetailDto>(
            $"/api/v1/rh/payslips/{latest.Year}/{latest.Month}");
        Assert.NotNull(detail);
        Assert.Equal(latest.GrossAmount, detail!.GrossAmount, precision: 2);
        Assert.Equal(latest.NetAmount, detail.NetAmount, precision: 2);

        var pdfResponse = await client.GetAsync($"/api/v1/rh/payslips/{latest.Year}/{latest.Month}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.True(pdfBytes.Length > 128);
    }

    [Fact]
    public async Task Income_statement_returns_rm_synced_values()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var year = DateTime.UtcNow.Year - 1;
        var informe = await client.GetAsync($"/api/v1/rh/payslips/informe/{year}");
        Assert.Equal(HttpStatusCode.OK, informe.StatusCode);

        var payload = await informe.Content.ReadFromJsonAsync<IncomeStatementDto>();
        Assert.NotNull(payload);
        Assert.Equal(year, payload!.Year);
        Assert.NotEmpty(payload.Lines);
        Assert.True(payload.TotalPaid >= 0m);
        Assert.True(payload.TotalWithheld >= 0m);
    }

    [Fact]
    public async Task Fgts_consulta_returns_rm_deposits()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var fgts = await client.GetFromJsonAsync<FgtsConsultaDto>("/api/v1/rh/payslips/consultas/fgts");
        Assert.NotNull(fgts);
        Assert.NotEmpty(fgts!.Deposits);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60),
        };

        var loginResponse = await client.PostAsJsonAsync(
            "api/v1/auth/login",
            new LoginRequest(Email, Password));

        Assert.True(
            loginResponse.IsSuccessStatusCode,
            $"Login failed ({(int)loginResponse.StatusCode}): {await loginResponse.Content.ReadAsStringAsync()}");

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return client;
    }
}
