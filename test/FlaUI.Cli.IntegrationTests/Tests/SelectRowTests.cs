namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class SelectRowTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public SelectRowTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task SelectRow_FirstRow_SelectsAndUpdatesStatus()
    {
        var findGrid = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findGrid.ExitCode);
        var grid = CliRunner.Deserialize<ElementFindResult>(findGrid.Stdout);
        Assert.NotNull(grid?.ElementId);

        var selectResult = await _fixture.Cli.RunAsync(
            $"elem select-row --id {grid.ElementId} --row 0 {_fixture.SessionArg}");
        Assert.Equal(0, selectResult.ExitCode);
        var result = CliRunner.Deserialize<SelectRowResult>(selectResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(0, result.Row);

        // Verify StatusLabel shows Alice
        var findStatus = await _fixture.Cli.RunAsync($"elem find --aid StatusLabel {_fixture.SessionArg}");
        Assert.Equal(0, findStatus.ExitCode);
        var status = CliRunner.Deserialize<ElementFindResult>(findStatus.Stdout);
        Assert.NotNull(status?.ElementId);

        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {status.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, valueResult.ExitCode);
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.NotNull(value?.Value);
        Assert.Contains("Alice", value.Value);
    }

    [Fact]
    public async Task SelectRow_DifferentRows_SelectsSequentially()
    {
        var findGrid = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findGrid.ExitCode);
        var grid = CliRunner.Deserialize<ElementFindResult>(findGrid.Stdout);
        Assert.NotNull(grid?.ElementId);

        // Select row 1 (Bob)
        await _fixture.Cli.RunAsync(
            $"elem select-row --id {grid.ElementId} --row 1 {_fixture.SessionArg}");

        // Select row 2 (Carol)
        var selectResult = await _fixture.Cli.RunAsync(
            $"elem select-row --id {grid.ElementId} --row 2 {_fixture.SessionArg}");
        Assert.Equal(0, selectResult.ExitCode);
        var result = CliRunner.Deserialize<SelectRowResult>(selectResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(2, result.Row);

        // Verify StatusLabel shows Carol
        var findStatus = await _fixture.Cli.RunAsync($"elem find --aid StatusLabel {_fixture.SessionArg}");
        Assert.Equal(0, findStatus.ExitCode);
        var status = CliRunner.Deserialize<ElementFindResult>(findStatus.Stdout);
        Assert.NotNull(status?.ElementId);

        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {status.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, valueResult.ExitCode);
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.NotNull(value?.Value);
        Assert.Contains("Carol", value.Value);
    }

    [Fact]
    public async Task SelectRow_InvalidIndex_ReturnsError()
    {
        var findGrid = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findGrid.ExitCode);
        var grid = CliRunner.Deserialize<ElementFindResult>(findGrid.Stdout);
        Assert.NotNull(grid?.ElementId);

        var selectResult = await _fixture.Cli.RunAsync(
            $"elem select-row --id {grid.ElementId} --row 99 {_fixture.SessionArg}");
        var error = CliRunner.Deserialize<ErrorResult>(selectResult.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
    }
}
