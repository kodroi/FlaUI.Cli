namespace FlaUI.Cli.IntegrationTests.Infrastructure;

public static class SolutionLocator
{
    public static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FlaUI.Cli.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not find FlaUI.Cli.sln by walking up from " + AppContext.BaseDirectory);
    }

    public static string GetTestAppPath(string solutionRoot)
    {
        var config = GetBuildConfiguration();
        return Path.Combine(solutionRoot, "test", "FlaUI.Cli.TestApp", "bin", config,
            "net10.0-windows", "FlaUI.Cli.TestApp.exe");
    }

    public static string GetCliPath(string solutionRoot)
    {
        var config = GetBuildConfiguration();
        return Path.Combine(solutionRoot, "src", "FlaUI.Cli", "bin", config,
            "net10.0", "FlaUI.Cli.exe");
    }

    public static string? GetSkipReason(string solutionRoot)
    {
        var cli = GetCliPath(solutionRoot);
        var app = GetTestAppPath(solutionRoot);

        if (!File.Exists(cli) && !File.Exists(app))
            return $"CLI ({cli}) and TestApp ({app}) not found. Build both in Release first.";
        if (!File.Exists(cli))
            return $"CLI not found at {cli}. Build FlaUI.Cli in Release first.";
        if (!File.Exists(app))
            return $"TestApp not found at {app}. Build FlaUI.Cli.TestApp in Release first.";

        return null;
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}
