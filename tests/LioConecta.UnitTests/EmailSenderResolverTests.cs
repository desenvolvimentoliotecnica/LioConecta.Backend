using System.Text.Json;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.UnitTests;

public class EmailSenderResolverTests
{
    [Fact]
    public void ResolveFromMetadata_ReturnsSenderWhenPresent()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            senderEmail = "leonardo.mendes@liotecnica.com.br",
            senderName = "Leonardo Sabino Mendes",
        });

        var result = EmailSenderResolver.ResolveFromMetadata(metadata);

        Assert.NotNull(result);
        Assert.Equal("leonardo.mendes@liotecnica.com.br", result.Address);
        Assert.Equal("Leonardo Sabino Mendes", result.Name);
    }

    [Fact]
    public void ResolveFromMetadata_UsesAddressAsNameWhenNameMissing()
    {
        var metadata = JsonSerializer.Serialize(new { senderEmail = "user@liotecnica.com.br" });

        var result = EmailSenderResolver.ResolveFromMetadata(metadata);

        Assert.NotNull(result);
        Assert.Equal("user@liotecnica.com.br", result.Address);
        Assert.Equal("user@liotecnica.com.br", result.Name);
    }

    [Fact]
    public void ResolveFromPerson_ReturnsActivePersonEmail()
    {
        var person = new Person
        {
            Email = "carlos.mendes@liotecnica.com.br",
            Name = "Carlos Mendes",
        };

        var result = EmailSenderResolver.ResolveFromPerson(person);

        Assert.NotNull(result);
        Assert.Equal("carlos.mendes@liotecnica.com.br", result.Address);
        Assert.Equal("Carlos Mendes", result.Name);
    }

    [Fact]
    public void Resolve_PrefersMetadataOverPerson()
    {
        var metadata = JsonSerializer.Serialize(new
        {
            senderEmail = "from.metadata@liotecnica.com.br",
            senderName = "Metadata Sender",
        });

        var person = new Person
        {
            Email = "from.person@liotecnica.com.br",
            Name = "Person Sender",
        };

        var result = EmailSenderResolver.Resolve(metadata, person);

        Assert.NotNull(result);
        Assert.Equal("from.metadata@liotecnica.com.br", result.Address);
        Assert.Equal("Metadata Sender", result.Name);
    }
}
