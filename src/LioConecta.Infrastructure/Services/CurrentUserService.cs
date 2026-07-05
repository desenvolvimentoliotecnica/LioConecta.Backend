using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace LioConecta.Infrastructure.Services;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db,
    IWebHostEnvironment environment) : ICurrentUserService
{
    public const string DevUserIdHeader = "X-Dev-User-Id";

    private static readonly Dictionary<string, UserRole> RoleClaimMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Employee"] = UserRole.Employee,
        ["Manager"] = UserRole.Manager,
        ["HR"] = UserRole.HR,
        ["TI"] = UserRole.TI,
        ["Facilities"] = UserRole.Facilities,
        ["Legal"] = UserRole.Legal,
        ["Admin"] = UserRole.Admin,
        ["AnalyticsViewer"] = UserRole.AnalyticsViewer,
        ["KioskReader"] = UserRole.KioskReader
    };

    public async Task<Guid> GetPersonIdAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HTTP context is available for the current user.");

        if (environment.IsDevelopment() &&
            httpContext.Request.Headers.TryGetValue(DevUserIdHeader, out var devUserId) &&
            !string.IsNullOrWhiteSpace(devUserId))
        {
            var devValue = devUserId.ToString().Trim();
            var personByDevHeader = await ResolvePersonByDevHeaderAsync(devValue, cancellationToken);
            if (personByDevHeader is not null)
            {
                return personByDevHeader.Id;
            }
        }

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var oidClaim = user.FindFirstValue("oid")
            ?? user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(oidClaim))
        {
            throw new InvalidOperationException("Azure AD object id claim was not found.");
        }

        if (!Guid.TryParse(oidClaim, out var azureAdObjectId))
        {
            throw new InvalidOperationException("Azure AD object id claim is not a valid GUID.");
        }

        var person = await db.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AzureAdObjectId == azureAdObjectId, cancellationToken);

        if (person is null)
        {
            throw new InvalidOperationException($"No person profile is linked to Azure AD object id {azureAdObjectId}.");
        }

        return person.Id;
    }

    public Task<IReadOnlyList<UserRole>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
        {
            if (environment.IsDevelopment() &&
                httpContext?.Request.Headers.ContainsKey(DevUserIdHeader) == true)
            {
                return Task.FromResult<IReadOnlyList<UserRole>>([UserRole.Employee]);
            }

            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var roles = httpContext.User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .Select(value => RoleClaimMap.TryGetValue(value, out var role) ? role : (UserRole?)null)
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .Distinct()
            .ToList();

        if (roles.Count == 0)
        {
            roles.Add(UserRole.Employee);
        }

        return Task.FromResult<IReadOnlyList<UserRole>>(roles);
    }

    public async Task<ViewerContext> GetViewerContextAsync(
        Guid targetPersonId,
        CancellationToken cancellationToken = default)
    {
        var viewerId = await GetPersonIdAsync(cancellationToken);
        if (viewerId == targetPersonId)
        {
            return ViewerContext.Self;
        }

        var roles = await GetRolesAsync(cancellationToken);
        if (roles.Contains(UserRole.Admin))
        {
            return ViewerContext.Admin;
        }

        if (roles.Contains(UserRole.HR))
        {
            return ViewerContext.HR;
        }

        return ViewerContext.Colleague;
    }

    private async Task<Domain.Entities.Person?> ResolvePersonByDevHeaderAsync(
        string devValue,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(devValue, out var personId))
        {
            return await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        }

        return await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == devValue, cancellationToken);
    }
}
