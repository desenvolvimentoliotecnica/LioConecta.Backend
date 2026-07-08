using LioConecta.Application.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.UnitTests;

public class PersonPhotoResolverTests
{
    [Fact]
    public void ResolveEffectivePhotoUrl_PrefersPortalAvatarOverGraphPhoto()
    {
        var person = new Person
        {
            PhotoUrl = "/media/people/maria-silva.jpg",
            PersonalDataJson = "{\"portalAvatarUrl\":\"/assets/avatars/animals/avatar-cat.png\"}",
        };

        Assert.Equal("/assets/avatars/animals/avatar-cat.png", PersonPhotoResolver.ResolveEffectivePhotoUrl(person));
        Assert.Equal("/media/people/maria-silva.jpg", PersonPhotoResolver.GetGraphPhotoUrl(person));
        Assert.Equal("/assets/avatars/animals/avatar-cat.png", PersonPhotoResolver.GetPortalAvatarUrl(person));
    }

    [Fact]
    public void ResolveEffectivePhotoUrl_UsesGraphWhenPortalMissing()
    {
        var person = new Person
        {
            PhotoUrl = "/media/people/joao-pereira.jpg",
        };

        Assert.Equal("/media/people/joao-pereira.jpg", PersonPhotoResolver.ResolveEffectivePhotoUrl(person));
    }

    [Fact]
    public void ClearPortalAvatar_RemovesOverride()
    {
        var person = new Person
        {
            PhotoUrl = "/media/people/joao-pereira.jpg",
            PersonalDataJson = "{\"portalAvatarUrl\":\"/assets/avatars/animals/avatar-dog.png\"}",
        };

        PersonPhotoResolver.ClearPortalAvatar(person);

        Assert.Null(PersonPhotoResolver.GetPortalAvatarUrl(person));
        Assert.Equal("/media/people/joao-pereira.jpg", PersonPhotoResolver.ResolveEffectivePhotoUrl(person));
    }

    [Fact]
    public void NormalizeAndValidatePortalAvatar_RejectsInvalidPath()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PersonPhotoResolver.NormalizeAndValidatePortalAvatar("/avatar-cat.png"));
    }
}
