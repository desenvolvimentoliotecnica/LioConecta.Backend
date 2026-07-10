using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Common;

/// <summary>
/// Modos de write-back SQL direto para o TOTVS RM (Onda 1B).
/// Ver docs/spike-writeback-sql-rm.md.
/// </summary>
public static class RmWriteBackModes
{
    public const string Off = "off";
    public const string DryRun = "dry_run";
    public const string ApplyRollbackable = "apply_rollbackable";
    public const string Apply = "apply";

    public static string Normalize(string? mode) =>
        (mode ?? Off).Trim().ToLowerInvariant() switch
        {
            DryRun => DryRun,
            ApplyRollbackable => ApplyRollbackable,
            Apply => Apply,
            _ => Off,
        };

    /// <summary>
    /// Resolve o modo de férias. Se "leave.rm.writeback.mode" não estiver definido, usa
    /// o legado "leave.rm.writeback.enabled" (true -&gt; apply, false -&gt; off).
    /// </summary>
    public static string ResolveLeaveMode(IAppSettingsProvider settings)
    {
        if (settings.TryGetString(AppSettingKeys.LeaveRmWriteBackMode, out var mode) &&
            !string.IsNullOrWhiteSpace(mode))
        {
            return Normalize(mode);
        }

        return settings.GetBool(AppSettingKeys.LeaveRmWriteBackEnabled, false) ? Apply : Off;
    }

    public static string ResolvePontoMode(IAppSettingsProvider settings)
    {
        if (settings.TryGetString(AppSettingKeys.PontoRmWriteBackMode, out var mode) &&
            !string.IsNullOrWhiteSpace(mode))
        {
            return Normalize(mode);
        }

        return Off;
    }

    public static bool IsProcessing(string mode) => mode != Off;

    /// <summary>
    /// Gate de segurança: dry_run e apply_rollbackable são liberados sempre que a
    /// integração RM estiver habilitada (uso em UAT/homolog). "apply" definitivo
    /// só é permitido quando o operador marcar allow_prod=true explicitamente.
    /// </summary>
    public static bool CanExecute(string mode, bool allowProd) =>
        mode switch
        {
            Off => false,
            DryRun => true,
            ApplyRollbackable => true,
            Apply => allowProd,
            _ => false,
        };
}
