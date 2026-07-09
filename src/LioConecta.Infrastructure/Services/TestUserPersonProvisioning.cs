using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

internal static class TestUserPersonProvisioning
{
    public static async Task<Person> ResolvePersonAsync(
        AppDbContext db,
        TestUser testUser,
        CancellationToken cancellationToken)
    {
        if (testUser.OptionalPersonId is Guid personId)
        {
            var linked = await db.People.FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
            if (linked is not null)
            {
                return linked;
            }
        }

        var byEmail = await db.People.FirstOrDefaultAsync(
            p => p.Email.ToLower() == testUser.Email,
            cancellationToken);
        if (byEmail is not null)
        {
            if (testUser.OptionalPersonId != byEmail.Id)
            {
                testUser.OptionalPersonId = byEmail.Id;
                testUser.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return byEmail;
        }

        var person = await CreateShadowPersonAsync(
            db,
            testUser.Email,
            testUser.DisplayName,
            cancellationToken);
        testUser.OptionalPersonId = person.Id;
        testUser.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return person;
    }

    public static async Task<Guid> ResolvePersonIdForNewTestUserAsync(
        AppDbContext db,
        string email,
        string displayName,
        Guid? optionalPersonId,
        CancellationToken cancellationToken)
    {
        if (optionalPersonId is Guid personId)
        {
            var linked = await db.People.AsNoTracking()
                .AnyAsync(p => p.Id == personId, cancellationToken);
            if (linked)
            {
                return personId;
            }
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var byEmail = await db.People.AsNoTracking()
            .Where(p => p.Email.ToLower() == normalizedEmail)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (byEmail != Guid.Empty)
        {
            return byEmail;
        }

        var person = await CreateShadowPersonAsync(db, normalizedEmail, displayName, cancellationToken);
        return person.Id;
    }

    private static async Task<Person> CreateShadowPersonAsync(
        AppDbContext db,
        string email,
        string displayName,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Slug = await GenerateUniqueSlugAsync(db, displayName, cancellationToken),
            Name = displayName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.People.Add(person);
        await db.SaveChangesAsync(cancellationToken);
        return person;
    }

    private static async Task<string> GenerateUniqueSlugAsync(
        AppDbContext db,
        string name,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var suffix = 1;

        while (await db.People.AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray()).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
