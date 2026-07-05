using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class SeedDataService(AppDbContext db, ILogger<SeedDataService> logger)
{
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.People.AnyAsync(cancellationToken))
        {
            await EnsureRichProfilesAsync(cancellationToken);
            await EnsureComunicadosCatalogAsync(cancellationToken);
            await EnsureArchivedAtBackfillAsync(cancellationToken);
            logger.LogDebug("Database already contains people; skipping seed.");
            return;
        }

        logger.LogInformation("Seeding initial LioConecta data...");

        var now = DateTimeOffset.UtcNow;
        var seedTime = now.AddDays(-30);

        var departments = new[]
        {
            new Department
            {
                Id = SeedIds.DeptExecutiva,
                Name = "Executiva",
                Code = "executiva",
                Description = "Diretoria Executiva",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Department
            {
                Id = SeedIds.DeptProduto,
                Name = "Produto",
                Code = "produto",
                Description = "Produto e Inovação",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            }
        };

        var people = new[]
        {
            CreatePerson(
                SeedIds.JulioSchwartzman,
                "julio-schwartzman",
                "Júlio Schwartzman",
                "CEO · Diretoria Executiva",
                "Executiva",
                SeedIds.DeptExecutiva,
                "julio.schwartzman@liotecnica.com.br",
                "(19) 32850",
                "Campinas, SP · Matriz",
                "@Júlio Schwartzman",
                managerId: null,
                orgChartId: "1",
                photoUrl: "/avatar-julio-schwartzman.png",
                birthDate: new DateOnly(1992, 3, 3),
                hireDate: new DateOnly(2020, 5, 1),
                tags: "[\"ceo\"]"),
            CreatePerson(
                SeedIds.CarlosMendes,
                "carlos-mendes",
                "Carlos Mendes",
                "Diretor de Produto",
                "Produto",
                SeedIds.DeptProduto,
                "carlos.mendes@liotecnica.com.br",
                "(19) 32248",
                "Campinas, SP · Produto",
                "@Carlos Mendes",
                managerId: SeedIds.JulioSchwartzman,
                orgChartId: "2",
                photoUrl: "/avatar-carlos-mendes.png",
                birthDate: new DateOnly(1988, 7, 12),
                hireDate: new DateOnly(2020, 1, 15),
                tags: "[\"director\"]"),
            CreatePerson(
                SeedIds.MariaSilva,
                "maria-silva",
                "Maria Silva",
                "Gerente de Projetos",
                "Produto",
                SeedIds.DeptProduto,
                "maria.silva@liotecnica.com.br",
                "(19) 32033",
                "Campinas, SP · Produto",
                "@Maria Silva",
                managerId: SeedIds.CarlosMendes,
                orgChartId: "3",
                photoUrl: "/avatar-maria-silva.png",
                birthDate: new DateOnly(1990, 11, 20),
                hireDate: new DateOnly(2022, 3, 10),
                tags: "[\"member\"]",
                skillsJson: ProfileSeedContent.MariaSilvaSkillsJson,
                personalDataJson: ProfileSeedContent.MariaSilvaPersonalDataJson),
            CreatePerson(
                SeedIds.RicardoSouza,
                "ricardo-souza",
                "Ricardo Souza",
                "Product Owner",
                "Produto",
                SeedIds.DeptProduto,
                "ricardo.souza@liotecnica.com.br",
                "(19) 32270",
                "Campinas, SP · Produto",
                "@Ricardo Souza",
                managerId: SeedIds.CarlosMendes,
                orgChartId: "4",
                photoUrl: "/avatar-carlos-mendes.png",
                birthDate: new DateOnly(1991, 4, 8),
                hireDate: new DateOnly(2021, 9, 1),
                tags: "[\"member\"]"),
            CreatePerson(
                SeedIds.JuliaSantos,
                "julia-santos",
                "Julia Santos",
                "Designer de Produto",
                "Produto",
                SeedIds.DeptProduto,
                "julia.santos@liotecnica.com.br",
                "(19) 32165",
                "Campinas, SP · Produto",
                "@Julia Santos",
                managerId: SeedIds.CarlosMendes,
                orgChartId: "5",
                photoUrl: "/avatar-julia-santos.png",
                birthDate: new DateOnly(1993, 6, 18),
                hireDate: new DateOnly(2021, 3, 22),
                tags: "[\"member\"]")
        };

        var comunicados = ComunicadoCatalogSeed.CreateAll(seedTime).ToArray();

        var feedPosts = new[]
        {
            new FeedPost
            {
                Id = SeedIds.FeedPostWelcome,
                AuthorId = SeedIds.JulioSchwartzman,
                Type = PostType.Social,
                Content = "Bem-vindos ao LioConecta! Este é o nosso novo hub de colaboração interna.",
                MetadataJson = "{}",
                IsPinned = true,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7)
            },
            new FeedPost
            {
                Id = SeedIds.FeedPostPoll,
                AuthorId = SeedIds.MariaSilva,
                Type = PostType.News,
                Content = "LioTécnica lança nova iniciativa de inovação aberta com squads multidisciplinares.",
                MetadataJson = "{\"category\":\"innovation\"}",
                IsPinned = false,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            }
        };

        var notifications = new[]
        {
            new Notification
            {
                Id = Guid.NewGuid(),
                PersonId = SeedIds.MariaSilva,
                Type = NotificationType.Comunicado,
                Title = "Novo comunicado urgente",
                Body = "Campanha de segurança da informação publicada.",
                Href = "/comunicados/urgentes",
                IsRead = false,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                PersonId = SeedIds.MariaSilva,
                Type = NotificationType.Feed,
                Title = "Novo post no feed",
                Body = "Júlio Schwartzman publicou uma atualização.",
                Href = "/feed",
                IsRead = false,
                CreatedAt = now.AddHours(-6),
                UpdatedAt = now.AddHours(-6)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                PersonId = SeedIds.JuliaSantos,
                Type = NotificationType.System,
                Title = "Perfil atualizado",
                Body = "Seu perfil foi sincronizado com sucesso.",
                Href = "/pessoas/julia-santos",
                IsRead = true,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3)
            }
        };

        db.Departments.AddRange(departments);
        db.People.AddRange(people);
        db.Comunicados.AddRange(comunicados);
        db.FeedPosts.AddRange(feedPosts);
        db.Notifications.AddRange(notifications);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed completed with {People} people.", people.Length);
    }

    private async Task EnsureRichProfilesAsync(CancellationToken cancellationToken)
    {
        var maria = await db.People.FirstOrDefaultAsync(p => p.Slug == "maria-silva", cancellationToken);
        if (maria is null || !string.IsNullOrWhiteSpace(maria.PersonalDataJson))
        {
            return;
        }

        maria.PersonalDataJson = ProfileSeedContent.MariaSilvaPersonalDataJson;
        maria.SkillsJson = ProfileSeedContent.MariaSilvaSkillsJson;
        maria.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated rich profile seed data for {Slug}.", maria.Slug);
    }

    private async Task EnsureComunicadosCatalogAsync(CancellationToken cancellationToken)
    {
        var existingSlugs = await db.Comunicados
            .Where(c => c.Slug != null)
            .Select(c => c.Slug!)
            .ToListAsync(cancellationToken);

        var existingSlugSet = existingSlugs.ToHashSet(StringComparer.Ordinal);
        var seedTime = DateTimeOffset.UtcNow.AddDays(-30);
        var added = 0;

        foreach (var entry in ComunicadoCatalogSeed.Entries)
        {
            if (existingSlugSet.Contains(entry.Slug))
            {
                continue;
            }

            var existsById = await db.Comunicados.AnyAsync(c => c.Id == entry.Id, cancellationToken);
            if (existsById)
            {
                continue;
            }

            db.Comunicados.Add(ComunicadoCatalogSeed.ToEntity(entry, seedTime));
            added++;
        }

        if (added == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} catalog comunicados.", added);
    }

    private async Task EnsureArchivedAtBackfillAsync(CancellationToken cancellationToken)
    {
        var pending = await db.Comunicados
            .Where(c => c.ArchivedAt == null && c.Kind == ComunicadoKind.Arquivo)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var comunicado in pending)
        {
            comunicado.ArchivedAt = comunicado.PublishedAt ?? comunicado.CreatedAt;
            comunicado.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Backfilled ArchivedAt for {Count} comunicados.", pending.Count);
    }

    private static Person CreatePerson(
        Guid id,
        string slug,
        string name,
        string title,
        string dept,
        Guid departmentId,
        string email,
        string phone,
        string location,
        string teamsUpn,
        Guid? managerId,
        string orgChartId,
        string photoUrl,
        DateOnly birthDate,
        DateOnly hireDate,
        string tags,
        string skillsJson = "[]",
        string? personalDataJson = null)
    {
        var seedTime = DateTimeOffset.UtcNow.AddDays(-30);
        return new Person
        {
            Id = id,
            Slug = slug,
            AzureAdObjectId = id,
            Name = name,
            Title = title,
            Dept = dept,
            DepartmentId = departmentId,
            Email = email,
            Phone = phone,
            Location = location,
            TeamsUpn = teamsUpn,
            ManagerId = managerId,
            OrgChartId = orgChartId,
            PhotoUrl = photoUrl,
            BirthDate = birthDate,
            HireDate = hireDate,
            TagsJson = tags,
            SkillsJson = skillsJson,
            PersonalDataJson = personalDataJson,
            Status = "active",
            IsActive = true,
            CreatedAt = seedTime,
            UpdatedAt = seedTime
        };
    }
}
