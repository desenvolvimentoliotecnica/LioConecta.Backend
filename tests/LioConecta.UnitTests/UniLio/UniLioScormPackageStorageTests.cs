using LioConecta.Infrastructure.Services.UniLio;

namespace LioConecta.UnitTests.UniLio;

public sealed class UniLioScormPackageStorageTests
{
    [Fact]
    public async Task ExtractAndValidate_DemoZip_ReturnsLaunchAndScorm12()
    {
        var zipPath = ResolveDemoZip();
        Assert.True(File.Exists(zipPath), $"Fixture ZIP não encontrado: {zipPath}");

        var packageDir = Path.Combine(Path.GetTempPath(), $"unilio-scorm-test-{Guid.NewGuid():N}");
        await using var stream = File.OpenRead(zipPath);

        var (launchPath, title, scoCount) = await UniLioScormPackageStorage.ExtractAndValidateAsync(
            stream,
            packageDir,
            CancellationToken.None);

        try
        {
            Assert.False(string.IsNullOrWhiteSpace(launchPath));
            Assert.Contains("index.html", launchPath, StringComparison.OrdinalIgnoreCase);
            Assert.True(scoCount >= 1);
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.True(File.Exists(Path.Combine(packageDir, launchPath.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(packageDir))
            {
                Directory.Delete(packageDir, recursive: true);
            }
        }
    }

    private static string ResolveDemoZip()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "LioConecta-FrontEnd", "e2e", "fixtures", "unilio-scorm-demo.zip")),
            Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects", "LioConecta-FrontEnd", "e2e", "fixtures", "unilio-scorm-demo.zip")),
            @"C:\Users\leonardo.mendes\Projects\LioConecta-FrontEnd\e2e\fixtures\unilio-scorm-demo.zip",
        };

        return candidates.First(File.Exists);
    }
}
