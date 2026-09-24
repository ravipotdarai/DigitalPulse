using Xunit;

namespace DigitalPulse.E2ETests;

public sealed class WebShellTests
{
    [Fact]
    public void Web_project_contains_the_landing_application()
    {
        var root = FindRepoRoot();
        var index = Path.Combine(root, "web", "digitalpulse-web", "index.html");
        Assert.True(File.Exists(index), "Expected web/digitalpulse-web/index.html for the DigitalPulse SPA.");
        var html = File.ReadAllText(index);
        Assert.Contains("DigitalPulse", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Theme_architecture_is_catalogued_and_independent_of_the_api()
    {
        var root = FindRepoRoot();
        var themeDir = Path.Combine(root, "web", "digitalpulse-web", "src", "theme");
        var required = new[]
        {
            "types.ts",
            "registry.ts",
            "applyTokens.ts",
            "persist.ts",
            "ThemeProvider.tsx",
            "ThemeSwitcher.tsx",
            Path.Combine("themes", "editorial.ts"),
            Path.Combine("themes", "executive.ts"),
            Path.Combine("themes", "futureAi.ts"),
            Path.Combine("themes", "minimal.ts")
        };

        foreach (var file in required)
        {
            var path = Path.Combine(themeDir, file);
            Assert.True(File.Exists(path), $"Expected theme file {file}.");
        }

        var types = File.ReadAllText(Path.Combine(themeDir, "types.ts"));
        foreach (var id in new[] { "editorial", "executive", "future-ai", "minimal" })
        {
            Assert.Contains(id, types, StringComparison.Ordinal);
        }

        var persist = File.ReadAllText(Path.Combine(themeDir, "persist.ts"));
        Assert.Contains("localStorage", persist, StringComparison.Ordinal);
        Assert.DoesNotContain("tenantId", persist, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hardening_files_exist_for_local_containers_and_ci()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "Dockerfile")));
        Assert.True(File.Exists(Path.Combine(root, "docker-compose.yml")));
        Assert.True(File.Exists(Path.Combine(root, ".github", "workflows", "ci.yml")));
        var dockerfile = File.ReadAllText(Path.Combine(root, "Dockerfile"));
        Assert.Contains("DigitalPulse.Api", dockerfile, StringComparison.Ordinal);
        var ci = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        Assert.Contains("dotnet test", ci, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null &&
               !File.Exists(Path.Combine(dir.FullName, "DigitalPulse.sln")) &&
               !File.Exists(Path.Combine(dir.FullName, "DigitalPulse.slnx")) &&
               !File.Exists(Path.Combine(dir.FullName, "README.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
