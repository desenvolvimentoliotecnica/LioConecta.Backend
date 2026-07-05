namespace LioConecta.Infrastructure.Seed;

/// <summary>
/// Valores padrão de desenvolvimento para SMTP Office 365 (Liotecnica).
/// Senha nunca em texto claro no repositório — aplicada via seed/API com criptografia AES.
/// </summary>
public static class EmailConfigurationDefaults
{
    public const string DevSmtpHost = "smtp.office365.com";
    public const int DevSmtpPort = 587;
    public const string DevSmtpUsername = "leonardo.mendes@liotecnica.com.br";
    public const string DevFromAddress = "leonardo.mendes@liotecnica.com.br";
    public const string DevFromName = "LioConecta";
    public const bool DevUseStartTls = true;
    public const bool DevIsEnabled = true;
}
