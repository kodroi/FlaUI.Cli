namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class GridTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public GridTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task GridInfo_DataGrid_ReturnsRowAndColumnCount()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var gridResult = await _fixture.Cli.RunAsync(
            $"elem grid-info --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, gridResult.ExitCode);
        var result = CliRunner.Deserialize<GridInfoResult>(gridResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(3, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
        Assert.NotNull(result.ColumnHeaders);
        Assert.Contains("Name", result.ColumnHeaders);
        Assert.Contains("Age", result.ColumnHeaders);
        Assert.Contains("City", result.ColumnHeaders);
    }

    [Fact]
    public async Task GetCell_DataGrid_ReturnsCorrectValue()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var cellResult = await _fixture.Cli.RunAsync(
            $"elem get-cell --id {found.ElementId} --row 0 --column 0 {_fixture.SessionArg}");
        Assert.Equal(0, cellResult.ExitCode);
        var result = CliRunner.Deserialize<GetCellResult>(cellResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(0, result.Row);
        Assert.Equal(0, result.Column);
        Assert.Equal("Alice", result.Value);
    }

    [Fact]
    public async Task GetCell_DataGrid_SecondRow()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var cellResult = await _fixture.Cli.RunAsync(
            $"elem get-cell --id {found.ElementId} --row 1 --column 2 {_fixture.SessionArg}");
        Assert.Equal(0, cellResult.ExitCode);
        var result = CliRunner.Deserialize<GetCellResult>(cellResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, result.Row);
        Assert.Equal(2, result.Column);
        Assert.Equal("Stockholm", result.Value);
    }
}
