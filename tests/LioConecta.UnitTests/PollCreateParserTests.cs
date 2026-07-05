using LioConecta.Application.Common;

namespace LioConecta.UnitTests;

public class PollCreateParserTests
{
    [Fact]
    public void Parse_ValidPoll_ReturnsNormalizedOptions()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["options"] = new[] { "Opção A", "Opção B", "Opção C" },
            ["heroImageUrl"] = "/bg-poll.png",
        };

        var parsed = PollCreateParser.Parse("Qual tema para a próxima palestra?", metadata);

        Assert.Equal("Qual tema para a próxima palestra?", parsed.Question);
        Assert.Equal(3, parsed.Options.Count);
        Assert.Equal("/bg-poll.png", parsed.HeroImageUrl);
    }

    [Fact]
    public void Parse_TooFewOptions_Throws()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["options"] = new[] { "Única opção" },
        };

        Assert.Throws<ArgumentException>(() =>
            PollCreateParser.Parse("Pergunta válida aqui?", metadata));
    }

    [Fact]
    public void Parse_DuplicateOptions_Throws()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["options"] = new[] { "Igual", "Igual" },
        };

        Assert.Throws<ArgumentException>(() =>
            PollCreateParser.Parse("Pergunta válida aqui?", metadata));
    }

    [Fact]
    public void Parse_ShortQuestion_Throws()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["options"] = new[] { "A", "B" },
        };

        Assert.Throws<ArgumentException>(() =>
            PollCreateParser.Parse("Sim", metadata));
    }
}
