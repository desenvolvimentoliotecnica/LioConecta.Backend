namespace LioConecta.Application.Common.Observability;

public sealed record AccessAuditRoutePattern(string Method, string Pattern, string EventName);

public static class AccessAuditRouteMatcher
{
    public static bool Matches(string httpMethod, string path, AccessAuditRoutePattern pattern)
    {
        if (!string.Equals(httpMethod, pattern.Method, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return MatchesGlob(path, pattern.Pattern);
    }

    internal static bool MatchesGlob(string path, string pattern)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var normalizedPath = path.TrimEnd('/');
        var normalizedPattern = pattern.TrimEnd('/');

        if (normalizedPattern.EndsWith("/**", StringComparison.Ordinal))
        {
            var prefix = normalizedPattern[..^3];
            return normalizedPath.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedPattern.Contains('*'))
        {
            return false;
        }

        return normalizedPath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
    }
}
