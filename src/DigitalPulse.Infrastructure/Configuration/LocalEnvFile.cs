namespace DigitalPulse.Infrastructure.Configuration;

public static class LocalEnvFile
{
    public static void Load()
    {
        foreach (var file in CandidateFiles())
        {
            Apply(file);
        }
    }

    private static IEnumerable<string> CandidateFiles()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                foreach (var name in new[] { ".env", ".env.local" })
                {
                    var path = Path.Combine(dir.FullName, name);
                    if (seen.Add(path) && File.Exists(path))
                    {
                        yield return path;
                    }
                }

                if (File.Exists(Path.Combine(dir.FullName, "DigitalPulse.slnx")) ||
                    File.Exists(Path.Combine(dir.FullName, "Directory.Packages.props")))
                {
                    break;
                }

                dir = dir.Parent;
            }
        }
    }

    private static void Apply(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
            {
                continue;
            }

            var split = line.Split('=', 2);
            var key = split[0].Trim();
            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is { Length: > 0 })
            {
                continue;
            }

            var value = split[1].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
