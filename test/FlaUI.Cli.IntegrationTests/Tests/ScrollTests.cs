namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ScrollTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public ScrollTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task GetScroll_ListBox_ReturnsScrollInfo()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid ScrollTestList {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var scrollResult = await _fixture.Cli.RunAsync(
            $"elem get-scroll --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, scrollResult.ExitCode);
        var result = CliRunner.Deserialize<ScrollInfoResult>(scrollResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.VerticallyScrollable);
    }

    [Fact]
    public async Task Scroll_ListBox_ChangesPosition()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid ScrollTestList {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Scroll to bottom
        var scrollResult = await _fixture.Cli.RunAsync(
            $"elem scroll --id {found.ElementId} --vertical 100 {_fixture.SessionArg}");
        Assert.Equal(0, scrollResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(scrollResult.Stdout);
        Assert.NotNull(action);
        Assert.True(action.Success);

        // Verify position changed
        var infoResult = await _fixture.Cli.RunAsync(
            $"elem get-scroll --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, infoResult.ExitCode);
        var info = CliRunner.Deserialize<ScrollInfoResult>(infoResult.Stdout);
        Assert.NotNull(info);
        Assert.True(info.VerticalPercent > 0);
    }
}
