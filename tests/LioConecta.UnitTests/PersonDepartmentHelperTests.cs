using LioConecta.Application.Common;
using LioConecta.Domain.Entities;

namespace LioConecta.UnitTests;

public class PersonDepartmentHelperTests
{
    [Fact]
    public void GetName_PrefersGraphDeptOverSeedDepartmentFk()
    {
        var person = new Person
        {
            Dept = "Sistemas",
            Department = new Department { Name = "Produto" },
            DepartmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"),
        };

        Assert.Equal("Sistemas", PersonDepartmentHelper.GetName(person));
    }

    [Fact]
    public void GetName_FallsBackToDepartmentWhenDeptMissing()
    {
        var person = new Person
        {
            Department = new Department { Name = "Executiva" },
        };

        Assert.Equal("Executiva", PersonDepartmentHelper.GetName(person));
    }
}
