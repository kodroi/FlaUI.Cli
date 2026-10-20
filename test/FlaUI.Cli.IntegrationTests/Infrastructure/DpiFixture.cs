using System.Diagnostics;
using System.Text.Json;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.IntegrationTests.Infrastructure;

internal sealed class DpiFixture(CliRunner cli) : IAsyncDisposable
{
    private readonly string _session = Path.Combine(Path.GetTempPath(), $"dpi-fixture-{Guid.NewGuid():N}.session.json");
    private readonly List<string> _outputs = [];
    private Process? _process;
    internal int Pid { get; private set; }
    internal string SessionPath => _session;
    internal string SessionArg => $"--session \"{_session}\"";

    internal async Task Start()
    {
        var app = SolutionLocator.GetTestAppPath(SolutionLocator.FindSolutionRoot());
        var result = await Run($"session new --app \"{app}\" --args flaui-fixture-dpi");
        Pid = result.GetProperty("pid").GetInt32();
        _process = Process.GetProcessById(Pid);
        _process.EnableRaisingEvents = true;
    }

    internal async Task<JsonElement> Run(string arguments)
    {
        var result = await cli.RunAsync($"{arguments} {SessionArg}");
        var lifetime = _process is { HasExited: true } ? $" Fixture exited with code {_process.ExitCode}." : "";
        Assert.True(result.ExitCode == 0, result.Stdout + result.Stderr + lifetime);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean(), result.Stdout);
        return json.RootElement.Clone();
    }

    internal async Task<string> Find(string aid) => (await Run($"elem find --aid {aid}")).GetProperty("elementId").GetString()!;
    internal async Task<string> Value(string aid) =>
        (await Run($"elem get-value --id {await Find(aid)}")).GetProperty("value").GetString()!;

    internal async Task<(JsonElement Result, string Path)> Capture(string? elementId = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dpi-evidence-{Guid.NewGuid():N}.png");
        _outputs.Add(path);
        return (await Run($"screenshot {(elementId is null ? "" : $"--id {elementId}")} --output \"{path}\""), path);
    }

    internal void Move(double x, double y, double width, double height)
    {
        using var engine = new AutomationEngine();
        engine.Attach(Pid);
        var window = engine.GetMainWindow();
        AutomationEngine.TransformElement(window, "fixture", x, y, width, height, null);
    }

    public async ValueTask DisposeAsync()
    {
        try { if (Pid != 0) await Run("session end --close-app"); }
        finally
        {
            if (_process is { HasExited: false })
            {
                try { _process.Kill(); _process.WaitForExit(5000); }
                catch (ArgumentException) { }
            }
            _process?.Dispose();
            File.Delete(_session);
            foreach (var file in _outputs) File.Delete(file);
        }
    }
}

public sealed class DpiDesktopFactAttribute : FactAttribute
{
    public DpiDesktopFactAttribute(int scale = 0, bool mixed = false, bool negative = false, bool anyMixed = false, bool spanning = false)
    {
        using var scope = DpiContext.Enter();
        var monitors = NativeInterop.ReadCoordinateContext(nint.Zero, null).Monitors;
        if (scale != 0 && !monitors.Any(m => m.ScalePercent == scale))
            Skip = $"No real {scale}% monitor is available. Not validated by simulated DPI.";
        if (mixed && !(monitors.Any(m => m.ScalePercent == 100) && monitors.Any(m => m.ScalePercent == 200)))
            Skip = "A real 100% + 200% mixed-DPI desktop is required.";
        if (negative && !monitors.Any(m => m.Bounds.X < 0 || m.Bounds.Y < 0))
            Skip = "A real negative-origin monitor is required.";
        if (anyMixed && monitors.Select(m => m.ScalePercent).Where(s => s.HasValue).Distinct().Count() < 2)
            Skip = "Two real monitors with different scales are required.";
        if (spanning && SpanningBounds(monitors) is null)
            Skip = "Adjacent differently scaled monitors with room for the visible fixture are required.";
    }

    internal static FlaUI.Cli.Models.BoundsInfo? SpanningBounds(IReadOnlyList<FlaUI.Cli.Models.MonitorContext> monitors)
    {
        foreach (var first in monitors)
        foreach (var second in monitors.Where(m => m.ScalePercent != first.ScalePercent))
        {
            var a = first.Bounds;
            var b = second.Bounds;
            var top = Math.Max(a.Y, b.Y);
            var left = Math.Max(a.X, b.X);
            if (a.X + a.Width == b.X && Math.Min(a.Y + a.Height, b.Y + b.Height) - top >= 880 && a.Width >= 600 && b.Width >= 600)
                return new(b.X - 600, top + 40, 1200, 800);
            if (a.Y + a.Height == b.Y && Math.Min(a.X + a.Width, b.X + b.Width) - left >= 1080 && a.Height >= 500 && b.Height >= 500)
                return new(left + 40, b.Y - 500, 1000, 1000);
        }
        return null;
    }
}
