using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Common;

public static class HelpDeskGlpiEntityTreeBuilder
{
    public static IReadOnlyList<GlpiEntity> Build(IEnumerable<GlpiEntity> rawEntities)
    {
        var entities = rawEntities
            .GroupBy(e => e.Id)
            .Select(group => group.First())
            .ToList();

        var childCounts = entities
            .Where(e => e.ParentId is > 0)
            .GroupBy(e => e.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var entity in entities)
        {
            entity.HasChildren = childCounts.ContainsKey(entity.Id);
        }

        return entities
            .OrderBy(e => e.FullName ?? e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int? NormalizeParentId(int? parentId) =>
        parentId is null or <= 0 ? null : parentId;
}
