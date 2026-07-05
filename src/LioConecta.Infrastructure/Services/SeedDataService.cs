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
                tags: "[\"member\"]"),
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

        var comunicados = new[]
        {
            new Comunicado
            {
                Id = SeedIds.ComunicadoSecurity,
                Kind = ComunicadoKind.Urgente,
                Title = "Campanha de segurança da informação",
                Excerpt = "Reforce boas práticas de segurança digital em todas as áreas.",
                ContentJson = "{\"body\":\"Atualize suas senhas e valide links antes de clicar.\"}",
                AuthorId = SeedIds.JulioSchwartzman,
                HeroImageUrl = "/bg-comunicado-security.png",
                IsMandatory = true,
                PublishedAt = now.AddDays(-10),
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-10)
            },
            new Comunicado
            {
                Id = SeedIds.ComunicadoBenefits,
                Kind = ComunicadoKind.Oficial,
                Title = "Novidades do programa de benefícios 2026",
                Excerpt = "Confira as atualizações do plano de saúde e vale-refeição.",
                ContentJson = "{\"body\":\"O RH publicou o guia completo de benefícios para 2026.\"}",
                AuthorId = SeedIds.CarlosMendes,
                HeroImageUrl = "/bg-benefits.png",
                IsMandatory = false,
                PublishedAt = now.AddDays(-5),
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            }
        };

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
        string tags)
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
            SkillsJson = "[]",
            Status = "active",
            IsActive = true,
            CreatedAt = seedTime,
            UpdatedAt = seedTime
        };
    }
}
