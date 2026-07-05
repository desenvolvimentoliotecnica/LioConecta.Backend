using System.Net.Mail;
using System.Text.RegularExpressions;

namespace LioConecta.Application.Services;

public static partial class EmailAddressValidator
{
    public const string AllowedDomain = "liotecnica.com.br";

    public static IReadOnlyList<string> ParseAndValidate(
        IReadOnlyList<string>? addresses,
        bool requireAllowedDomain = true)
    {
        if (addresses is null || addresses.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        foreach (var raw in addresses)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            foreach (var part in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var email = part.Trim().ToLowerInvariant();
                if (!IsValidEmail(email))
                {
                    throw new ArgumentException($"Endereco de e-mail invalido: {part.Trim()}");
                }

                if (requireAllowedDomain && !email.EndsWith($"@{AllowedDomain}", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Somente e-mails @{AllowedDomain} sao permitidos: {email}");
                }

                if (!normalized.Contains(email, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(email);
                }
            }
        }

        return normalized;
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email);
            return email.Contains('@');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public static partial class EmailHtmlSanitizer
{
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitized = ScriptTagRegex().Replace(html, string.Empty);
        sanitized = EventHandlerRegex().Replace(sanitized, string.Empty);
        sanitized = JavascriptUrlRegex().Replace(sanitized, string.Empty);
        return sanitized.Trim();
    }

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = HtmlTagRegex().Replace(html, " ");
        text = WebWhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex("<script\\b[^<]*(?:(?!<\\/script>)<[^<]*)*<\\/script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex("\\s(on\\w+)\\s*=\\s*(\"[^\"]*\"|'[^']*'|[^\\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex("javascript:", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptUrlRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WebWhitespaceRegex();
}
