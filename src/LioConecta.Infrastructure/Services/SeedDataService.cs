using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Security;
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
            await EnsureRmLinkedProfilesCleanAsync(cancellationToken);
            await EnsureComunicadosCatalogAsync(cancellationToken);
            await EnsureArchivedAtBackfillAsync(cancellationToken);
            await EnsureGroupsCatalogAsync(cancellationToken);
            await EnsurePayslipsCatalogAsync(cancellationToken);
            await EnsureBenefitsCatalogAsync(cancellationToken);
            await EnsureLeaveCatalogAsync(cancellationToken);
            await EnsureFacilitiesMenuCatalogAsync(cancellationToken);
            await EnsurePollSeedAsync(cancellationToken);
            await EnsureEmployeeIdsAsync(cancellationToken);
            await EnsureDevTestUserProfileAsync(cancellationToken);
            await EnsureSuperAdminPortalUserAsync(cancellationToken);
            await EnsureGraphDirectoryDepartmentLinksAsync(cancellationToken);
            await EnsureTotvsRmConfigurationAsync(cancellationToken);
            await EnsureEmailConfigurationDevDefaultsAsync(cancellationToken);
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
                "leonardo-sabino-mendes",
                "Leonardo Sabino Mendes",
                title: null,
                dept: null,
                departmentId: null,
                "leonardo.mendes@liotecnica.com.br",
                phone: null,
                location: null,
                teamsUpn: null,
                managerId: SeedIds.CarlosMendes,
                orgChartId: "3",
                photoUrl: null,
                birthDate: null,
                hireDate: null,
                tags: "[]",
                employeeId: "00000887"),
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
        await EnsureSuperAdminPortalUserAsync(cancellationToken);
        await EnsureGroupsCatalogAsync(cancellationToken);
        await EnsureFacilitiesMenuCatalogAsync(cancellationToken);
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

    private async Task EnsureRmLinkedProfilesCleanAsync(CancellationToken cancellationToken)
    {
        var rmLinkedPeople = await db.People
            .Where(p => p.EmployeeId != null && p.EmployeeId != "")
            .ToListAsync(cancellationToken);

        if (rmLinkedPeople.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var person in rmLinkedPeople)
        {
            if (!string.IsNullOrWhiteSpace(person.PersonalDataJson))
            {
                person.PersonalDataJson = null;
                changed = true;
            }

            if (!string.Equals(person.SkillsJson, "[]", StringComparison.Ordinal))
            {
                person.SkillsJson = "[]";
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cleared mock profile payloads for {Count} RM-linked people.", rmLinkedPeople.Count);
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
        var legacy = await db.Payslips
            .Where(p => p.Source == null || p.Source == "")
            .ToListAsync(cancellationToken);

        if (legacy.Count > 0)
        {
            db.Payslips.RemoveRange(legacy);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Removed {Count} legacy seed payslips.", legacy.Count);
        }

        var mariaId = SeedIds.MariaSilva;
        var hasPayslips = await db.Payslips.AnyAsync(p => p.PersonId == mariaId, cancellationToken);
        if (hasPayslips)
        {
            return;
        }

        // Holerites passam a vir exclusivamente do sync TOTVS RM; não re-seedamos dados fictícios.
        return;
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

    private async Task EnsureFacilitiesMenuCatalogAsync(CancellationToken cancellationToken)
    {
        var seedTime = DateTimeOffset.UtcNow.AddDays(-1);
        var added = 0;

        foreach (var entry in FacilitiesMenuCatalogSeed.Entries)
        {
            var existsById = await db.CafeteriaMenus.AnyAsync(m => m.Id == entry.Id, cancellationToken);
            if (existsById)
            {
                continue;
            }

            var existsByDate = await db.CafeteriaMenus.AnyAsync(m => m.Date == entry.Date, cancellationToken);
            if (existsByDate)
            {
                continue;
            }

            db.CafeteriaMenus.Add(FacilitiesMenuCatalogSeed.ToEntity(entry, seedTime));
            added++;
        }

        if (added == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded {Count} cafeteria menu day(s) for week starting {WeekStart:yyyy-MM-dd}.",
            added,
            FacilitiesMenuCatalogSeed.WeekStart);
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

    private async Task EnsureEmployeeIdsAsync(CancellationToken cancellationToken)
    {
        var devUser = await db.People.FirstOrDefaultAsync(p => p.Id == SeedIds.MariaSilva, cancellationToken);
        if (devUser is null || !string.IsNullOrWhiteSpace(devUser.EmployeeId))
        {
            return;
        }

        devUser.EmployeeId = "00000887";
        devUser.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded EmployeeId for {Slug}.", devUser.Slug);
    }

    private async Task EnsureDevTestUserProfileAsync(CancellationToken cancellationToken)
    {
        var devUser = await db.People.FirstOrDefaultAsync(p => p.Id == SeedIds.MariaSilva, cancellationToken);
        if (devUser is null)
        {
            return;
        }

        var changed = false;

        if (!string.Equals(devUser.Name, "Leonardo Sabino Mendes", StringComparison.Ordinal))
        {
            devUser.Name = "Leonardo Sabino Mendes";
            changed = true;
        }

        if (!string.Equals(devUser.Email, "leonardo.mendes@liotecnica.com.br", StringComparison.OrdinalIgnoreCase))
        {
            devUser.Email = "leonardo.mendes@liotecnica.com.br";
            changed = true;
        }

        if (!string.Equals(devUser.EmployeeId, "00000887", StringComparison.Ordinal))
        {
            devUser.EmployeeId = "00000887";
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.PersonalDataJson))
        {
            devUser.PersonalDataJson = null;
            changed = true;
        }

        if (!string.Equals(devUser.SkillsJson, "[]", StringComparison.Ordinal))
        {
            devUser.SkillsJson = "[]";
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.Title))
        {
            devUser.Title = null;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.Dept))
        {
            devUser.Dept = null;
            changed = true;
        }

        if (devUser.DepartmentId is not null)
        {
            devUser.DepartmentId = null;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.Location))
        {
            devUser.Location = null;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.Phone))
        {
            devUser.Phone = null;
            changed = true;
        }

        if (devUser.BirthDate is not null)
        {
            devUser.BirthDate = null;
            changed = true;
        }

        if (devUser.HireDate is not null)
        {
            devUser.HireDate = null;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.PhotoUrl))
        {
            devUser.PhotoUrl = null;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(devUser.TeamsUpn))
        {
            devUser.TeamsUpn = null;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        devUser.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Aligned dev test user profile to Leonardo Sabino Mendes ({Slug}).", devUser.Slug);
    }

    private async Task EnsureSuperAdminPortalUserAsync(CancellationToken cancellationToken)
    {
        const string superAdminEmail = "leonardo.mendes@liotecnica.com.br";
        var person = await db.People.FirstOrDefaultAsync(
            p => p.Email.ToLower() == superAdminEmail,
            cancellationToken);

        if (person is null)
        {
            logger.LogWarning("Super-admin person {Email} not found; skipping portal_users seed.", superAdminEmail);
            return;
        }

        var existing = await db.PortalUsers.FirstOrDefaultAsync(
            u => u.Email.ToLower() == superAdminEmail,
            cancellationToken);

        if (existing is not null)
        {
            return;
        }

        var password = Environment.GetEnvironmentVariable("PORTAL_SUPER_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "ChangeMe@2026";
            logger.LogWarning(
                "PORTAL_SUPER_ADMIN_PASSWORD not set — using default bootstrap password for {Email}.",
                superAdminEmail);
        }

        var now = DateTimeOffset.UtcNow;
        db.PortalUsers.Add(new PortalUser
        {
            Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffff01"),
            Email = superAdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PersonId = person.Id,
            RolesJson = "[\"Admin\",\"Employee\"]",
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded super-admin portal user for {Email}.", superAdminEmail);
    }

    private async Task EnsureGraphDirectoryDepartmentLinksAsync(CancellationToken cancellationToken)
    {
        var people = await db.People
            .Include(p => p.Department)
            .Where(p => p.DepartmentId != null)
            .ToListAsync(cancellationToken);

        if (people.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var person in people)
        {
            var shouldClear = person.Dept is null
                || (person.Department is not null
                    && !string.Equals(person.Dept, person.Department.Name, StringComparison.OrdinalIgnoreCase));

            if (!shouldClear)
            {
                continue;
            }

            person.DepartmentId = null;
            person.UpdatedAt = DateTimeOffset.UtcNow;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cleared stale department FK links so directory follows Graph Dept field.");
    }

    private async Task EnsureTotvsRmConfigurationAsync(CancellationToken cancellationToken)
    {
        if (await db.TotvsRmConfigurations.AnyAsync(cancellationToken))
        {
            return;
        }

        db.TotvsRmConfigurations.Add(new TotvsRmConfiguration
        {
            Id = Guid.NewGuid(),
            IsEnabled = false,
            Server = string.Empty,
            Port = 1433,
            Database = string.Empty,
            UserName = string.Empty,
            PasswordProtected = null,
            TrustServerCertificate = true,
            TimesheetPeriodStartDay = 16,
            TimesheetPeriodEndDay = 15,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default TOTVS RM configuration row.");
    }

    private async Task EnsureEmailConfigurationDevDefaultsAsync(CancellationToken cancellationToken)
    {
        var entity = await db.EmailConfigurations.FirstOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (entity is null)
        {
            entity = new EmailConfiguration
            {
                Id = Guid.NewGuid(),
                IsEnabled = EmailConfigurationDefaults.DevIsEnabled,
                FromAddress = EmailConfigurationDefaults.DevFromAddress,
                FromName = EmailConfigurationDefaults.DevFromName,
                SmtpHost = EmailConfigurationDefaults.DevSmtpHost,
                SmtpPort = EmailConfigurationDefaults.DevSmtpPort,
                SmtpUsername = EmailConfigurationDefaults.DevSmtpUsername,
                UseStartTls = EmailConfigurationDefaults.DevUseStartTls,
                TimeoutSeconds = 30,
                MaxAttempts = 5,
                InitialRetryDelaySeconds = 60,
                MaxRetryDelaySeconds = 21600,
                DispatchBatchSize = 20,
                DispatchIntervalSeconds = 30,
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.EmailConfigurations.Add(entity);
        }
        else if (string.IsNullOrWhiteSpace(entity.SmtpHost))
        {
            entity.IsEnabled = EmailConfigurationDefaults.DevIsEnabled;
            entity.FromAddress = EmailConfigurationDefaults.DevFromAddress;
            entity.FromName = EmailConfigurationDefaults.DevFromName;
            entity.SmtpHost = EmailConfigurationDefaults.DevSmtpHost;
            entity.SmtpPort = EmailConfigurationDefaults.DevSmtpPort;
            entity.SmtpUsername = EmailConfigurationDefaults.DevSmtpUsername;
            entity.UseStartTls = EmailConfigurationDefaults.DevUseStartTls;
            entity.UpdatedAt = now;
        }

        var devPassword = Environment.GetEnvironmentVariable("LIO_DEV_SMTP_PASSWORD");
        if (!string.IsNullOrWhiteSpace(devPassword) &&
            string.IsNullOrWhiteSpace(entity.SmtpPasswordProtected))
        {
            entity.SmtpPasswordProtected = SecretProtector.Protect(
                devPassword.Trim(),
                "LioConecta.Backend::EmailConfiguration::Secret::v1");
            entity.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Person CreatePerson(
        Guid id,
        string slug,
        string name,
        string? title,
        string? dept,
        Guid? departmentId,
        string email,
        string? phone,
        string? location,
        string? teamsUpn,
        Guid? managerId,
        string orgChartId,
        string? photoUrl,
        DateOnly? birthDate,
        DateOnly? hireDate,
        string tags,
        string skillsJson = "[]",
        string? personalDataJson = null,
        string? employeeId = null)
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
            EmployeeId = employeeId,
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
