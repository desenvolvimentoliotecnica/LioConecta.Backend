using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services.UniLio;

internal static class UniLioScormPackageStorage
{
    public const string PublicPathPrefix = "/unilio/scorm";
    public const long MaxSizeBytes = 209_715_200; // 200 MB

    public static string ResolvePackagesRoot(IHostEnvironment environment) =>
        Path.Combine(environment.ContentRootPath, "App_Data", "unilio", "scorm-packages");

    public static string ResolvePackageDirectory(IHostEnvironment environment, Guid packageId) =>
        Path.Combine(ResolvePackagesRoot(environment), packageId.ToString("N"));

    public static void ValidateZip(string fileName, long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (sizeBytes > MaxSizeBytes)
        {
            throw new InvalidOperationException("Pacote SCORM excede o limite de 200 MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Envie um arquivo .zip no formato SCORM 1.2.");
        }
    }

    public static async Task<(string LaunchPath, string ManifestTitle, int ScoCount)> ExtractAndValidateAsync(
        Stream zipStream,
        string packageDirectory,
        CancellationToken cancellationToken)
    {
        if (Directory.Exists(packageDirectory))
        {
            Directory.Delete(packageDirectory, recursive: true);
        }

        Directory.CreateDirectory(packageDirectory);

        var tempZip = Path.Combine(Path.GetTempPath(), $"unilio-scorm-{Guid.NewGuid():N}.zip");
        try
        {
            await using (var fs = File.Create(tempZip))
            {
                await zipStream.CopyToAsync(fs, cancellationToken);
            }

            ExtractZipSafely(tempZip, packageDirectory);

            var manifestPath = FindManifest(packageDirectory)
                ?? throw new InvalidOperationException("Pacote inválido: imsmanifest.xml não encontrado.");

            var manifestDir = Path.GetDirectoryName(manifestPath)!;
            var (launchRelative, title, scoCount, isScorm12) = ParseManifest(manifestPath, manifestDir, packageDirectory);

            if (!isScorm12)
            {
                throw new InvalidOperationException(
                    "Apenas SCORM 1.2 é suportado neste MVP. Pacotes SCORM 2004 não são aceitos.");
            }

            if (string.IsNullOrWhiteSpace(launchRelative))
            {
                throw new InvalidOperationException("Pacote inválido: não foi possível determinar o arquivo de launch do SCO.");
            }

            var launchAbsolute = Path.GetFullPath(Path.Combine(packageDirectory, launchRelative.Replace('/', Path.DirectorySeparatorChar)));
            if (!launchAbsolute.StartsWith(Path.GetFullPath(packageDirectory), StringComparison.OrdinalIgnoreCase)
                || !File.Exists(launchAbsolute))
            {
                throw new InvalidOperationException($"Arquivo de launch não encontrado: {launchRelative}");
            }

            return (launchRelative.Replace('\\', '/'), title, scoCount);
        }
        catch
        {
            if (Directory.Exists(packageDirectory))
            {
                Directory.Delete(packageDirectory, recursive: true);
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempZip))
            {
                File.Delete(tempZip);
            }
        }
    }

    public static void DeletePackageDirectory(IHostEnvironment environment, string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            return;
        }

        var absolute = Path.Combine(ResolvePackagesRoot(environment), storageRoot);
        if (Directory.Exists(absolute))
        {
            Directory.Delete(absolute, recursive: true);
        }
    }

    public static string BuildPublicLaunchUrl(Guid packageId, string launchPath) =>
        $"{PublicPathPrefix}/{packageId:N}/{launchPath.TrimStart('/')}";

    private static void ExtractZipSafely(string zipPath, string destinationDirectory)
    {
        var destFull = Path.GetFullPath(destinationDirectory);
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith('/'))
            {
                var dirPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
                if (!dirPath.StartsWith(destFull, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Pacote ZIP contém caminho inválido (path traversal).");
                }

                Directory.CreateDirectory(dirPath);
                continue;
            }

            var targetPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
            if (!targetPath.StartsWith(destFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Pacote ZIP contém caminho inválido (path traversal).");
            }

            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            entry.ExtractToFile(targetPath, overwrite: true);
        }
    }

    private static string? FindManifest(string packageDirectory)
    {
        var direct = Path.Combine(packageDirectory, "imsmanifest.xml");
        if (File.Exists(direct))
        {
            return direct;
        }

        return Directory
            .EnumerateFiles(packageDirectory, "imsmanifest.xml", SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static (string LaunchRelative, string Title, int ScoCount, bool IsScorm12) ParseManifest(
        string manifestPath,
        string manifestDir,
        string packageRoot)
    {
        XDocument doc;
        using (var stream = File.OpenRead(manifestPath))
        {
            doc = XDocument.Load(stream);
        }

        var root = doc.Root ?? throw new InvalidOperationException("imsmanifest.xml inválido.");
        XNamespace ns = root.GetDefaultNamespace();
        XNamespace adlcp = root.GetNamespaceOfPrefix("adlcp") ?? "http://www.adlnet.org/xsd/adlcp_rootv1p2";
        XNamespace imsss = root.GetNamespaceOfPrefix("imsss") ?? "http://www.imsglobal.org/xsd/imsss";

        var schemaVersion = root
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "metadata")
            ?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "schemaversion")
            ?.Value
            ?.Trim();

        var hasScorm2004 = string.Equals(schemaVersion, "CAM 1.3", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaVersion, "2004 3rd Edition", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaVersion, "2004 4th Edition", StringComparison.OrdinalIgnoreCase)
            || root.Descendants(imsss + "sequencing").Any()
            || root.ToString().Contains("adlcp_v1p3", StringComparison.OrdinalIgnoreCase);

        var looksLike12 = string.IsNullOrWhiteSpace(schemaVersion)
            || schemaVersion.Contains("1.2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaVersion, "1.2", StringComparison.OrdinalIgnoreCase);

        var isScorm12 = looksLike12 && !hasScorm2004;

        var title = root
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "organizations")
            ?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "organization")
            ?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "title")
            ?.Value
            ?.Trim()
            ?? "Pacote SCORM";

        var organizations = root.Elements().FirstOrDefault(e => e.Name.LocalName == "organizations");
        var defaultOrgId = organizations?.Attribute("default")?.Value;
        var organization = organizations?
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "organization"
                && (defaultOrgId is null || e.Attribute("identifier")?.Value == defaultOrgId))
            ?? organizations?.Elements().FirstOrDefault(e => e.Name.LocalName == "organization");

        var firstItem = organization?
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "item" && e.Attribute("identifierref") is not null);

        var resourceId = firstItem?.Attribute("identifierref")?.Value;
        var resources = root.Elements().FirstOrDefault(e => e.Name.LocalName == "resources");
        var resource = resources?
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "resource"
                && (resourceId is null || e.Attribute("identifier")?.Value == resourceId));

        resource ??= resources?.Elements().FirstOrDefault(e => e.Name.LocalName == "resource");

        var scoCount = resources?
            .Elements()
            .Count(e => e.Name.LocalName == "resource"
                && string.Equals(
                    e.Attribute(adlcp + "scormtype")?.Value
                    ?? e.Attributes().FirstOrDefault(a => a.Name.LocalName == "scormtype")?.Value,
                    "sco",
                    StringComparison.OrdinalIgnoreCase))
            ?? 0;

        if (scoCount == 0)
        {
            scoCount = resources?.Elements().Count(e => e.Name.LocalName == "resource") ?? 1;
        }

        var href = resource?.Attribute("href")?.Value
            ?? throw new InvalidOperationException("Resource de launch sem atributo href.");

        // Launch path relative to package root (account for nested manifest).
        var launchFromManifestDir = Path.GetFullPath(Path.Combine(manifestDir, href.Replace('/', Path.DirectorySeparatorChar)));
        var packageFull = Path.GetFullPath(packageRoot);
        var relative = Path.GetRelativePath(packageFull, launchFromManifestDir).Replace('\\', '/');

        return (relative, title, Math.Max(1, scoCount), isScorm12);
    }
}
