using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.UnitTests;

public class CareerSalaryVisibilityTests
{
    [Theory]
    [InlineData(ViewerContext.Self)]
    [InlineData(ViewerContext.HR)]
    [InlineData(ViewerContext.Admin)]
    public void CanViewSalaryValues_ReturnsTrue_ForPrivilegedContexts(ViewerContext context)
    {
        var allowed = CareerSalaryVisibility.CanViewSalaryValues(
            context,
            [UserRole.Employee],
            viewer: null,
            viewerHasDirectReports: false);

        Assert.True(allowed);
    }

    [Fact]
    public void CanViewSalaryValues_ReturnsTrue_ForManagerRole()
    {
        var allowed = CareerSalaryVisibility.CanViewSalaryValues(
            ViewerContext.Colleague,
            [UserRole.Manager],
            viewer: null,
            viewerHasDirectReports: false);

        Assert.True(allowed);
    }

    [Fact]
    public void CanViewSalaryValues_ReturnsTrue_ForDirectorTitle()
    {
        var viewer = new Person
        {
            Title = "Diretor de Produto",
            TagsJson = "[]",
        };

        var allowed = CareerSalaryVisibility.CanViewSalaryValues(
            ViewerContext.Colleague,
            [UserRole.Employee],
            viewer,
            viewerHasDirectReports: false);

        Assert.True(allowed);
    }

    [Fact]
    public void CanViewSalaryValues_ReturnsTrue_ForPeopleManagerWithoutRoleClaim()
    {
        var viewer = new Person
        {
            Title = "Coordenador de TI",
            TagsJson = "[]",
        };

        var allowed = CareerSalaryVisibility.CanViewSalaryValues(
            ViewerContext.Colleague,
            [UserRole.Employee],
            viewer,
            viewerHasDirectReports: true);

        Assert.True(allowed);
    }

    [Fact]
    public void CanViewSalaryValues_ReturnsFalse_ForRegularColleague()
    {
        var viewer = new Person
        {
            Title = "Analista de Sistemas",
            TagsJson = "[\"member\"]",
        };

        var allowed = CareerSalaryVisibility.CanViewSalaryValues(
            ViewerContext.Colleague,
            [UserRole.Employee],
            viewer,
            viewerHasDirectReports: false);

        Assert.False(allowed);
    }
}
