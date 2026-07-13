using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Common;

/// <summary>
/// Mapeamento de entidades GLPI → áreas do wizard Help Desk (sem catálogo JSON).
/// </summary>
public static class HelpDeskGlpiAreaMapper
{
    public static string ResolveEntityIcon(string? name)
    {
        var normalized = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Contains("ti") || normalized.Contains("tecnolog") || normalized.Contains("infra"))
        {
            return "laptop";
        }

        if (normalized.Contains("financ") || normalized.Contains("custo") || normalized.Contains("contab"))
        {
            return "money";
        }

        if (normalized.Contains("pricing") || normalized.Contains("preço") || normalized.Contains("preco") ||
            normalized.Contains("comercial"))
        {
            return "clipboard";
        }

        return "folder";
    }

    public static int CountSelectableLeaves(IReadOnlyList<GlpiItilCategory> categories)
    {
        if (categories.Count == 0)
        {
            return 0;
        }

        var parentsWithChildren = categories
            .Where(category => category.ParentId is > 0)
            .Select(category => category.ParentId!.Value)
            .ToHashSet();

        return categories.Count(category => !parentsWithChildren.Contains(category.Id));
    }
}
