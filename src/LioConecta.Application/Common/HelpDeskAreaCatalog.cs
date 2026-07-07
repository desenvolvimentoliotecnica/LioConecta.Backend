using System.Text.Json;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Common;

public sealed record HelpDeskAreaDefinition(
    string Id,
    string Name,
    string Icon,
    int EntityId,
    IReadOnlyList<int> CategoryRootIds,
    int? ServiceCount);

public static class HelpDeskAreaCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public const string DefaultAreasJson =
        """
        [
          {"id":"ti","name":"Área TI","icon":"laptop","entityId":1,"categoryRootIds":[],"serviceCount":21},
          {"id":"custo","name":"Área CUSTO","icon":"money","entityId":1,"categoryRootIds":[2001],"serviceCount":1},
          {"id":"pricing","name":"Área PRINCING","icon":"clipboard","entityId":1,"categoryRootIds":[2002,2003],"serviceCount":6},
          {"id":"financeira","name":"Área Financeira","icon":"money","entityId":1,"categoryRootIds":[2004,2005],"serviceCount":2}
        ]
        """;

    public static IReadOnlyList<HelpDeskAreaDefinition> Parse(string? json)
    {
        var payload = string.IsNullOrWhiteSpace(json) ? DefaultAreasJson : json.Trim();
        try
        {
            var rows = JsonSerializer.Deserialize<List<HelpDeskAreaDefinitionJson>>(payload, JsonOptions) ?? [];
            return rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Id) && !string.IsNullOrWhiteSpace(row.Name))
                .Select(row => new HelpDeskAreaDefinition(
                    row.Id.Trim(),
                    row.Name.Trim(),
                    string.IsNullOrWhiteSpace(row.Icon) ? "folder" : row.Icon.Trim(),
                    row.EntityId <= 0 ? 1 : row.EntityId,
                    row.CategoryRootIds?.Where(id => id > 0).Distinct().ToList() ?? [],
                    row.ServiceCount is > 0 ? row.ServiceCount : null))
                .ToList();
        }
        catch (JsonException)
        {
            return Parse(DefaultAreasJson);
        }
    }

    public static IReadOnlyList<GlpiItilCategory> ResolveAreaCategories(
        HelpDeskAreaDefinition area,
        IReadOnlyList<GlpiItilCategory> allCategories)
    {
        if (allCategories.Count == 0)
        {
            return [];
        }

        var scoped = allCategories
            .Where(category => category.EntityId == area.EntityId || category.EntityId == 0)
            .ToList();

        if (scoped.Count == 0)
        {
            scoped = allCategories.ToList();
        }

        if (area.CategoryRootIds.Count == 0)
        {
            return area.Id.Equals("ti", StringComparison.OrdinalIgnoreCase)
                ? scoped
                : [];
        }

        var allowedIds = new HashSet<int>();
        foreach (var rootId in area.CategoryRootIds)
        {
            CollectDescendants(rootId, scoped, allowedIds);
        }

        return scoped.Where(category => allowedIds.Contains(category.Id)).ToList();
    }

    public static int CountSelectableServices(
        HelpDeskAreaDefinition area,
        IReadOnlyList<GlpiItilCategory> areaCategories)
    {
        if (area.ServiceCount is > 0)
        {
            return area.ServiceCount.Value;
        }

        if (areaCategories.Count == 0)
        {
            return 0;
        }

        if (area.CategoryRootIds.Count > 0)
        {
            return CountLeavesUnderRoots(area.CategoryRootIds, areaCategories);
        }

        return CountLeaves(areaCategories);
    }

    private static int CountLeavesUnderRoots(
        IReadOnlyList<int> rootIds,
        IReadOnlyList<GlpiItilCategory> categories)
    {
        var childLookup = categories
            .Where(category => category.ParentId is > 0)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        var total = 0;
        foreach (var rootId in rootIds)
        {
            total += CountLeavesFromRoot(rootId, childLookup);
        }

        return total;
    }

    private static int CountLeavesFromRoot(int categoryId, IReadOnlyDictionary<int, int> childLookup) =>
        childLookup.ContainsKey(categoryId) ? childLookup[categoryId] : 1;

    private static int CountLeaves(IReadOnlyList<GlpiItilCategory> categories)
    {
        var childLookup = categories
            .Where(category => category.ParentId is > 0)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        return categories.Count(category => !childLookup.ContainsKey(category.Id));
    }

    private static void CollectDescendants(
        int rootId,
        IReadOnlyList<GlpiItilCategory> categories,
        ISet<int> allowedIds)
    {
        if (!allowedIds.Add(rootId))
        {
            return;
        }

        foreach (var child in categories.Where(category => category.ParentId == rootId))
        {
            CollectDescendants(child.Id, categories, allowedIds);
        }
    }

    private sealed class HelpDeskAreaDefinitionJson
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public int EntityId { get; set; } = 1;

        public List<int>? CategoryRootIds { get; set; }

        public int? ServiceCount { get; set; }
    }
}
