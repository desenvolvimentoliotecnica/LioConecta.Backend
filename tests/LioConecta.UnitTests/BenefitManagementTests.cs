using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Services;
using Xunit;

namespace LioConecta.UnitTests;

public class BenefitManageAuthorizationTests
{
    [Fact]
    public void Categories_ContainsExpectedValues()
    {
        Assert.Contains("saude", BenefitManageAuthorization.Categories);
        Assert.Contains("familia", BenefitManageAuthorization.Categories);
        Assert.Equal(5, BenefitManageAuthorization.Categories.Count);
    }

    [Fact]
    public void Statuses_ContainsExpectedValues()
    {
        Assert.Contains("obrigatorio", BenefitManageAuthorization.Statuses);
        Assert.Contains("opcional", BenefitManageAuthorization.Statuses);
        Assert.Contains("flexivel", BenefitManageAuthorization.Statuses);
    }
}

public class BenefitDetailsJsonHelperTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsDetails()
    {
        var lines = new List<BenefitDetailLineDto> { new("Mensalidade", 100m, "Nota") };
        var dependents = new List<BenefitDependentDto> { new("Ana", "Filha", 50m) };
        var notes = new List<string> { "Observação" };

        var json = BenefitDetailsJsonHelper.Serialize(lines, dependents, notes);
        var parsed = BenefitDetailsJsonHelper.Deserialize(json);

        Assert.Single(parsed.Lines);
        Assert.Equal("Mensalidade", parsed.Lines[0].Label);
        Assert.Single(parsed.Dependents);
        Assert.Equal("Ana", parsed.Dependents[0].Name);
        Assert.Single(parsed.Notes);
    }
}
