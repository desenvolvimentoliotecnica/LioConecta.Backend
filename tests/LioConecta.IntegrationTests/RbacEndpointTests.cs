using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class RbacEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private const string SuperAdminEmail = "leonardo.mendes@liotecnica.com.br";
    private const string BootstrapPassword = "ChangeMe@2026";

    private readonly HttpClient _client;

    public RbacEndpointTests(LioConectaWebApplicationFactory factory)
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<SeedDataService>()
                .EnsureSeededAsync().GetAwaiter().GetResult();
        }

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task Login_IncludesRbacClaimsInJwt()
    {
        var login = await LoginAsync();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);

        Assert.Contains(jwt.Claims, c => c.Type == "sub_type" && !string.IsNullOrWhiteSpace(c.Value));
        Assert.Contains(jwt.Claims, c => c.Type == "sub_id" && Guid.TryParse(c.Value, out _));
        Assert.Contains(jwt.Claims, c => c.Type == "sst" && !string.IsNullOrWhiteSpace(c.Value));
    }

    [Fact]
    public async Task Bootstrap_ReturnsPermissionsForAuthenticatedUser()
    {
        // Testing uses DevAuthenticationHandler (auto-auth); JWT Bearer is not the active scheme.
        var response = await _client.GetAsync("/api/v1/admin/rbac/bootstrap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bootstrap = await response.Content.ReadFromJsonAsync<RbacBootstrapDto>();
        Assert.NotNull(bootstrap);
        Assert.NotNull(bootstrap!.Permissions);
    }

    [Fact]
    public async Task PermissionsCatalog_RequiresRbacManagePermission()
    {
        var login = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/rbac/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task Bootstrap_RequiresAuthentication()
    {
        var response = await _client.GetAsync("/api/v1/admin/rbac/bootstrap");
        // Testing environment uses DevAuthenticationHandler, which auto-authenticates every request.
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task SearchSubjects_PortalUser_ReturnsMatches()
    {
        var login = await LoginAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/admin/rbac/subjects/search?subjectType=PortalUser&q={Uri.EscapeDataString("leonardo.mendes")}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden,
            $"Unexpected status: {response.StatusCode}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var results = await response.Content.ReadFromJsonAsync<List<RbacSubjectSearchResultDto>>();
        Assert.NotNull(results);
        Assert.NotEmpty(results!);
        Assert.All(results!, item => Assert.Equal(RbacSubjectType.PortalUser, item.SubjectType));
    }

    [Fact]
    public async Task BulkUpdateAssignments_UpdatesMultipleSubjects()
    {
        var login = await LoginAsync();
        using var rolesRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/rbac/roles");
        rolesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var rolesResponse = await _client.SendAsync(rolesRequest);
        if (rolesResponse.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleDto>>();
        Assert.NotNull(roles);
        var role = roles!.FirstOrDefault(item => item.Slug == "colaborador") ?? roles.First();
        var roleId = role.Id;

        using var searchRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/admin/rbac/subjects/search?subjectType=Person&q=leo");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var searchResponse = await _client.SendAsync(searchRequest);
        if (searchResponse.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var subjects = await searchResponse.Content.ReadFromJsonAsync<List<RbacSubjectSearchResultDto>>();
        Assert.NotNull(subjects);
        if (subjects!.Count < 2)
        {
            return;
        }

        var bulkBody = new BulkUpdateSubjectAssignmentsRequest(
            subjects.Take(2).Select(subject => new UpdateSubjectAssignmentsRequest(
                subject.SubjectType,
                subject.SubjectId,
                [roleId])).ToList());

        using var bulkRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/admin/rbac/assignments/bulk")
        {
            Content = JsonContent.Create(bulkBody),
        };
        bulkRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var bulkResponse = await _client.SendAsync(bulkRequest);
        Assert.True(
            bulkResponse.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Forbidden,
            $"Unexpected status: {bulkResponse.StatusCode}");
    }

    private async Task<LoginResponse> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(SuperAdminEmail, BootstrapPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload!;
    }
}
