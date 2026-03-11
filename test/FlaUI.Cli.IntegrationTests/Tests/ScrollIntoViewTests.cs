namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ScrollIntoViewTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public ScrollIntoViewTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task ScrollIntoView_OffScreenListItem_ScrollsAndReturnsTrue()
    {
        // Item 10 is at the bottom of a 60px-high ListBox — it's off-screen
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid ScrollItem10 {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Verify the item is off-screen before scrolling
        var stateBefore = await _fixture.Cli.RunAsync(
            $"elem get-state --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, stateBefore.ExitCode);
        var stateBeforeResult = CliRunner.Deserialize<GetStateResult>(stateBefore.Stdout);
        Assert.NotNull(stateBeforeResult);
        Assert.True(stateBeforeResult.IsOffscreen);

        // Scroll it into view
        var scrollResult = await _fixture.Cli.RunAsync(
            $"elem scroll-into-view --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, scrollResult.ExitCode);
        var result = CliRunner.Deserialize<ScrollIntoViewResult>(scrollResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Scrolled);

        // Verify the item is now visible
        var stateAfter = await _fixture.Cli.RunAsync(
            $"elem get-state --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, stateAfter.ExitCode);
        var stateAfterResult = CliRunner.Deserialize<GetStateResult>(stateAfter.Stdout);
        Assert.NotNull(stateAfterResult);
        Assert.False(stateAfterResult.IsOffscreen);
    }

    [Fact]
    public async Task ScrollIntoView_ElementWithoutScrollItemPattern_ReturnsFalse()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid SubmitButton {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var scrollResult = await _fixture.Cli.RunAsync(
            $"elem scroll-into-view --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, scrollResult.ExitCode);
        var result = CliRunner.Deserialize<ScrollIntoViewResult>(scrollResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(found.ElementId, result.ElementId);
        Assert.False(result.Scrolled);
    }

    [Fact]
    public async Task ScrollIntoView_InvalidElement_ReturnsError()
    {
        var scrollResult = await _fixture.Cli.RunAsync(
            $"elem scroll-into-view --id nonexistent {_fixture.SessionArg}");
        var error = CliRunner.Deserialize<ErrorResult>(scrollResult.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
    }
}
