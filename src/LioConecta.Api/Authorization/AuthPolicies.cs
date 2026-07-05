namespace LioConecta.Api.Authorization;

public static class AuthPolicies
{
    public const string RequireHR = nameof(RequireHR);

    public const string RequireAdmin = nameof(RequireAdmin);

    public const string RequireTI = nameof(RequireTI);

    public const string RequireKioskReader = nameof(RequireKioskReader);
}
