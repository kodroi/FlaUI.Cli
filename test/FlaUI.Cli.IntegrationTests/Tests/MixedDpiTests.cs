using System.Drawing;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using Xunit.Abstractions;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class MixedDpiTests(ITestOutputHelper output)
{
    [DpiDesktopFact(100)]
    public Task PhysicalCaptureAndClick_At100Percent() => AtScale(100);

    [DpiDesktopFact(200)]
    public Task PhysicalCaptureAndClick_At200Percent() => AtScale(200);

    [DpiDesktopFact(anyMixed: true)]
    public async Task SameSession_MovesAcrossAvailableDifferentScales()
    {
        var monitors = Monitors().GroupBy(m => m.ScalePercent).Select(g => g.First()).Take(2).ToArray();
        await using var app = CreateApp();
        await app.Start();
        foreach (var monitor in new[] { monitors[0], monitors[1], monitors[0] })
        {
            Place(app, monitor);
            await VerifyImageAndMouse(app);
        }
    }

    [DpiDesktopFact(mixed: true)]
    public async Task SameSession_MovesAcross100And200PercentMonitors()
    {
        var monitors = Monitors();
        await using var app = CreateApp();
        await app.Start();
        foreach (var scale in new[] { 100, 200, 100 })
        {
            Place(app, monitors.First(m => m.ScalePercent == scale));
            await VerifyImageAndMouse(app);
        }
    }

    [DpiDesktopFact(negative: true)]
    public async Task PhysicalCaptureAndClick_OnNegativeOriginMonitor()
    {
        await using var app = CreateApp();
        await app.Start();
        Place(app, Monitors().First(m => m.Bounds.X < 0 || m.Bounds.Y < 0));
        await VerifyImageAndMouse(app);
    }

    [DpiDesktopFact(spanning: true)]
    public async Task SpanningWindow_ReportsIntersectingMonitorsWithoutRescaling()
    {
        var monitors = Monitors();
        var bounds = DpiDesktopFactAttribute.SpanningBounds(monitors)!;
        await using var app = CreateApp();
        await app.Start();
        app.Move(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        var capture = await app.Capture();
        Assert.True(capture.Result.GetProperty("coordinateContext").GetProperty("monitors").GetArrayLength() >= 2);
        await VerifyImageAndMouse(app);
    }

    private async Task AtScale(int scale)
    {
        await using var app = CreateApp();
        await app.Start();
        Place(app, Monitors().First(m => m.ScalePercent == scale));
        await VerifyImageAndMouse(app);
    }

    private static DpiFixture CreateApp() => new(new(SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot())));
    private static IReadOnlyList<MonitorContext> Monitors()
    {
        using var scope = DpiContext.Enter();
        return NativeInterop.ReadCoordinateContext(nint.Zero, null).Monitors;
    }

    private static void Place(DpiFixture app, MonitorContext monitor)
    {
        var scale = (monitor.ScalePercent ?? 100) / 100.0;
        app.Move(monitor.Bounds.X + 40, monitor.Bounds.Y + 40,
            Math.Min(600 * scale, monitor.Bounds.Width - 80), Math.Min(420 * scale, monitor.Bounds.Height - 120));
    }

    private async Task VerifyImageAndMouse(DpiFixture app)
    {
        var capture = await app.Capture();
        output.WriteLine(capture.Result.ToString());
        var source = capture.Result.GetProperty("coordinateContext").GetProperty("bounds");
        using var bitmap = new Bitmap(capture.Path);
        Assert.Equal(capture.Result.GetProperty("width").GetInt32(), bitmap.Width);
        Assert.Equal(capture.Result.GetProperty("height").GetInt32(), bitmap.Height);
        foreach (var (aid, color) in new[] { ("DpiTopLeft", Color.Red), ("DpiTopRight", Color.Lime),
                     ("DpiBottomLeft", Color.Blue), ("DpiBottomRight", Color.Yellow) })
        {
            var props = await app.Run($"elem props --id {await app.Find(aid)}");
            var bounds = props.GetProperty("bounds");
            var x = (int)(bounds.GetProperty("x").GetDouble() + bounds.GetProperty("width").GetDouble() / 2 - source.GetProperty("x").GetDouble());
            var y = (int)(bounds.GetProperty("y").GetDouble() + bounds.GetProperty("height").GetDouble() / 2 - source.GetProperty("y").GetDouble());
            Assert.InRange(x, 0, bitmap.Width - 1);
            Assert.InRange(y, 0, bitmap.Height - 1);
            var actual = bitmap.GetPixel(x, y);
            Assert.True(Math.Abs(actual.R - color.R) < 10 && Math.Abs(actual.G - color.G) < 10 && Math.Abs(actual.B - color.B) < 10,
                $"{aid} at ({x},{y}): expected {color}, got {actual}.");
        }
        var cell = await app.Find("Batch-B");
        var click = await app.Run($"elem click --id {cell}");
        output.WriteLine(click.ToString());
        Assert.NotEqual("mouse=0", await app.Value("DpiMouseStatus"));
        Assert.Equal("selected=Batch-B", await app.Value("DpiSelectionStatus"));
    }
}
