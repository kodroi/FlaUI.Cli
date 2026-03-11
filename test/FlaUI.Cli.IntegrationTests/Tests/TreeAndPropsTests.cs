namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class TreeAndPropsTests
{
    private readonly TestAppFixture _fixture;

    public TreeAndPropsTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Tree_ReturnsHierarchy()
    {
        var result = await _fixture.Cli.RunAsync($"elem tree {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var tree = CliRunner.Deserialize<ElementTreeResult>(result.Stdout);
        Assert.NotNull(tree);
        Assert.True(tree.Success);
        Assert.NotNull(tree.Root);
        Assert.NotEmpty(tree.Root.Children);
    }

    [Fact]
    public async Task Props_ReturnsElementProperties()
    {
        // Find an element first
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var propsResult = await _fixture.Cli.RunAsync(
            $"elem props --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, propsResult.ExitCode);
        var props = CliRunner.Deserialize<ElementPropsResult>(propsResult.Stdout);
        Assert.NotNull(props);
        Assert.True(props.Success);
        Assert.Equal("FirstNameInput", props.AutomationId);
        Assert.Equal("Edit", props.ControlType);
        Assert.True(props.IsEnabled);
    }
}
