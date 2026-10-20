using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class DpiScreenshotTests(TestAppFixture fixture)
{
    [Fact]
    public async Task Screenshot_ReportsPhysicalSourceAndCurrentMonitorContext()
    {
        var output = Path.Combine(Path.GetTempPath(), $"dpi-{Guid.NewGuid():N}.png");
        try
        {
            var result = await fixture.Cli.RunAsync($"screenshot --output \"{output}\" {fixture.SessionArg}");
            Assert.Equal(0, result.ExitCode);
            using var json = JsonDocument.Parse(result.Stdout);
            var root = json.RootElement;
            var context = root.GetProperty("coordinateContext");
            Assert.Equal("physicalPixels", context.GetProperty("coordinateSpace").GetString());
            Assert.Equal("perMonitorV2", context.GetProperty("clientAwareness").GetString());
            Assert.Equal("desktopRegion", context.GetProperty("captureMethod").GetString());
            var bounds = context.GetProperty("bounds");
            Assert.Equal(root.GetProperty("width").GetInt32(), bounds.GetProperty("width").GetInt32());
            Assert.Equal(root.GetProperty("height").GetInt32(), bounds.GetProperty("height").GetInt32());
            Assert.NotEmpty(context.GetProperty("monitors").EnumerateArray());
        }
        finally { if (File.Exists(output)) File.Delete(output); }
    }
}
