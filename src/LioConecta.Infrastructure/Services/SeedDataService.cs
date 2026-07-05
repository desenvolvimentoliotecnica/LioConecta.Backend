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
            await EnsureGroupsCatalogAsync(cancellationToken);
            await EnsurePayslipsCatalogAsync(cancellationToken);
            await EnsureBenefitsCatalogAsync(cancellationToken);
            await EnsureLeaveCatalogAsync(cancellationToken);
            await EnsurePollSeedAsync(cancellationToken);
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
                Type = PostType.Poll,
                Content = "Qual tema você gostaria para a próxima palestra de liderança?",
                MetadataJson = "{\"heroImageUrl\":\"/bg-poll.png\"}",
                IsPinned = false,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            }
        };

        var pollSeedId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0001");
        var poll = new Poll
        {
            Id = pollSeedId,
            PostId = SeedIds.FeedPostPoll,
            Question = "Qual tema você gostaria para a próxima palestra de liderança?",
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-2),
            Options =
            [
                new PollOption
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0002"),
                    PollId = pollSeedId,
                    Text = "Gestão de conflitos",
                    SortOrder = 0,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2)
                },
                new PollOption
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0003"),
                    PollId = pollSeedId,
                    Text = "Comunicação assertiva",
                    SortOrder = 1,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2)
                },
                new PollOption
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0004"),
                    PollId = pollSeedId,
                    Text = "Inteligência emocional",
                    SortOrder = 2,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2)
                }
            ]
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
        db.Polls.Add(poll);
        db.Notifications.AddRange(notifications);

        await db.SaveChangesAsync(cancellationToken);
        await EnsureGroupsCatalogAsync(cancellationToken);
        logger.LogInformation("Seed completed with {People} people.", people.Length);
    }

    private async Task EnsureGroupsCatalogAsync(CancellationToken cancellationToken)
    {
        var existingIds = await db.Groups
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
        var existingIdSet = existingIds.ToHashSet();
        var reviewedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var added = 0;

        foreach (var entry in GroupCatalogSeed.Entries)
        {
            if (existingIdSet.Contains(entry.Id))
            {
                continue;
            }

            db.Groups.Add(GroupCatalogSeed.ToGroupEntity(entry, reviewedAt));
            db.GroupMembers.AddRange(GroupCatalogSeed.ToMembers(entry));
            added++;
        }

        if (added == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} catalog groups.", added);
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

    private async Task EnsurePayslipsCatalogAsync(CancellationToken cancellationToken)
    {
        var mariaId = SeedIds.MariaSilva;
        var hasPayslips = await db.Payslips.AnyAsync(p => p.PersonId == mariaId, cancellationToken);
        if (hasPayslips)
        {
            return;
        }

        var seedTime = DateTimeOffset.UtcNow.AddDays(-30);
        foreach (var payslip in PayslipCatalogSeed.BuildPayslips(mariaId, seedTime))
        {
            db.Payslips.Add(payslip);
        }

        var hasInforme = await db.IncomeStatements.AnyAsync(
            i => i.PersonId == mariaId && i.Year == 2025,
            cancellationToken);
        if (!hasInforme)
        {
            db.IncomeStatements.Add(PayslipCatalogSeed.BuildIncomeStatement2025(mariaId, seedTime));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded payslip catalog for Maria Silva.");
    }

    private async Task EnsureBenefitsCatalogAsync(CancellationToken cancellationToken)
    {
        var mariaId = SeedIds.MariaSilva;
        var hasBenefits = await db.EmployeeBenefits.AnyAsync(b => b.PersonId == mariaId, cancellationToken);
        if (hasBenefits)
        {
            return;
        }

        var seedTime = DateTimeOffset.UtcNow.AddDays(-30);
        foreach (var benefit in BenefitCatalogSeed.BuildForPerson(mariaId, seedTime))
        {
            db.EmployeeBenefits.Add(benefit);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded benefits catalog for Maria Silva.");
    }

    private async Task EnsureLeaveCatalogAsync(CancellationToken cancellationToken)
    {
        var mariaId = SeedIds.MariaSilva;
        var hasBalance = await db.EmployeeLeaveBalances.AnyAsync(b => b.PersonId == mariaId, cancellationToken);
        if (hasBalance)
        {
            return;
        }

        var seedTime = DateTimeOffset.UtcNow.AddDays(-30);
        db.EmployeeLeaveBalances.Add(LeaveCatalogSeed.BuildBalanceForPerson(mariaId, seedTime));

        foreach (var record in LeaveCatalogSeed.BuildRecordsForPerson(mariaId, seedTime))
        {
            db.LeaveRecords.Add(record);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded leave catalog for Maria Silva.");
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

    private async Task EnsurePollSeedAsync(CancellationToken cancellationToken)
    {
        var hasPoll = await db.Polls.AnyAsync(p => p.PostId == SeedIds.FeedPostPoll, cancellationToken);
        if (hasPoll)
        {
            return;
        }

        var postExists = await db.FeedPosts.AnyAsync(p => p.Id == SeedIds.FeedPostPoll, cancellationToken);
        if (!postExists)
        {
            var now = DateTimeOffset.UtcNow.AddDays(-2);
            db.FeedPosts.Add(new FeedPost
            {
                Id = SeedIds.FeedPostPoll,
                AuthorId = SeedIds.MariaSilva,
                Type = PostType.Poll,
                Content = "Qual tema você gostaria para a próxima palestra de liderança?",
                MetadataJson = "{\"heroImageUrl\":\"/bg-poll.png\"}",
                IsPinned = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var post = await db.FeedPosts.FirstAsync(p => p.Id == SeedIds.FeedPostPoll, cancellationToken);
            post.Type = PostType.Poll;
            post.Content = "Qual tema você gostaria para a próxima palestra de liderança?";
            post.MetadataJson = "{\"heroImageUrl\":\"/bg-poll.png\"}";
            post.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var pollSeedId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0001");
        var seededAt = DateTimeOffset.UtcNow.AddDays(-2);
        db.Polls.Add(new Poll
        {
            Id = pollSeedId,
            PostId = SeedIds.FeedPostPoll,
            Question = "Qual tema você gostaria para a próxima palestra de liderança?",
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
            Options =
            [
                new PollOption
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0002"),
                    PollId = pollSeedId,
                    Text = "Gestão de conflitos",
                    SortOrder = 0,
                    CreatedAt = seededAt,
                    UpdatedAt = seededAt
                },
                new PollOption
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0003"),
                    PollId = pollSeedId,
                    Text = "Comunicação assertiva",
                    SortOrder = 1,
                    CreatedAt = seededAt,
                    UpdatedAt = seededAt
                },
                new PollOption
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0004"),
                    PollId = pollSeedId,
                    Text = "Inteligência emocional",
                    SortOrder = 2,
                    CreatedAt = seededAt,
                    UpdatedAt = seededAt
                }
            ]
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded sample poll for feed.");
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
