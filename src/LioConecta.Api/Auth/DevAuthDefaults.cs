namespace LioConecta.Api.Auth;

public static class DevAuthDefaults
{
    public const string SchemeName = "DevAuth";

    /// <summary>Azure AD object id claim for Maria Silva (matches SeedIds.MariaSilva).</summary>
    public static readonly Guid MariaSilvaObjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb103");

    public const string MariaSilvaSlug = "maria-silva";

    public const string MariaSilvaEmail = "maria.silva@liotecnica.com.br";

    public const string MariaSilvaName = "Maria Silva";
}
