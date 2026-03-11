namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class VirtualizedSelectTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public VirtualizedSelectTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Select_VirtualizedCombo_OffscreenItem_Succeeds()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid VirtualizedCombo {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Select an item far down the list that would be off-screen in a virtualized combo
        var selectResult = await _fixture.Cli.RunAsync(
            $"elem select --id {found.ElementId} --item \"VItem 150\" {_fixture.SessionArg}");
        Assert.Equal(0, selectResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(selectResult.Stdout);
        Assert.True(action?.Success);

        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, valueResult.ExitCode);
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.Contains("VItem 150", value?.Value);
    }
}
