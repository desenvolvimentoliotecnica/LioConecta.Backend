using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class AuthEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private const string SuperAdminEmail = "leonardo.mendes@liotecnica.com.br";
    private const string BootstrapPassword = "ChangeMe@2026";

    private readonly HttpClient _client;

    public AuthEndpointTests(LioConectaWebApplicationFactory factory)
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
    public async Task Login_LocalSuperAdmin_ReturnsJwtWithClaims()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(SuperAdminEmail, BootstrapPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.True(payload.ExpiresInSeconds > 0);
        Assert.Equal(SuperAdminEmail, payload.User.Email);
        Assert.Contains("Admin", payload.User.Roles.Select(role => role.ToString()));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.AccessToken);
        Assert.Contains(jwt.Claims, claim => claim.Type == "oid" && !string.IsNullOrWhiteSpace(claim.Value));
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == "preferred_username"
                && string.Equals(claim.Value, SuperAdminEmail, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(SuperAdminEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLdapTest_ReturnsOkForAdmin()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/admin/ldap/test", new TestLdapConnectionRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LdapConnectionTestResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Message));
    }
}

[Collection("WebApp")]
public class AuthLdapLoginTests : IClassFixture<LdapMockWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthLdapLoginTests(LdapMockWebApplicationFactory factory)
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
    public async Task Login_LdapUser_ProvisionsPersonAndReturnsEmployeeRole()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(FakeLdapAuthService.LdapUserEmail, FakeLdapAuthService.LdapUserPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.Equal(FakeLdapAuthService.LdapUserEmail, payload!.User.Email);
        Assert.DoesNotContain("Admin", payload.User.Roles.Select(role => role.ToString()));
        Assert.Contains("Employee", payload.User.Roles.Select(role => role.ToString()));
    }

    [Fact]
    public async Task Login_SuperAdminWithLdapPassword_FallsBackToLdap()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("leonardo.mendes@liotecnica.com.br", FakeLdapAuthService.LdapUserPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.Contains("Admin", payload!.User.Roles.Select(role => role.ToString()));
    }
}
