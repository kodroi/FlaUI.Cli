using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class DpiInteractionTests
{
    [Fact]
    public async Task BatchDpiFailure_PreservesRecovery_AndContinuesWithPatternAction()
    {
        await using var app = new DpiFixture(new(SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot())));
        await app.Start();
        var toggleId = await app.Find("DpiToggle");
        var output = Path.Combine(Path.GetTempPath(), $"blocked-{Guid.NewGuid():N}.png");
        using var engine = new AutomationEngine();
        var sessions = new FlaUI.Cli.Services.SessionManager();
        var path = app.SessionPath;
        var session = sessions.Load(path);
        var batch = new BatchExecutor(engine, sessions, session, path);
        using var unavailable = new DpiContext(_ => nint.Zero, () => false);
        var result = batch.Execute([
            new() { Cmd = "screenshot", Args = new() { ["output"] = output } },
            new() { Cmd = "elem click", Args = new() { ["id"] = toggleId } }
        ], continueOnError: true);
        Assert.False(result.Success);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Succeeded);
        var error = result.Steps![0].Error!;
        Assert.Equal("dpi_unavailable", error.ErrorCode);
        Assert.NotNull(error.Recovery);
        Assert.NotNull(error.EffectiveWindowHandle);
        Assert.NotNull(error.CoordinateContext);
        Assert.False(File.Exists(output));
    }

    [Theory]
    [InlineData("")]
    [InlineData("--double")]
    [InlineData("--right")]
    public async Task RealMouseInput_ReportsPointAndReachesTheTarget(string option)
    {
        await using var app = new DpiFixture(new(SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot())));
        await app.Start();
        var cell = await app.Find("Batch-B");
        var result = await app.Run($"elem click --id {cell} {option}");
        Assert.True(result.GetProperty("coordinateContext").TryGetProperty("inputPoint", out _));
        Assert.NotEqual("mouse=0", await app.Value("DpiMouseStatus"));
        if (option != "--right") Assert.Equal("selected=Batch-B", await app.Value("DpiSelectionStatus"));
    }

    [Fact]
    public async Task UnavailableCoordinates_BlockMouseButAllowTogglePattern()
    {
        await using var app = new DpiFixture(new(SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot())));
        await app.Start();
        // All synchronous UIA work stays on this thread inside the engine's scope.
        using (var engine = new AutomationEngine())
        {
            engine.Attach(app.Pid);
            var window = engine.GetMainWindow();
            var toggle = window.FindFirstDescendant(cf => cf.ByAutomationId("DpiToggle"));
            var cell = window.FindFirstDescendant(cf => cf.ByAutomationId("Batch-B"));
            Assert.NotNull(toggle);
            Assert.NotNull(cell);
            using var unavailable = new DpiContext(_ => nint.Zero, () => false);
            Assert.Throws<DpiUnavailableException>(() => AutomationEngine.Click(cell));
            AutomationEngine.Click(toggle);
            Assert.Equal("On", toggle.Patterns.Toggle.Pattern.ToggleState.Value.ToString());
        }
        Assert.Equal("mouse=0", await app.Value("DpiMouseStatus"));
    }
}
