namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ExpandCollapseTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public ExpandCollapseTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Expand_Expander_ShowsExpandedState()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestExpander {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var expandResult = await _fixture.Cli.RunAsync(
            $"elem expand --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, expandResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(expandResult.Stdout);
        Assert.True(action?.Success);

        var stateResult = await _fixture.Cli.RunAsync(
            $"elem get-state --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, stateResult.ExitCode);
        var state = CliRunner.Deserialize<GetStateResult>(stateResult.Stdout);
        Assert.Equal("Expanded", state?.ExpandState);
    }

    [Fact]
    public async Task Collapse_Expander_ShowsCollapsedState()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestExpander {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Expand first
        await _fixture.Cli.RunAsync($"elem expand --id {found.ElementId} {_fixture.SessionArg}");

        // Collapse
        var collapseResult = await _fixture.Cli.RunAsync(
            $"elem collapse --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, collapseResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(collapseResult.Stdout);
        Assert.True(action?.Success);

        var stateResult = await _fixture.Cli.RunAsync(
            $"elem get-state --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, stateResult.ExitCode);
        var state = CliRunner.Deserialize<GetStateResult>(stateResult.Stdout);
        Assert.Equal("Collapsed", state?.ExpandState);
    }

    [Fact]
    public async Task FindAfterExpand_FindsExpanderContent()
    {
        var findExpander = await _fixture.Cli.RunAsync($"elem find --aid TestExpander {_fixture.SessionArg}");
        Assert.Equal(0, findExpander.ExitCode);
        var expander = CliRunner.Deserialize<ElementFindResult>(findExpander.Stdout);
        Assert.NotNull(expander?.ElementId);

        // Expand to reveal content
        await _fixture.Cli.RunAsync($"elem expand --id {expander.ElementId} {_fixture.SessionArg}");
        await Task.Delay(200);

        // Now find the content inside
        var findContent = await _fixture.Cli.RunAsync($"elem find --aid ExpanderContent {_fixture.SessionArg}");
        Assert.Equal(0, findContent.ExitCode);
        var content = CliRunner.Deserialize<ElementFindResult>(findContent.Stdout);
        Assert.True(content?.Success);
        Assert.NotNull(content?.ElementId);
    }

    [Fact]
    public async Task ExpanderContent_BecomesVisible_AfterExpand()
    {
        // Find the expander and expand it
        var findExpander = await _fixture.Cli.RunAsync($"elem find --aid TestExpander {_fixture.SessionArg}");
        Assert.Equal(0, findExpander.ExitCode);
        var expander = CliRunner.Deserialize<ElementFindResult>(findExpander.Stdout);
        Assert.NotNull(expander?.ElementId);

        await _fixture.Cli.RunAsync($"elem expand --id {expander.ElementId} {_fixture.SessionArg}");
        await Task.Delay(200);

        // Find content and verify it's visible (not offscreen) after expanding
        var findContent = await _fixture.Cli.RunAsync($"elem find --aid ExpanderContent {_fixture.SessionArg}");
        Assert.Equal(0, findContent.ExitCode);
        var content = CliRunner.Deserialize<ElementFindResult>(findContent.Stdout);
        Assert.NotNull(content?.ElementId);

        var stateResult = await _fixture.Cli.RunAsync(
            $"elem get-state --id {content.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, stateResult.ExitCode);
        var state = CliRunner.Deserialize<GetStateResult>(stateResult.Stdout);
        Assert.False(state?.IsOffscreen);
        Assert.True(state?.IsVisible);
    }
}
