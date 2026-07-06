using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.UnitTests;

public class HelpDeskItilCategoryTreeBuilderTests
{
    [Fact]
    public void Build_DeduplicatesByLabelAndComputesHasChildren()
    {
        var raw = new List<GlpiItilCategory>
        {
            new() { Id = 1, Name = "TI", FullName = "TI", ParentId = null },
            new() { Id = 2, Name = "TI", FullName = "TI", ParentId = null },
            new() { Id = 10, Name = "Web", FullName = "TI > Web", ParentId = 1 },
            new() { Id = 11, Name = "VPN", FullName = "TI > VPN", ParentId = 1 },
        };

        var result = HelpDeskItilCategoryTreeBuilder.Build(raw);

        Assert.Single(result.Where(c => (c.FullName ?? c.Name).Equals("TI", StringComparison.OrdinalIgnoreCase)));
        var root = result.Single(c => c.Id == 1);
        Assert.True(root.HasChildren);
        Assert.False(result.Single(c => c.Id == 10).HasChildren);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(5, 5)]
    public void NormalizeParentId_MapsRootValues(int? input, int? expected)
    {
        Assert.Equal(expected, HelpDeskItilCategoryTreeBuilder.NormalizeParentId(input));
    }
}
