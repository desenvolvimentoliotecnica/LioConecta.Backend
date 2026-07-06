using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class PersonPhotoStorageService(
    IAppSettingsProvider settingsProvider,
    IHostEnvironment hostEnvironment) : IPersonPhotoStorageService
{
    public string BuildPublicUrl(string slug) => $"/media/people/{SanitizeSlug(slug)}.jpg";

    public async Task<string> SaveAsync(
        string slug,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        if (content.IsEmpty)
        {
            throw new InvalidOperationException("Photo content is empty.");
        }

        var safeSlug = SanitizeSlug(slug);
        var absolutePath = Path.Combine(ResolveStorageRoot(), $"{safeSlug}.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await File.WriteAllBytesAsync(absolutePath, content.ToArray(), cancellationToken);
        return BuildPublicUrl(safeSlug);
    }

    private string ResolveStorageRoot()
    {
        var configured = settingsProvider.GetString(AppSettingKeys.MediaPeopleRootPath, "App_Data/media/people");
        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(hostEnvironment.ContentRootPath, configured);
    }

    private static string SanitizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "sem-slug";
        }

        var chars = slug.Trim().ToLowerInvariant()
            .Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
            .ToArray();

        return chars.Length == 0 ? "sem-slug" : new string(chars);
    }
}
