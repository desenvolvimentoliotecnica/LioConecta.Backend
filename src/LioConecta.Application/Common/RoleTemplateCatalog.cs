using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Common;

public record RoleTemplateDefinition(
    string Slug,
    string Name,
    string Description,
    BusinessArea? BusinessArea,
    bool IsSystem,
    bool IsKeyUserTemplate,
    IReadOnlyList<(string PermissionKey, DataScope Scope)> Permissions);

public static class RoleTemplateCatalog
{
    private static readonly (string PermissionKey, DataScope Scope)[] EmployeeBase =
    [
        ("portal.access", DataScope.Self),
        ("sitemap.read", DataScope.Self),
        ("favorites.manage", DataScope.Self),
        ("feed.read", DataScope.Self),
        ("feed.interact", DataScope.Self),
        ("polls.read", DataScope.Self),
        ("documents.read", DataScope.Global),
        ("groups.read", DataScope.Self),
        ("groups.create", DataScope.Self),
        ("calendar.read", DataScope.Self),
        ("people.read", DataScope.Global),
        ("people.profile.read", DataScope.Self),
        ("org_chart.view", DataScope.Global),
        ("ramais.read", DataScope.Global),
        ("benefits.read", DataScope.Self),
        ("payslips.read", DataScope.Self),
        ("leave.read", DataScope.Self),
        ("leave.request", DataScope.Self),
        ("transport.read", DataScope.Self),
        ("reimbursement.read", DataScope.Self),
        ("travel_advance.read", DataScope.Self),
        ("ponto.read", DataScope.Self),
        ("ponto.request", DataScope.Self),
        ("systems.read", DataScope.Global),
        ("activities.read", DataScope.Self),
        ("helpdesk.read", DataScope.Self),
        ("facilities.request", DataScope.Self),
        ("legal.request", DataScope.Self),
        ("unilio.access", DataScope.Self),
        ("unilio.learn.read", DataScope.Self),
        ("unilio.learn.assess", DataScope.Self),
        ("unilio.learn.certificates", DataScope.Self),
        ("unilio.compliance.read", DataScope.Self),
        ("unilio.community.read", DataScope.Self),
        ("unilio.events.read", DataScope.Self),
        ("unilio.events.register", DataScope.Self),
        ("unilio.skills.read", DataScope.Self),
        ("unilio.recommendations.read", DataScope.Self),
        ("comunicados.read", DataScope.Global),
        ("wiki.read", DataScope.Global),
        ("feedback.submit", DataScope.Self),
    ];

    public static IReadOnlyList<RoleTemplateDefinition> All() =>
    [
        Role("Employee", "Colaborador", "Acesso base do portal", null, true, false, EmployeeBase),
        Role("Manager", "Gestor", "Gestor de equipe", BusinessArea.RH, true, false,
            EmployeeBase.Concat([
                ("leave.approve", DataScope.Team),
                ("ponto.approve", DataScope.Team),
                ("payslips.read", DataScope.Team),
            ]).ToArray()),
        Role("HR", "Recursos Humanos", "Operações de RH", BusinessArea.RH, true, false,
            EmployeeBase.Concat([
                ("benefits.manage", DataScope.Global),
                ("leave.manage", DataScope.Global),
                ("leave.approve", DataScope.Global),
                ("leave.notify", DataScope.Global),
                ("rh_requests.manage", DataScope.Global),
                ("transport.read", DataScope.Self),
                ("transport.manage", DataScope.Global),
                ("ponto.manage", DataScope.Global),
                ("ponto.approve", DataScope.Global),
                ("ponto.admin", DataScope.Global),
                ("people.salary.read", DataScope.Global),
                ("payslips.audit", DataScope.Global),
                ("mood.analytics", DataScope.Global),
                ("feedback.triage", DataScope.Global),
            ]).ToArray()),
        Role("TI", "Tecnologia da Informação", "Operações de TI", BusinessArea.TI, true, false,
            EmployeeBase.Concat([
                ("helpdesk.manage", DataScope.Global),
                ("wiki.manage", DataScope.Global),
                ("systems.manage", DataScope.Global),
                ("equipment.manage", DataScope.Global),
                ("vpn.manage", DataScope.Global),
                ("ramais.manage", DataScope.Global),
            ]).ToArray()),
        Role("Facilities", "Facilities", "Operações de facilities", BusinessArea.Facilities, true, false,
            EmployeeBase.Concat(PermissionCatalog.All()
                .Where(p => p.Key.StartsWith("facilities.", StringComparison.Ordinal) && p.Key != "facilities.request")
                .Select(p => (PermissionKey: p.Key, Scope: DataScope.Global))).ToArray()),
        Role("Legal", "Jurídico", "Compliance e jurídico", BusinessArea.Juridico, true, false,
            EmployeeBase.Concat(PermissionCatalog.All()
                .Where(p => p.Key.StartsWith("legal.", StringComparison.Ordinal) && p.Key != "legal.request")
                .Select(p => (PermissionKey: p.Key, Scope: DataScope.Global)).ToArray())
                .Concat([("documents.manage", DataScope.Global)]).ToArray()),
        Role("Admin", "Administrador", "Administração da plataforma", BusinessArea.Plataforma, true, false,
            EmployeeBase.Concat(PermissionCatalog.All()
                .Where(p => p.Key.StartsWith("admin.", StringComparison.Ordinal)
                    || p.Key.StartsWith("rbac.", StringComparison.Ordinal)
                    || p.Key.StartsWith("portal.integration.", StringComparison.Ordinal)
                    || p.Key is "groups.approve" or "org_chart.govern" or "org_chart.edit")
                .Select(p => (PermissionKey: p.Key, Scope: DataScope.Global)).ToArray())
                .Concat([
                    ("loop.access", DataScope.Global), ("loop.manage", DataScope.Global), ("loop.approve", DataScope.Global),
                    ("pulse.access", DataScope.Global), ("pulse.manage", DataScope.Global),
                    ("compass.access", DataScope.Global), ("compass.read", DataScope.Global), ("compass.manage", DataScope.Global),
                    ("compass.financial.read", DataScope.Global), ("compass.financial.reconcile", DataScope.Global),
                    ("compass.export", DataScope.Global),
                    ("feed.manage", DataScope.Global), ("news.manage", DataScope.Global),
                    ("comunicados.publish.official", DataScope.Global), ("comunicados.publish.departmental", DataScope.Global),
                    ("comunicados.publish.urgent", DataScope.Global), ("comunicados.manage", DataScope.Global),
                    ("wiki.manage", DataScope.Global),
                    ("mood.analytics", DataScope.Global), ("feedback.triage", DataScope.Global),
                    ("payslips.audit", DataScope.Global),
                ]).Distinct().ToArray()),
        Role("AnalyticsViewer", "Analytics", "Observabilidade e auditoria", BusinessArea.Analytics, true, false,
            EmployeeBase.Concat([
                ("analytics.view", DataScope.Global),
                ("analytics.export", DataScope.Global),
                ("audit.view", DataScope.Global),
                ("audit.export", DataScope.Global),
            ]).ToArray()),
        Role("KioskReader", "Quiosque", "Modo totem", BusinessArea.Quiosque, true, false,
            [("kiosk.read", DataScope.Global)]),
        KeyUser("KeyUser-RH", "Key User RH", BusinessArea.RH,
            ("benefits.manage", DataScope.Global), ("leave.manage", DataScope.Global), ("leave.approve", DataScope.Global),
            ("rh_requests.manage", DataScope.Global), ("transport.read", DataScope.Self), ("transport.manage", DataScope.Global), ("ponto.manage", DataScope.Global),
            ("ponto.approve", DataScope.Global), ("ponto.admin", DataScope.Global), ("people.salary.read", DataScope.Global),
            ("payslips.audit", DataScope.Global), ("mood.analytics", DataScope.Global), ("feedback.triage", DataScope.Global)),
        KeyUser("KeyUser-Financeiro", "Key User Financeiro", BusinessArea.Financeiro,
            ("reimbursement.read", DataScope.Self), ("reimbursement.manage", DataScope.Global),
            ("travel_advance.read", DataScope.Self), ("travel_advance.manage", DataScope.Global)),
        KeyUser("KeyUser-Contabil", "Key User Contábil", BusinessArea.Contabil,
            ("compass.access", DataScope.Global), ("compass.read", DataScope.Global), ("compass.financial.read", DataScope.Global),
            ("compass.financial.reconcile", DataScope.Global), ("compass.manage", DataScope.Global), ("compass.export", DataScope.Global)),
        KeyUser("KeyUser-TI", "Key User TI", BusinessArea.TI,
            ("helpdesk.manage", DataScope.Global), ("wiki.manage", DataScope.Global),
            ("systems.manage", DataScope.Global), ("equipment.manage", DataScope.Global),
            ("vpn.manage", DataScope.Global), ("ramais.manage", DataScope.Global)),
        KeyUser("KeyUser-Facilities", "Key User Facilities", BusinessArea.Facilities,
            PermissionCatalog.All().Where(p => p.Key.StartsWith("facilities.", StringComparison.Ordinal))
                .Select(p => (PermissionKey: p.Key, Scope: DataScope.Global)).ToArray()),
        KeyUser("KeyUser-Juridico", "Key User Jurídico", BusinessArea.Juridico,
            PermissionCatalog.All().Where(p => p.Key.StartsWith("legal.", StringComparison.Ordinal))
                .Select(p => (PermissionKey: p.Key, Scope: DataScope.Global)).Concat([("documents.manage", DataScope.Global)]).ToArray()),
        KeyUser("KeyUser-Marketing", "Key User Marketing", BusinessArea.Marketing,
            ("feed.manage", DataScope.Global), ("polls.manage", DataScope.Global), ("celebrations.manage", DataScope.Global),
            ("news.manage", DataScope.Global), ("comunicados.read", DataScope.Global),
            ("comunicados.publish.official", DataScope.Global), ("comunicados.publish.departmental", DataScope.Department),
            ("comunicados.publish.urgent", DataScope.Global), ("comunicados.manage", DataScope.Global),
            ("kiosk.content.manage", DataScope.Global)),
        KeyUser("KeyUser-Pessoas", "Key User Pessoas", BusinessArea.Pessoas,
            ("people.read", DataScope.Global), ("org_chart.view_full", DataScope.Global), ("org_chart.edit", DataScope.Global),
            ("org_chart.govern", DataScope.Global), ("ramais.manage", DataScope.Global), ("people.profile.read", DataScope.Global)),
        KeyUser("KeyUser-Projetos", "Key User Projetos", BusinessArea.Projetos,
            ("loop.access", DataScope.Global), ("loop.manage", DataScope.Global), ("loop.approve", DataScope.Global),
            ("pulse.access", DataScope.Global), ("pulse.manage", DataScope.Global)),
        KeyUser("KeyUser-Planejamento", "Key User Planejamento", BusinessArea.Planejamento,
            ("compass.access", DataScope.Global), ("compass.read", DataScope.Global), ("compass.manage", DataScope.Global),
            ("compass.scenarios.manage", DataScope.Global), ("compass.decisions.manage", DataScope.Global)),
        KeyUser("KeyUser-Plataforma", "Key User Plataforma", BusinessArea.Plataforma,
            PermissionCatalog.All().Where(p => p.Key.StartsWith("admin.", StringComparison.Ordinal)
                || p.Key.StartsWith("rbac.", StringComparison.Ordinal) || p.Key == "groups.approve")
                .Select(p => (PermissionKey: p.Key, Scope: DataScope.Global)).ToArray()),
        KeyUser("KeyUser-Analytics", "Key User Analytics", BusinessArea.Analytics,
            ("analytics.view", DataScope.Global), ("analytics.export", DataScope.Global),
            ("audit.view", DataScope.Global), ("audit.export", DataScope.Global)),
        KeyUser("KeyUser-Quiosque", "Key User Quiosque", BusinessArea.Quiosque, ("kiosk.read", DataScope.Global)),
        KeyUser("KeyUser-UniLio-Instrutor", "Key User UniLio Instrutor", BusinessArea.UniLio,
            ("unilio.instructor.panel", DataScope.Self), ("unilio.courses.author", DataScope.Self),
            ("unilio.courses.edit.own", DataScope.Self), ("unilio.community.post", DataScope.Self)),
        KeyUser("KeyUser-UniLio-Gestor", "Key User UniLio Gestor", BusinessArea.UniLio,
            ("unilio.team.view", DataScope.Team), ("unilio.reports.view", DataScope.Team)),
        KeyUser("KeyUser-UniLio-TD", "Key User UniLio T&D", BusinessArea.UniLio,
            ("unilio.courses.approve", DataScope.Global), ("unilio.courses.publish", DataScope.Global),
            ("unilio.compliance.manage", DataScope.Global), ("unilio.reports.view", DataScope.Global),
            ("unilio.paths.manage", DataScope.Global), ("unilio.skills.manage", DataScope.Global)),
    ];

    private static RoleTemplateDefinition Role(
        string slug, string name, string description, BusinessArea? area, bool isSystem, bool isKeyUser,
        IReadOnlyList<(string PermissionKey, DataScope Scope)> permissions) =>
        new(slug, name, description, area, isSystem, isKeyUser, permissions);

    private static RoleTemplateDefinition KeyUser(
        string slug, string name, BusinessArea area, params (string PermissionKey, DataScope Scope)[] extra) =>
        Role(slug, name, $"Key user — {name}", area, false, true,
            EmployeeBase.Concat(extra).Distinct().ToArray());
}
