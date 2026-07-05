namespace LioConecta.Api.Auth;

public static class DevAuthDefaults
{
    public const string SchemeName = "DevAuth";

    /// <summary>Azure AD object id claim for the default dev user (matches SeedIds.MariaSilva).</summary>
    public static readonly Guid DevUserObjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb103");

    public const string DevUserSlug = "leonardo-sabino-mendes";

    public const string DevUserEmail = "leonardo.mendes@liotecnica.com.br";

    public const string DevUserName = "Leonardo Sabino Mendes";
}
