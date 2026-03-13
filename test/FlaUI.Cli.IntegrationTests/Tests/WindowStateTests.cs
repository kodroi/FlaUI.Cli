namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class WindowStateTests
{
    private readonly TestAppFixture _fixture;

    public WindowStateTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetWindowState_MainWindow_ReturnsState()
    {
        var listResult = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, listResult.ExitCode);
        var windowList = CliRunner.Deserialize<WindowListResult>(listResult.Stdout);
        Assert.NotNull(windowList?.Windows);
        var mainWindow = windowList.Windows.First(w => w.Title == "Contact Form");

        var stateResult = await _fixture.Cli.RunAsync(
            $"window get-state --handle {mainWindow.Handle} {_fixture.SessionArg}");
        Assert.Equal(0, stateResult.ExitCode);
        var state = CliRunner.Deserialize<WindowStateResult>(stateResult.Stdout);
        Assert.NotNull(state);
        Assert.True(state.Success);
        Assert.True(state.CanMinimize);
        Assert.True(state.CanMaximize);
        Assert.Equal("Contact Form", state.Title);
    }

    [Fact]
    public async Task Minimize_MainWindow_ChangesState()
    {
        var listResult = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, listResult.ExitCode);
        var windowList = CliRunner.Deserialize<WindowListResult>(listResult.Stdout);
        Assert.NotNull(windowList?.Windows);
        var mainWindow = windowList.Windows.First(w => w.Title == "Contact Form");

        try
        {
            // Minimize
            var minResult = await _fixture.Cli.RunAsync(
                $"window minimize --handle {mainWindow.Handle} {_fixture.SessionArg}");
            Assert.Equal(0, minResult.ExitCode);
            var minState = CliRunner.Deserialize<WindowStateResult>(minResult.Stdout);
            Assert.NotNull(minState);
            Assert.True(minState.Success);
            Assert.Equal("Minimized", minState.VisualState);
        }
        finally
        {
            // Restore via focus
            await _fixture.Cli.RunAsync(
                $"window focus --handle {mainWindow.Handle} {_fixture.SessionArg}");
            await Task.Delay(300);
        }
    }

    [Fact]
    public async Task Maximize_MainWindow_ChangesState()
    {
        var listResult = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, listResult.ExitCode);
        var windowList = CliRunner.Deserialize<WindowListResult>(listResult.Stdout);
        Assert.NotNull(windowList?.Windows);
        var mainWindow = windowList.Windows.First(w => w.Title == "Contact Form");

        try
        {
            // Maximize
            var maxResult = await _fixture.Cli.RunAsync(
                $"window maximize --handle {mainWindow.Handle} {_fixture.SessionArg}");
            Assert.Equal(0, maxResult.ExitCode);
            var maxState = CliRunner.Deserialize<WindowStateResult>(maxResult.Stdout);
            Assert.NotNull(maxState);
            Assert.True(maxState.Success);
            Assert.Equal("Maximized", maxState.VisualState);
        }
        finally
        {
            // Restore to normal via focus (which restores)
            // Use get-state first to check, then restore manually if needed
            var stateResult = await _fixture.Cli.RunAsync(
                $"window get-state --handle {mainWindow.Handle} {_fixture.SessionArg}");
            var state = CliRunner.Deserialize<WindowStateResult>(stateResult.Stdout);
            if (state?.VisualState != "Normal")
            {
                await _fixture.Cli.RunAsync(
                    $"window focus --handle {mainWindow.Handle} {_fixture.SessionArg}");
                await Task.Delay(300);
            }
        }
    }
}
