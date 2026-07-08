using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.UnitTests;

public class PersonRmProfileMapperTests
{
    [Fact]
    public void ApplyRmProfile_UsesEffectivePortalAvatarInsteadOfRawGraphPhoto()
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Slug = "leonardo-mendes",
            Name = "Leonardo Sabino Mendes",
            Email = "leonardo.mendes@liotecnica.com.br",
            PhotoUrl = "/media/people/leonardo-mendes.jpg",
            PersonalDataJson = "{\"portalAvatarUrl\":\"/assets/avatars/animals/avatar-crab.png\"}",
        };

        var rm = new RmEmployeeProfileRecord
        {
            Chapa = "00012345",
            Nome = "Leonardo Sabino Mendes",
            FuncaoDescricao = "Desenvolvedor Sr.",
            SecaoDescricao = "Sistemas",
        };

        var profile = PersonRmProfileMapper.ApplyRmProfile(
            person,
            rm,
            new RmEmployeeCareerHistoryData(),
            ViewerContext.Self,
            includeSalaryValues: false);

        Assert.Equal("/assets/avatars/animals/avatar-crab.png", profile.PhotoUrl);
        Assert.Equal("/assets/avatars/animals/avatar-crab.png", profile.PortalPhotoUrl);
        Assert.Equal("/media/people/leonardo-mendes.jpg", profile.GraphPhotoUrl);
    }
}
