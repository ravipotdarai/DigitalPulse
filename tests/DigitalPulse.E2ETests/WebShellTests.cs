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
