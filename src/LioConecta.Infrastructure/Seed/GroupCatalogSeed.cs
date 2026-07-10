using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Infrastructure.Seed;

internal static class GroupCatalogSeed
{
    internal sealed record Entry(
        Guid Id,
        string Name,
        string Description,
        GroupType Type,
        GroupAccessMode AccessMode,
        string Icon,
        Guid OwnerId,
        DateTimeOffset CreatedAt,
        IReadOnlyList<Guid> MemberPersonIds);

    internal static IReadOnlyList<Entry> Entries { get; } =
    [
        new(
            SeedIds.GroupEngenhariaQualidade,
            "Engenharia de Qualidade",
            "Padrões de QA, testes automatizados e boas práticas de entrega contínua.",
            GroupType.Departamental,
            GroupAccessMode.Open,
            "fa-building",
            SeedIds.CarlosMendes,
            DateTimeOffset.UtcNow.AddDays(-120),
            [SeedIds.CarlosMendes, SeedIds.MariaSilva, SeedIds.RicardoSouza, SeedIds.JuliaSantos, SeedIds.JulioSchwartzman]),
        new(
            SeedIds.GroupDataAnalytics,
            "Data & Analytics",
            "Dashboards, indicadores, BI e cultura data-driven na LioConecta.",
            GroupType.Departamental,
            GroupAccessMode.RequiresApproval,
            "fa-building",
            SeedIds.RicardoSouza,
            DateTimeOffset.UtcNow.AddDays(-90),
            [SeedIds.RicardoSouza, SeedIds.CarlosMendes, SeedIds.MariaSilva, SeedIds.JuliaSantos]),
        new(
            SeedIds.GroupCustomerSuccess,
            "Customer Success",
            "Retenção, NPS, jornada do cliente e playbooks de suporte.",
            GroupType.Departamental,
            GroupAccessMode.Open,
            "fa-building",
            SeedIds.JuliaSantos,
            DateTimeOffset.UtcNow.AddDays(-75),
            [SeedIds.JuliaSantos, SeedIds.MariaSilva, SeedIds.RicardoSouza]),
        new(
            SeedIds.GroupHackathon2026,
            "Hackathon 2026",
            "Inscrições, squads, desafios e cronograma do hackathon interno.",
            GroupType.Projeto,
            GroupAccessMode.Open,
            "fa-diagram-project",
            SeedIds.MariaSilva,
            DateTimeOffset.UtcNow.AddDays(-5),
            [SeedIds.MariaSilva, SeedIds.JuliaSantos, SeedIds.RicardoSouza, SeedIds.CarlosMendes, SeedIds.JulioSchwartzman]),
        new(
            SeedIds.GroupMentoriasLio,
            "Mentorias Lio",
            "Programa de mentoria cruzada entre áreas e trilhas de desenvolvimento.",
            GroupType.Comunidade,
            GroupAccessMode.RequiresApproval,
            "fa-users",
            SeedIds.JulioSchwartzman,
            DateTimeOffset.UtcNow.AddDays(-8),
            [SeedIds.JulioSchwartzman, SeedIds.CarlosMendes, SeedIds.MariaSilva, SeedIds.JuliaSantos]),
        new(
            SeedIds.GroupFotografiaCorp,
            "Fotografia Corporativa",
            "Registros de eventos, ensaios internos e banco de imagens da marca.",
            GroupType.Interesse,
            GroupAccessMode.Open,
            "fa-heart",
            SeedIds.JuliaSantos,
            DateTimeOffset.UtcNow.AddDays(-60),
            [SeedIds.JuliaSantos, SeedIds.MariaSilva]),
        new(
            SeedIds.GroupVoluntariadoLio,
            "Voluntariado Lio",
            "Ações sociais, campanhas de doação e projetos com ONGs parceiras.",
            GroupType.Comunidade,
            GroupAccessMode.Open,
            "fa-users",
            SeedIds.MariaSilva,
            DateTimeOffset.UtcNow.AddDays(-45),
            [SeedIds.MariaSilva, SeedIds.JuliaSantos, SeedIds.RicardoSouza, SeedIds.CarlosMendes, SeedIds.JulioSchwartzman]),
        new(
            SeedIds.GroupAgileCoaches,
            "Agile Coaches",
            "Cerimônias, frameworks ágeis, facilitação e troca entre Scrum Masters.",
            GroupType.Interesse,
            GroupAccessMode.RequiresApproval,
            "fa-heart",
            SeedIds.CarlosMendes,
            DateTimeOffset.UtcNow.AddDays(-100),
            [SeedIds.CarlosMendes, SeedIds.RicardoSouza, SeedIds.MariaSilva]),
        new(
            SeedIds.GroupExpansaoLatam,
            "Expansão LATAM",
            "Operação internacional, localização e alinhamentos da expansão regional.",
            GroupType.Projeto,
            GroupAccessMode.RequiresApproval,
            "fa-diagram-project",
            SeedIds.JulioSchwartzman,
            DateTimeOffset.UtcNow.AddDays(-12),
            [SeedIds.JulioSchwartzman, SeedIds.CarlosMendes, SeedIds.MariaSilva]),
        new(
            SeedIds.GroupWellbeingSaude,
            "Wellbeing & Saúde",
            "Saúde mental, ergonomia, pausas ativas e iniciativas de bem-estar.",
            GroupType.Interesse,
            GroupAccessMode.Open,
            "fa-heart",
            SeedIds.JuliaSantos,
            DateTimeOffset.UtcNow.AddDays(-4),
            [SeedIds.JuliaSantos, SeedIds.MariaSilva, SeedIds.RicardoSouza, SeedIds.CarlosMendes]),
        new(
            SeedIds.GroupPrivacidadeLgpd,
            "Privacidade e LGPD",
            "Governança de dados, políticas de privacidade e conformidade regulatória.",
            GroupType.Departamental,
            GroupAccessMode.RequiresApproval,
            "fa-building",
            SeedIds.RicardoSouza,
            DateTimeOffset.UtcNow.AddDays(-80),
            [SeedIds.RicardoSouza, SeedIds.CarlosMendes]),
        new(
            SeedIds.GroupInovacaoAberta,
            "Inovação Aberta",
            "Parcerias externas, POCs, labs de inovação e desafios abertos.",
            GroupType.Projeto,
            GroupAccessMode.Open,
            "fa-diagram-project",
            SeedIds.CarlosMendes,
            DateTimeOffset.UtcNow.AddDays(-55),
            [SeedIds.CarlosMendes, SeedIds.MariaSilva, SeedIds.JuliaSantos, SeedIds.RicardoSouza]),
    ];

    internal static Group ToGroupEntity(Entry entry, DateTimeOffset reviewedAt)
        => new()
        {
            Id = entry.Id,
            Name = entry.Name,
            Description = entry.Description,
            Type = entry.Type,
            AccessMode = GroupAccessMode.Open,
            Icon = entry.Icon,
            Status = GroupStatus.Active,
            IsPrivate = false,
            OwnerId = entry.OwnerId,
            SubmittedAt = entry.CreatedAt,
            ReviewedById = SeedIds.MariaSilva,
            ReviewedAt = reviewedAt,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = reviewedAt,
        };

    internal static IEnumerable<GroupMember> ToMembers(Entry entry)
    {
        var joinedAt = entry.CreatedAt;
        foreach (var personId in entry.MemberPersonIds.Distinct())
        {
            yield return new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = entry.Id,
                PersonId = personId,
                Role = personId == entry.OwnerId ? GroupMemberRole.Owner : GroupMemberRole.Member,
                JoinedAt = joinedAt,
                CreatedAt = joinedAt,
                UpdatedAt = joinedAt,
            };
        }
    }
}
