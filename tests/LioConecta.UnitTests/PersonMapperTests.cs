using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.UnitTests;

public class PersonMapperTests
{
    [Fact]
    public void ToSummary_MapsCoreFields()
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Slug = "maria-silva",
            Name = "Maria Silva",
            Title = "Gerente de Projetos",
            Dept = "Produto",
            Email = "maria.silva@liotecnica.com.br",
            PhotoUrl = "/avatar-maria-silva.png",
            IsActive = true
        };

        var dto = PersonMapper.ToSummary(person);

        Assert.Equal("maria-silva", dto.Slug);
        Assert.Equal("Maria Silva", dto.Name);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void ToProfile_HidesPersonalDataForColleagueViewer()
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Slug = "carlos-mendes",
            Name = "Carlos Mendes",
            Email = "carlos.mendes@liotecnica.com.br",
            Phone = "(19) 99999-0000",
            BirthDate = new DateOnly(1985, 1, 1)
        };

        var dto = PersonMapper.ToProfile(person, ViewerContext.Colleague);

        Assert.Null(dto.Phone);
        Assert.Null(dto.BirthDate);
        Assert.Contains("***", dto.Email);
    }
}
