using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LioConecta.Application.Common;

public static partial class PersonSlugHelper
{
    public static string FromEmailOrUpn(string? mail, string? userPrincipalName)
    {
        var source = FirstNonEmpty(mail, userPrincipalName);
        if (string.IsNullOrWhiteSpace(source))
        {
            return Guid.NewGuid().ToString("N")[..12];
        }

        var atIndex = source.IndexOf('@');
        var localPart = atIndex > 0 ? source[..atIndex] : source;
        return Slugify(localPart);
    }

    public static string DepartmentIdFromName(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return "sem-departamento";
        }

        return Slugify(departmentName);
    }

    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "sem-nome";
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(ch);
        }

        var text = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        text = NonSlugChars().Replace(text, "-");
        text = MultiDash().Replace(text, "-").Trim('-');
        return string.IsNullOrWhiteSpace(text) ? "sem-nome" : text;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonSlugChars();

    [GeneratedRegex(@"-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultiDash();
}
