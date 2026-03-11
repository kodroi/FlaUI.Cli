using System.Reflection;
using System.Text.Json;

namespace FlaUI.Cli.Infrastructure;

public static class UpdateChecker
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".flaui");

    private static readonly string CacheFile = Path.Combine(CacheDir, "last-update-check.json");
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);
    private const string PackageId = "flaui.tool";
    private const string NuGetIndexUrl = $"https://api.nuget.org/v3-flatcontainer/{PackageId}/index.json";

    public static void RunInBackground()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await CheckForUpdateAsync();
            }
            catch
            {
                // Never let update check crash the CLI
            }
        });
    }

    private static async Task CheckForUpdateAsync()
    {
        var currentVersion = GetCurrentVersion();
        if (currentVersion is null)
            return;

        var cache = ReadCache();
        if (cache is not null && DateTime.UtcNow - cache.LastCheck < CheckInterval)
        {
            // Still within check interval — show cached message if applicable
            if (cache.LatestVersion is not null && IsNewer(cache.LatestVersion, currentVersion))
                PrintUpdateMessage(currentVersion, cache.LatestVersion);
            return;
        }

        var latestVersion = await FetchLatestVersionAsync();
        if (latestVersion is null)
            return;

        WriteCache(new UpdateCache { LastCheck = DateTime.UtcNow, LatestVersion = latestVersion });

        if (IsNewer(latestVersion, currentVersion))
            PrintUpdateMessage(currentVersion, latestVersion);
    }

    private static string? GetCurrentVersion()
    {
        var attr = typeof(UpdateChecker).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = attr?.InformationalVersion;
        // Strip build metadata (e.g. "0.1.0+Branch.main.Sha.abc123" → "0.1.0")
        if (version is not null)
        {
            var plusIndex = version.IndexOf('+');
            if (plusIndex >= 0)
                version = version[..plusIndex];
        }
        return version;
    }

    private static async Task<string?> FetchLatestVersionAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var json = await http.GetStringAsync(NuGetIndexUrl);
        using var doc = JsonDocument.Parse(json);
        var versions = doc.RootElement.GetProperty("versions");
        if (versions.GetArrayLength() == 0)
            return null;

        // Last entry is the latest version
        return versions[versions.GetArrayLength() - 1].GetString();
    }

    private static bool IsNewer(string latest, string current)
    {
        return Version.TryParse(latest, out var latestVer)
               && Version.TryParse(current, out var currentVer)
               && latestVer > currentVer;
    }

    private static void PrintUpdateMessage(string current, string latest)
    {
        Console.Error.WriteLine(
            $"A new version of FlaUI.Tool is available: {current} \u2192 {latest}. " +
            "Run 'dotnet tool update -g FlaUI.Tool' to update.");
    }

    private static UpdateCache? ReadCache()
    {
        try
        {
            if (!File.Exists(CacheFile))
                return null;
            var json = File.ReadAllText(CacheFile);
            return JsonSerializer.Deserialize<UpdateCache>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteCache(UpdateCache cache)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var json = JsonSerializer.Serialize(cache);
            File.WriteAllText(CacheFile, json);
        }
        catch
        {
            // Non-critical — ignore write failures
        }
    }

    private sealed class UpdateCache
    {
        public DateTime LastCheck { get; set; }
        public string? LatestVersion { get; set; }
    }
}
