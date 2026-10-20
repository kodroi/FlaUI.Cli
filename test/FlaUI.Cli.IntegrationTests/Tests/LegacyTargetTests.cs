using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class LegacyTargetTests(TestAppFixture fixture)
{
    [Fact]
    public async Task LegacySession_WithStoredDialog_MigratesAndFallsBackAfterClosure()
    {
        var file = Path.Combine(Path.GetTempPath(), $"flaui-legacy-{Guid.NewGuid():N}.session.json");
        await fixture.Cli.RunAsync($"elem menu --path \"Help > About\" {fixture.SessionArg}");
        try
        {
            var waited = await fixture.Cli.RunAsync($"wait --title About --timeout 5000 {fixture.SessionArg}");
            using var wait = JsonDocument.Parse(waited.Stdout);
            var handle = wait.RootElement.GetProperty("windowHandle").GetString()!;
            var legacy = JsonNode.Parse(File.ReadAllText(fixture.SessionPath))!.AsObject();
            legacy.Remove("targetWindowHandle");
            legacy.Remove("targetWindowTitle");
            legacy["application"]!["mainWindowHandle"] = Convert.ToInt64(handle[2..], 16);
            legacy["application"]!["mainWindowTitle"] = "About";
            File.WriteAllText(file, legacy.ToJsonString());
            var inspected = await fixture.Cli.RunAsync($"session status --session \"{file}\"");
            Assert.Equal(0, inspected.ExitCode);
            using var selected = JsonDocument.Parse(inspected.Stdout);
            Assert.Equal(handle, selected.RootElement.GetProperty("effectiveWindowHandle").GetString());
            await fixture.Cli.RunAsync($"window close --handle {handle} {fixture.SessionArg}");
            var fallback = await fixture.Cli.RunAsync($"session status --session \"{file}\"");
            Assert.Equal(0, fallback.ExitCode);
            using var restored = JsonDocument.Parse(fallback.Stdout);
            Assert.Contains("Contact Form", restored.RootElement.GetProperty("effectiveWindowTitle").GetString());
            Assert.True(restored.RootElement.GetProperty("windowValid").GetBoolean());
        }
        finally
        {
            await fixture.Cli.RunAsync($"window close --title About {fixture.SessionArg}");
            await fixture.Cli.RunAsync($"session status {fixture.SessionArg}");
            File.Delete(file);
        }
    }
}
