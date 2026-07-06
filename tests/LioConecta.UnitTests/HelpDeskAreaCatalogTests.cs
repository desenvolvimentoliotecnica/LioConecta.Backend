using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.UnitTests;

public class HelpDeskAreaCatalogTests
{
    [Fact]
    public void Parse_ReturnsDefaultMobileAreas()
    {
        var areas = HelpDeskAreaCatalog.Parse(null);
        Assert.Equal(4, areas.Count);
        Assert.Contains(areas, area => area.Name == "Área TI" && area.ServiceCount == 21);
        Assert.Contains(areas, area => area.Name == "Área CUSTO" && area.ServiceCount == 1);
    }

    [Fact]
    public void ResolveAreaCategories_FiltersByConfiguredRoots()
    {
        var all = new List<GlpiItilCategory>
        {
            new() { Id = 1, Name = "TI", ParentId = null, EntityId = 1 },
            new() { Id = 2, Name = "Hardware", ParentId = null, EntityId = 1 },
            new() { Id = 10, Name = "Notebooks", ParentId = 2, EntityId = 1 },
        };

        var area = new HelpDeskAreaDefinition("hardware", "Hardware", "folder", 1, [2], null);
        var scoped = HelpDeskAreaCatalog.ResolveAreaCategories(area, all);

        Assert.Equal(2, scoped.Count);
        Assert.Contains(scoped, category => category.Id == 2);
        Assert.Contains(scoped, category => category.Id == 10);
    }
}
