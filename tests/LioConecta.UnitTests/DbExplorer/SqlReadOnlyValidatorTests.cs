using LioConecta.Application.Common.DbExplorer;

namespace LioConecta.UnitTests.DbExplorer;

public sealed class SqlReadOnlyValidatorTests
{
    [Theory]
    [InlineData("SELECT * FROM people")]
    [InlineData("SELECT * FROM people;")]
    [InlineData("WITH cte AS (SELECT 1 AS n) SELECT * FROM cte")]
    [InlineData("EXPLAIN SELECT id FROM people")]
    public void Validate_allows_select(string sql)
    {
        SqlReadOnlyValidator.Validate(sql, DbExplorerDialect.PostgreSql);
    }

    [Theory]
    [InlineData("INSERT INTO people VALUES (1)")]
    [InlineData("UPDATE people SET name = 'x'")]
    [InlineData("DELETE FROM people")]
    [InlineData("DROP TABLE people")]
    [InlineData("SELECT 1; SELECT 2")]
    public void Validate_rejects_unsafe_sql(string sql)
    {
        Assert.Throws<SqlReadOnlyValidationException>(() =>
            SqlReadOnlyValidator.Validate(sql, DbExplorerDialect.PostgreSql));
    }

    [Theory]
    [InlineData("SELECT * FROM people;", "SELECT * FROM people")]
    [InlineData("  SELECT 1 ;  ", "SELECT 1")]
    public void NormalizeForExecution_strips_trailing_semicolon(string input, string expected)
    {
        Assert.Equal(expected, SqlReadOnlyValidator.NormalizeForExecution(input));
    }
}
