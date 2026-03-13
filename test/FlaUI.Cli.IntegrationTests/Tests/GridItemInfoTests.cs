namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class GridItemInfoTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public GridItemInfoTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task GridItemInfo_DataGridCell_ReturnsRowAndColumn()
    {
        // Find the DataGrid
        var findGrid = await _fixture.Cli.RunAsync($"elem find --aid TestGrid {_fixture.SessionArg}");
        Assert.Equal(0, findGrid.ExitCode);
        var grid = CliRunner.Deserialize<ElementFindResult>(findGrid.Stdout);
        Assert.NotNull(grid?.ElementId);

        // Get tree to find a cell element
        var treeResult = await _fixture.Cli.RunAsync(
            $"elem tree --root {grid.ElementId} --depth 5 {_fixture.SessionArg}");
        Assert.Equal(0, treeResult.ExitCode);
        var tree = CliRunner.Deserialize<ElementTreeResult>(treeResult.Stdout);
        Assert.NotNull(tree?.Root);

        // Find a DataItem (row) then a cell within it
        var dataItem = FindNodeByControlType(tree.Root, "DataItem");
        Assert.NotNull(dataItem);

        // The first child that is not a header should be a cell with GridItem pattern
        var cell = dataItem.Children.FirstOrDefault(c => c.ControlType != "Header");
        Assert.NotNull(cell);
        Assert.NotNull(cell.ElementId);

        var gridItemResult = await _fixture.Cli.RunAsync(
            $"elem grid-item-info --id {cell.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, gridItemResult.ExitCode);
        var result = CliRunner.Deserialize<GridItemInfoResult>(gridItemResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(0, result.Row);
        Assert.True(result.ColumnSpan >= 1);
    }

    private static TreeNode? FindNodeByControlType(TreeNode node, string controlType)
    {
        if (node.ControlType == controlType)
            return node;

        foreach (var child in node.Children)
        {
            var found = FindNodeByControlType(child, controlType);
            if (found is not null)
                return found;
        }

        return null;
    }
}
