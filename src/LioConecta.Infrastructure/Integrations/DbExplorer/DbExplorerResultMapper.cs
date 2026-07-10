using System.Globalization;
using System.Text;
using LioConecta.Application.DTOs.DbExplorer;

namespace LioConecta.Infrastructure.Integrations.DbExplorer;

internal static class DbExplorerResultMapper
{
    public static IReadOnlyList<object?> ReadRow(System.Data.IDataRecord reader, int fieldCount)
    {
        var row = new object?[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            var value = reader.GetValue(i);
            row[i] = value is DBNull ? null : value;
        }

        return row;
    }

    public static string ToCsv(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(FormatCell(v)))));
        }

        return sb.ToString();
    }

    private static string FormatCell(object? value) =>
        value switch
        {
            null => string.Empty,
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
