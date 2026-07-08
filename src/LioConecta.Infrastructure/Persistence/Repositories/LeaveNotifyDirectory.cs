using System.Text.Json;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class LeaveNotifyDirectory(AppDbContext db) : ILeaveNotifyDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Person>> FindActivePeopleByPortalRolesAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (roles.Count == 0)
        {
            return [];
        }

        var roleSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        var portalUsers = await db.PortalUsers
            .AsNoTracking()
            .Where(u => u.IsActive)
            .ToListAsync(cancellationToken);

        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in portalUsers)
        {
            var userRoles = ParseRoles(user.RolesJson);
            if (userRoles.Any(r => roleSet.Contains(r)))
            {
                emails.Add(user.Email);
            }
        }

        if (emails.Count == 0)
        {
            return [];
        }

        var normalized = emails.Select(e => e.Trim().ToLowerInvariant()).ToList();
        return await db.People
            .AsNoTracking()
            .Where(p => p.IsActive && normalized.Contains(p.Email.ToLower()))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<string> ParseRoles(string? rolesJson)
    {
        if (string.IsNullOrWhiteSpace(rolesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(rolesJson, JsonOptions)?
                       .Where(r => !string.IsNullOrWhiteSpace(r))
                       .Select(r => r.Trim())
                       .ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }
}
