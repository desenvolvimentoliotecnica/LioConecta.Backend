using LioConecta.Application.Common;
using LioConecta.Application.DTOs;

namespace LioConecta.UnitTests;

public class HelpDeskTicketCreateValidatorTests
{
    private static CreateHelpDeskTicketRequestDto ValidRequest() =>
        new("VPN instável", "media", 2, 3, "Descrição detalhada do problema com mais de dez caracteres.");

    [Fact]
    public void Validate_AcceptsValidRequest()
    {
        HelpDeskTicketCreateValidator.Validate(ValidRequest());
    }

    [Fact]
    public void Validate_RejectsEmptySubject()
    {
        var request = ValidRequest() with { Subject = "   " };
        var exception = Assert.Throws<ArgumentException>(() => HelpDeskTicketCreateValidator.Validate(request));
        Assert.Contains("Assunto", exception.Message);
    }

    [Fact]
    public void Validate_RejectsShortDescription()
    {
        var request = ValidRequest() with { Description = "curta" };
        var exception = Assert.Throws<ArgumentException>(() => HelpDeskTicketCreateValidator.Validate(request));
        Assert.Contains("Descrição", exception.Message);
    }

    [Fact]
    public void Validate_RejectsInvalidEntityId()
    {
        var request = ValidRequest() with { EntityId = 0 };
        var exception = Assert.Throws<ArgumentException>(() => HelpDeskTicketCreateValidator.Validate(request));
        Assert.Contains("Entidade", exception.Message);
    }

    [Fact]
    public void Validate_RejectsInvalidCategoryId()
    {
        var request = ValidRequest() with { CategoryId = 0 };
        var exception = Assert.Throws<ArgumentException>(() => HelpDeskTicketCreateValidator.Validate(request));
        Assert.Contains("Categoria", exception.Message);
    }

    [Theory]
    [InlineData("baixa")]
    [InlineData("media")]
    [InlineData("alta")]
    [InlineData("critica")]
    public void Validate_AcceptsKnownPriorities(string priority)
    {
        HelpDeskTicketCreateValidator.Validate(ValidRequest() with { Priority = priority });
    }

    [Fact]
    public void Validate_RejectsUnknownPriority()
    {
        var request = ValidRequest() with { Priority = "desconhecida" };
        Assert.Throws<ArgumentException>(() => HelpDeskTicketCreateValidator.Validate(request));
    }
}
