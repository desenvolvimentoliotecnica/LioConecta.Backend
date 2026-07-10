using System.Text.RegularExpressions;

namespace LioConecta.Application.Common.DbExplorer;

public enum DbExplorerDialect
{
    PostgreSql,
    SqlServer,
}

public sealed class SqlReadOnlyValidationException(string message) : Exception(message);

public sealed class DbExplorerQueryException(string message) : Exception(message);

public static class SqlReadOnlyValidator
{
    private static readonly string[] BlockedKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "MERGE",
        "GRANT", "REVOKE", "EXEC", "EXECUTE", "CALL", "COPY", "INTO", "VACUUM",
        "REINDEX", "CLUSTER", "REFRESH", "MATERIALIZED", "LOCK", "UNLOCK",
    ];

    private static readonly Regex MultiStatementPattern = new(@";\s*\S", RegexOptions.Compiled);

    public static void Validate(string sql, DbExplorerDialect dialect)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new SqlReadOnlyValidationException("SQL vazio.");
        }

        var trimmed = NormalizeForExecution(sql);
        if (trimmed.Length > 8192)
        {
            throw new SqlReadOnlyValidationException("SQL excede o tamanho máximo permitido.");
        }

        if (MultiStatementPattern.IsMatch(trimmed))
        {
            throw new SqlReadOnlyValidationException("Apenas um statement SQL é permitido.");
        }

        var normalized = StripComments(trimmed);
        var firstToken = GetFirstToken(normalized);
        if (firstToken is not ("SELECT" or "WITH" or "EXPLAIN"))
        {
            throw new SqlReadOnlyValidationException("Somente consultas SELECT são permitidas.");
        }

        if (firstToken == "EXPLAIN" && !Regex.IsMatch(normalized, @"^\s*EXPLAIN\s+(?:\(.*?\)\s+)?SELECT\b", RegexOptions.IgnoreCase))
        {
            throw new SqlReadOnlyValidationException("EXPLAIN só é permitido com SELECT.");
        }

        var upper = normalized.ToUpperInvariant();
        foreach (var keyword in BlockedKeywords)
        {
            if (Regex.IsMatch(upper, $@"\b{keyword}\b"))
            {
                throw new SqlReadOnlyValidationException($"Palavra-chave não permitida: {keyword}.");
            }
        }

        if (upper.Contains("FOR UPDATE", StringComparison.Ordinal))
        {
            throw new SqlReadOnlyValidationException("FOR UPDATE não é permitido.");
        }
    }

    /// <summary>Remove trailing semicolon so the SQL can be wrapped as a subquery.</summary>
    public static string NormalizeForExecution(string sql) =>
        string.IsNullOrWhiteSpace(sql) ? sql : sql.Trim().TrimEnd(';').Trim();

    public static void ValidateIdentifier(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            throw new SqlReadOnlyValidationException($"{label} inválido.");
        }
    }

    public static bool IsTableBlocked(string schema, string table, IReadOnlySet<string> blocked)
    {
        var key = $"{schema}.{table}".ToLowerInvariant();
        return blocked.Contains(key) || blocked.Contains(table.ToLowerInvariant());
    }

    private static string StripComments(string sql)
    {
        var withoutBlock = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(withoutBlock, @"--.*?$", " ", RegexOptions.Multiline).Trim();
    }

    private static string GetFirstToken(string sql)
    {
        var match = Regex.Match(sql, @"^\s*(\w+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
    }
}
