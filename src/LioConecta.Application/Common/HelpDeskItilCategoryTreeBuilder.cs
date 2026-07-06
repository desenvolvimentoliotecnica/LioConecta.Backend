using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Common;

public static class HelpDeskItilCategoryTreeBuilder
{
    public static IReadOnlyList<GlpiItilCategory> Build(
        IEnumerable<GlpiItilCategory> rawCategories,
        bool dedupeByLabel = true)
    {
        var items = rawCategories.ToList();
        var deduped = dedupeByLabel
            ? items
                .GroupBy(c => (c.FullName ?? c.Name).Trim().ToLowerInvariant(), StringComparer.Ordinal)
                .Select(group => group.OrderBy(c => c.Id).First())
                .ToList()
            : items
                .GroupBy(c => c.Id)
                .Select(group => group.First())
                .ToList();

        var childCounts = deduped
            .Where(c => c.ParentId is > 0)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var category in deduped)
        {
            category.HasChildren = childCounts.ContainsKey(category.Id);
        }

        return deduped
            .OrderBy(c => c.FullName ?? c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int? NormalizeParentId(int? parentId) =>
        parentId is null or <= 0 ? null : parentId;
}
