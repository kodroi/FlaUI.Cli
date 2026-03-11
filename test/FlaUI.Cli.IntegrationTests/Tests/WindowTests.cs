namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class WindowTests
{
    private readonly TestAppFixture _fixture;

    public WindowTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WindowList_ReturnsAtLeastOneWindow()
    {
        var result = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, result.ExitCode);

        var windowList = CliRunner.Deserialize<WindowListResult>(result.Stdout);
        Assert.NotNull(windowList);
        Assert.True(windowList.Success);
        Assert.NotNull(windowList.Windows);
        Assert.NotEmpty(windowList.Windows);

        var mainWindow = windowList.Windows.FirstOrDefault(w => w.Title == "Contact Form");
        Assert.NotNull(mainWindow);
        Assert.NotNull(mainWindow.Handle);
        Assert.False(mainWindow.IsModal);
    }

    [Fact]
    public async Task WindowFocus_BringsWindowToForeground()
    {
        // First get the window handle
        var listResult = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, listResult.ExitCode);
        var windowList = CliRunner.Deserialize<WindowListResult>(listResult.Stdout);
        Assert.NotNull(windowList?.Windows);
        var mainWindow = windowList.Windows.First(w => w.Title == "Contact Form");

        // Focus the window
        var focusResult = await _fixture.Cli.RunAsync(
            $"window focus --handle {mainWindow.Handle} {_fixture.SessionArg}");
        Assert.Equal(0, focusResult.ExitCode);

        var focus = CliRunner.Deserialize<WindowFocusResult>(focusResult.Stdout);
        Assert.NotNull(focus);
        Assert.True(focus.Success);
        Assert.Equal("Contact Form", focus.Title);
    }

    [Fact]
    public async Task WindowList_ContainsHandleAndBounds()
    {
        var result = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, result.ExitCode);

        var windowList = CliRunner.Deserialize<WindowListResult>(result.Stdout);
        Assert.NotNull(windowList?.Windows);

        var window = windowList.Windows.First();
        Assert.NotNull(window.Handle);
        Assert.NotNull(window.Bounds);
        Assert.True(window.Bounds.Width > 0);
        Assert.True(window.Bounds.Height > 0);
    }
}
