using System.Text.RegularExpressions;

namespace LioConecta.Application.Common;

public static partial class SlugHelper
{
    public static string FromTitle(string title, Guid fallbackId)
    {
        var slug = SlugPattern().Replace(title.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
        {
            slug = fallbackId.ToString("N")[..12];
        }

        return slug.Length <= 80 ? slug : slug[..80].TrimEnd('-');
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
