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

    [Fact]
    public async Task ScrollIntoView_OffScreenButton_ScrollsViaAncestorPattern()
    {
        // Find the OffScreenButton inside the 80px ScrollViewer
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid OffScreenButton {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Get initial bounds — button is below the 80px viewport
        var propsBefore = await _fixture.Cli.RunAsync(
            $"elem props --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, propsBefore.ExitCode);
        var boundsBefore = CliRunner.Deserialize<ElementPropsResult>(propsBefore.Stdout);
        Assert.NotNull(boundsBefore?.Bounds);

        // Scroll into view — should use ancestor ScrollPattern fallback
        var scrollResult = await _fixture.Cli.RunAsync(
            $"elem scroll-into-view --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, scrollResult.ExitCode);
        var result = CliRunner.Deserialize<ScrollIntoViewResult>(scrollResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Scrolled);

        // Get bounds after scroll — Y should have changed as the element moved up
        var propsAfter = await _fixture.Cli.RunAsync(
            $"elem props --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, propsAfter.ExitCode);
        var boundsAfter = CliRunner.Deserialize<ElementPropsResult>(propsAfter.Stdout);
        Assert.NotNull(boundsAfter?.Bounds);
        Assert.True(boundsAfter.Bounds.Y < boundsBefore.Bounds.Y,
            $"Expected button to move up after scroll. Before Y={boundsBefore.Bounds.Y}, After Y={boundsAfter.Bounds.Y}");
    }

    [Fact]
    public async Task Click_OffScreenButton_WorksWithAutoScroll()
    {
        // Find the OffScreenButton
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid OffScreenButton {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Click it — should auto-scroll via EnsureInteractable and then click
        var clickResult = await _fixture.Cli.RunAsync(
            $"elem click --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, clickResult.ExitCode);

        // Verify StatusLabel shows the click message
        var findStatus = await _fixture.Cli.RunAsync($"elem find --aid StatusLabel {_fixture.SessionArg}");
        Assert.Equal(0, findStatus.ExitCode);
        var status = CliRunner.Deserialize<ElementFindResult>(findStatus.Stdout);
        Assert.NotNull(status?.ElementId);

        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {status.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, valueResult.ExitCode);
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.NotNull(value?.Value);
        Assert.Contains("OffScreenButton clicked", value.Value);
    }
}
