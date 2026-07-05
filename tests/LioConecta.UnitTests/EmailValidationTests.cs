using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public class EmailValidationTests
{
    [Fact]
    public void ParseAndValidate_AcceptsCorporateDomain()
    {
        var result = EmailAddressValidator.ParseAndValidate(["leonardo.mendes@liotecnica.com.br"]);
        Assert.Single(result);
        Assert.Equal("leonardo.mendes@liotecnica.com.br", result[0]);
    }

    [Fact]
    public void ParseAndValidate_RejectsExternalDomain()
    {
        Assert.Throws<ArgumentException>(() =>
            EmailAddressValidator.ParseAndValidate(["user@gmail.com"]));
    }

    [Fact]
    public void ParseAndValidate_ParsesCommaSeparatedList()
    {
        var result = EmailAddressValidator.ParseAndValidate([
            "a@liotecnica.com.br, b@liotecnica.com.br",
        ]);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        var html = "<p>Olá</p><script>alert(1)</script>";
        var sanitized = EmailHtmlSanitizer.Sanitize(html);
        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Olá", sanitized);
    }

    [Fact]
    public void ToPlainText_StripsHtml()
    {
        var text = EmailHtmlSanitizer.ToPlainText("<p><strong>Teste</strong></p>");
        Assert.Contains("Teste", text);
        Assert.DoesNotContain("<", text);
    }
}
