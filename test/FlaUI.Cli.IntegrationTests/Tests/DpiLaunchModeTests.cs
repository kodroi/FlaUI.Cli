using System.Drawing;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class DpiLaunchModeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ApphostAndHostedDll_UsePhysicalCoordinates(bool hosted)
    {
        var path = SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot());
        return Verify(hosted ? new CliRunner("dotnet", $"\"{Path.ChangeExtension(path, ".dll")}\"") : new(path));
    }

    [InstalledDpiToolFact]
    public Task IsolatedInstalledTool_UsesPhysicalCoordinates() =>
        Verify(new(Environment.GetEnvironmentVariable("FLAUI_DPI_TOOL_PATH")!));

    private static async Task Verify(CliRunner cli)
    {
        await using var app = new DpiFixture(cli);
        await app.Start();
        var capture = await app.Capture();
        var context = capture.Result.GetProperty("coordinateContext");
        Assert.Equal("perMonitorV2", context.GetProperty("clientAwareness").GetString());
        Assert.Equal("perMonitorV2", context.GetProperty("windowAwareness").GetString());
        var monitor = Assert.Single(context.GetProperty("monitors").EnumerateArray());
        Assert.Equal(context.GetProperty("windowDpi").GetDouble() / 96 * 100,
            monitor.GetProperty("scalePercent").GetDouble());
        var markerId = await app.Find("DpiBottomRight");
        var props = await app.Run($"elem props --id {markerId}");
        var marker = props.GetProperty("bounds");
        var source = context.GetProperty("bounds");
        using var bitmap = new Bitmap(capture.Path);
        var x = (int)(marker.GetProperty("x").GetDouble() + marker.GetProperty("width").GetDouble() / 2 - source.GetProperty("x").GetDouble());
        var y = (int)(marker.GetProperty("y").GetDouble() + marker.GetProperty("height").GetDouble() / 2 - source.GetProperty("y").GetDouble());
        Assert.InRange(x, 0, bitmap.Width - 1);
        Assert.InRange(y, 0, bitmap.Height - 1);
        var pixel = bitmap.GetPixel(x, y);
        Assert.True(pixel.R > 240 && pixel.G > 240 && pixel.B < 10, $"Bottom-right yellow marker missing: {pixel}");
        await app.Run($"elem click --id {await app.Find("Batch-B")}");
        Assert.NotEqual("mouse=0", await app.Value("DpiMouseStatus"));
        Assert.Equal("selected=Batch-B", await app.Value("DpiSelectionStatus"));
    }
}

public sealed class InstalledDpiToolFactAttribute : FactAttribute
{
    public InstalledDpiToolFactAttribute()
    {
        if (!File.Exists(Environment.GetEnvironmentVariable("FLAUI_DPI_TOOL_PATH")))
            Skip = "Prepare an isolated locally packed tool and set FLAUI_DPI_TOOL_PATH to its flaui.exe.";
    }
}
