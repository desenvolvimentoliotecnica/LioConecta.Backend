using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.UnitTests;

public class HelpDeskGlpiAreaMapperTests
{
    [Fact]
    public void CountSelectableLeaves_IgnoresParentsWithChildren()
    {
        var categories = new List<GlpiItilCategory>
        {
            new() { Id = 1, Name = "Hardware", ParentId = null, EntityId = 1 },
            new() { Id = 2, Name = "Notebook", ParentId = 1, EntityId = 1 },
            new() { Id = 3, Name = "Desktop", ParentId = 1, EntityId = 1 },
            new() { Id = 4, Name = "Rede", ParentId = null, EntityId = 1 },
        };

        Assert.Equal(3, HelpDeskGlpiAreaMapper.CountSelectableLeaves(categories));
    }

    [Fact]
    public void CountSelectableLeaves_Empty_ReturnsZero()
    {
        Assert.Equal(0, HelpDeskGlpiAreaMapper.CountSelectableLeaves([]));
    }

    [Fact]
    public void ResolveEntityIcon_UsesHeuristicsFromName()
    {
        Assert.Equal("laptop", HelpDeskGlpiAreaMapper.ResolveEntityIcon("Área TI"));
        Assert.Equal("money", HelpDeskGlpiAreaMapper.ResolveEntityIcon("Financeiro"));
        Assert.Equal("folder", HelpDeskGlpiAreaMapper.ResolveEntityIcon("Operações"));
    }
}
