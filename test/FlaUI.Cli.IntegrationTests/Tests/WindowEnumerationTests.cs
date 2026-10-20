using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class WindowEnumerationTests
{
    [Fact]
    public async Task NativeDecorativeWindow_DoesNotPreventUsefulWindowDiscovery()
    {
        var root = SolutionLocator.FindSolutionRoot();
        var cli = new CliRunner(SolutionLocator.GetCliPath(root));
        var session = Path.Combine(Path.GetTempPath(), $"flaui-enum-{Guid.NewGuid():N}.session.json");
        try
        {
            var launched = await cli.RunAsync($"session new --app \"{SolutionLocator.GetTestAppPath(root)}\" --args flaui-fixture-decorative --wait-title \"Contact Form\" --session \"{session}\"");
            Assert.Equal(0, launched.ExitCode);
            var listed = await cli.RunAsync($"window list --session \"{session}\"");
            Assert.Equal(0, listed.ExitCode);
            using var json = JsonDocument.Parse(listed.Stdout);
            var windows = json.RootElement.GetProperty("windows").EnumerateArray().ToArray();
            Assert.Contains(windows, window => window.GetProperty("title").GetString()!.Contains("Contact Form"));
            var decorative = Assert.Single(windows, window => window.GetProperty("title").GetString() == "FlaUI decorative");
            Assert.Equal("Static", decorative.GetProperty("className").GetString(), ignoreCase: true);
            Assert.True(decorative.TryGetProperty("bounds", out _));
            Assert.False(decorative.TryGetProperty("isModal", out var modal) && modal.ValueKind != JsonValueKind.Null);
            Assert.False(json.RootElement.GetProperty("complete").GetBoolean());
            Assert.NotEmpty(json.RootElement.GetProperty("warnings").EnumerateArray());
        }
        finally
        {
            if (File.Exists(session)) await cli.RunAsync($"session end --close-app --force --session \"{session}\"");
            File.Delete(session);
        }
    }
}
