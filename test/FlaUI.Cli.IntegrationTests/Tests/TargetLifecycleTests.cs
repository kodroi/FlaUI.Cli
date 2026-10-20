using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class TargetLifecycleTests(TestAppFixture fixture)
{
    [Fact]
    public async Task Wait_SelectsDialog_AndClosureRestoresMainWindow()
    {
        var mainElementResult = await fixture.Cli.RunAsync($"elem find --aid SubmitButton {fixture.SessionArg}");
        using var mainElement = JsonDocument.Parse(mainElementResult.Stdout);
        var mainId = mainElement.RootElement.GetProperty("elementId").GetString();
        await fixture.Cli.RunAsync($"elem menu --path \"Help > About\" {fixture.SessionArg}");
        var output = Path.Combine(Path.GetTempPath(), $"flaui-target-{Guid.NewGuid():N}.png");
        try
        {
            var wait = await fixture.Cli.RunAsync($"wait --title About --timeout 5000 {fixture.SessionArg}");
            using var waited = JsonDocument.Parse(wait.Stdout);
            var handle = waited.RootElement.GetProperty("windowHandle").GetString();
            var focused = await fixture.Cli.RunAsync($"window focus --handle {handle} {fixture.SessionArg}");
            Assert.Equal(0, focused.ExitCode);
            using var focus = JsonDocument.Parse(focused.Stdout);
            Assert.Equal(handle, focus.RootElement.GetProperty("effectiveWindowHandle").GetString());
            var found = await fixture.Cli.RunAsync($"elem find --aid AboutTitle --window {handle} {fixture.SessionArg}");
            Assert.Equal(0, found.ExitCode);
            var explicitCapture = await fixture.Cli.RunAsync($"screenshot --window {handle} --output \"{output}\" {fixture.SessionArg}");
            Assert.Equal(0, explicitCapture.ExitCode);
            var status = await fixture.Cli.RunAsync($"session status {fixture.SessionArg}");
            using var selected = JsonDocument.Parse(status.Stdout);
            Assert.Equal(handle, selected.RootElement.GetProperty("effectiveWindowHandle").GetString());
            var mainRead = await fixture.Cli.RunAsync($"elem get-value --id {mainId} {fixture.SessionArg}");
            using var read = JsonDocument.Parse(mainRead.Stdout);
            Assert.Equal(mainElement.RootElement.GetProperty("windowHandle").GetString(),
                read.RootElement.GetProperty("effectiveWindowHandle").GetString());
            var constrained = await fixture.Cli.RunAsync($"elem get-value --id {mainId} --window {handle} {fixture.SessionArg}");
            Assert.Equal(3, constrained.ExitCode);
            var capture = await fixture.Cli.RunAsync($"screenshot --output \"{output}\" {fixture.SessionArg}");
            using var captured = JsonDocument.Parse(capture.Stdout);
            Assert.Equal(handle, captured.RootElement.GetProperty("effectiveWindowHandle").GetString());
            await fixture.Cli.RunAsync($"window close --handle {handle} {fixture.SessionArg}");
            var fallback = await fixture.Cli.RunAsync($"session status {fixture.SessionArg}");
            using var restored = JsonDocument.Parse(fallback.Stdout);
            Assert.Contains("Contact Form", restored.RootElement.GetProperty("effectiveWindowTitle").GetString());
            Assert.True(restored.RootElement.GetProperty("targetFallback").GetBoolean());
        }
        finally
        {
            await fixture.Cli.RunAsync($"window close --title About {fixture.SessionArg}");
            File.Delete(output);
        }
    }

    [Theory]
    [InlineData("0xNOT", "invalid_handle", 1)]
    [InlineData("0x1", "not_found", 3)]
    public async Task InvalidExplicitWindow_HasStructuredFailure(string handle, string code, int exitCode)
    {
        var result = await fixture.Cli.RunAsync($"elem find --aid SubmitButton --window {handle} {fixture.SessionArg}");
        Assert.Equal(exitCode, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.Equal(code, json.RootElement.GetProperty("errorCode").GetString());
        Assert.False(json.RootElement.TryGetProperty("effectiveWindowHandle", out _));
    }
}
