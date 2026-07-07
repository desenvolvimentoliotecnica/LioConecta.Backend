using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Mapping;

namespace LioConecta.UnitTests;

public class PersonRmCareerHistoryBuilderTests
{
    private static RmEmployeeProfileRecord CreateProfile(
        DateTime admission,
        string funcao = "Analista",
        string secao = "TI")
        => new()
        {
            Chapa = "00012345",
            DataAdmissao = admission,
            FuncaoDescricao = funcao,
            SecaoDescricao = secao,
        };

    [Fact]
    public void Build_WithOnlyAdmission_CreatesAdmissionAndCurrent()
    {
        var admission = new DateTime(2021, 3, 15);
        var profile = CreateProfile(admission, "Coord. TI", "Gestão Sistemas");

        var history = PersonRmCareerHistoryBuilder.Build(profile, new RmEmployeeCareerHistoryData());

        Assert.Equal(2, history.Count);
        Assert.Equal("atual", history[0]["type"]);
        Assert.Equal("Coord. TI", history[0]["title"]);
        Assert.Equal("admission", history[1]["type"]);
        Assert.Equal("mar de 2021", history[1]["date"]);
    }

    [Fact]
    public void Build_WithFunctionPromotions_MarksLatestAsCurrent()
    {
        var admission = new DateTime(2019, 1, 10);
        var profile = CreateProfile(admission, "Gerente de TI", "Gestão Sistemas");
        var data = new RmEmployeeCareerHistoryData
        {
            FunctionHistory =
            [
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = admission,
                    CodFuncao = "001",
                    FuncaoDescricao = "Analista de TI",
                },
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = new DateTime(2021, 6, 1),
                    CodFuncao = "002",
                    FuncaoDescricao = "Coord. TI",
                },
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = new DateTime(2024, 2, 1),
                    CodFuncao = "003",
                    FuncaoDescricao = "Gerente de TI",
                },
            ],
        };

        var history = PersonRmCareerHistoryBuilder.Build(profile, data);

        Assert.Equal(4, history.Count);
        Assert.Equal("atual", history[0]["type"]);
        Assert.Equal("Gerente de TI", history[0]["title"]);
        Assert.Equal("promotion", history[1]["type"]);
        Assert.Equal("Gerente de TI", history[1]["title"]);
        Assert.Equal("promotion", history[2]["type"]);
        Assert.Equal("Coord. TI", history[2]["title"]);
        Assert.Equal("admission", history[3]["type"]);
    }

    [Fact]
    public void Build_WithSectionTransfer_AddsTransferEvent()
    {
        var admission = new DateTime(2020, 5, 1);
        var profile = CreateProfile(admission, "Analista", "Marketing");
        var data = new RmEmployeeCareerHistoryData
        {
            FunctionHistory =
            [
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = admission,
                    CodFuncao = "010",
                    FuncaoDescricao = "Analista",
                },
            ],
            SectionHistory =
            [
                new RmEmployeeSectionHistoryRecord
                {
                    EventDate = admission,
                    CodSecao = "100",
                    SecaoDescricao = "RH",
                },
                new RmEmployeeSectionHistoryRecord
                {
                    EventDate = new DateTime(2022, 8, 15),
                    CodSecao = "200",
                    SecaoDescricao = "Marketing",
                },
            ],
        };

        var history = PersonRmCareerHistoryBuilder.Build(profile, data);

        Assert.Contains(history, item => item["type"]?.ToString() == "transfer");
        Assert.Contains(history, item => item["dept"]?.ToString() == "Marketing" && item["type"]?.ToString() == "transfer");
    }

    [Fact]
    public void Build_WithSalaryChange_AddsSalaryEvent()
    {
        var admission = new DateTime(2018, 2, 1);
        var profile = CreateProfile(admission);
        var data = new RmEmployeeCareerHistoryData
        {
            SalaryHistory =
            [
                new RmEmployeeSalaryHistoryRecord
                {
                    EventDate = new DateTime(2023, 1, 1),
                    Salario = 8500m,
                    Motivo = "Mérito",
                },
            ],
        };

        var history = PersonRmCareerHistoryBuilder.Build(profile, data, includeSalaryValues: false);

        var salaryEvent = Assert.Single(history, item => item["type"]?.ToString() == "salary");
        Assert.Contains("Mérito", salaryEvent["note"]?.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Valor:", salaryEvent["note"]?.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("R$", salaryEvent["note"]?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithSalaryChange_ShowsValuesWhenAllowed()
    {
        var admission = new DateTime(2018, 2, 1);
        var profile = CreateProfile(admission);
        var data = new RmEmployeeCareerHistoryData
        {
            SalaryHistory =
            [
                new RmEmployeeSalaryHistoryRecord
                {
                    EventDate = new DateTime(2023, 1, 1),
                    Salario = 8500m,
                    Motivo = "Mérito",
                },
            ],
        };

        var history = PersonRmCareerHistoryBuilder.Build(profile, data, includeSalaryValues: true);

        var salaryEvent = Assert.Single(history, item => item["type"]?.ToString() == "salary");
        Assert.Contains("Valor:", salaryEvent["note"]?.ToString(), StringComparison.Ordinal);
        Assert.Contains("R$", salaryEvent["note"]?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Build_DeduplicatesSameDayEvents_PrefersFunctionOverSalary()
    {
        var admission = new DateTime(2021, 1, 1);
        var changeDate = new DateTime(2023, 5, 10);
        var profile = CreateProfile(admission, "Coord. TI", "TI");
        var data = new RmEmployeeCareerHistoryData
        {
            FunctionHistory =
            [
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = admission,
                    CodFuncao = "001",
                    FuncaoDescricao = "Analista",
                },
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = changeDate,
                    CodFuncao = "002",
                    FuncaoDescricao = "Coord. TI",
                },
            ],
            SalaryHistory =
            [
                new RmEmployeeSalaryHistoryRecord
                {
                    EventDate = changeDate,
                    Salario = 10000m,
                },
            ],
        };

        var history = PersonRmCareerHistoryBuilder.Build(profile, data);

        Assert.DoesNotContain(history, item => item["type"]?.ToString() == "salary");
        Assert.Contains(history, item =>
            item["type"]?.ToString() == "promotion"
            && item["title"]?.ToString() == "Coord. TI");
    }

    [Fact]
    public void Build_OrdersEventsDescendingByDate()
    {
        var admission = new DateTime(2017, 1, 1);
        var profile = CreateProfile(admission, "Gerente", "Operações");
        var data = new RmEmployeeCareerHistoryData
        {
            FunctionHistory =
            [
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = admission,
                    CodFuncao = "001",
                    FuncaoDescricao = "Assistente",
                },
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = new DateTime(2020, 1, 1),
                    CodFuncao = "002",
                    FuncaoDescricao = "Analista",
                },
                new RmEmployeeFunctionHistoryRecord
                {
                    EventDate = new DateTime(2024, 1, 1),
                    CodFuncao = "003",
                    FuncaoDescricao = "Gerente",
                },
            ],
        };

        var history = PersonRmCareerHistoryBuilder.Build(profile, data);
        var dates = history.Select(item => item["date"]?.ToString()).ToList();

        Assert.Equal("atual", history[0]["type"]);
        Assert.True(dates[0]!.CompareTo(dates[^1]) > 0);
    }

    [Theory]
    [InlineData(2021, 3, 15, "mar de 2021")]
    [InlineData(2020, 1, 1, "jan de 2020")]
    public void FormatTimelineDate_ReturnsPortugueseMonthLabel(int year, int month, int day, string expected)
    {
        var formatted = PersonRmCareerHistoryBuilder.FormatTimelineDate(new DateTime(year, month, day));
        Assert.Equal(expected, formatted);
    }
}
