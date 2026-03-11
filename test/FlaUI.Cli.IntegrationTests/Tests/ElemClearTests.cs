namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ElemClearTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public ElemClearTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Clear_TextBox_RemovesContent()
    {
        // Type text into the field first
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        await _fixture.Cli.RunAsync($"elem type --id {found.ElementId} --text \"Alice\" {_fixture.SessionArg}");

        // Verify text was typed
        var beforeValue = await _fixture.Cli.RunAsync($"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        var before = CliRunner.Deserialize<GetValueResult>(beforeValue.Stdout);
        Assert.Equal("Alice", before?.Value);

        // Clear the field
        var clearResult = await _fixture.Cli.RunAsync($"elem clear --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, clearResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(clearResult.Stdout);
        Assert.NotNull(action);
        Assert.True(action.Success);
        Assert.Equal("Element cleared.", action.Message);

        // Verify the field is empty
        var afterValue = await _fixture.Cli.RunAsync($"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        var after = CliRunner.Deserialize<GetValueResult>(afterValue.Stdout);
        Assert.Equal("", after?.Value);
    }

    [Fact]
    public async Task Clear_AlreadyEmptyTextBox_Succeeds()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var clearResult = await _fixture.Cli.RunAsync($"elem clear --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, clearResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(clearResult.Stdout);
        Assert.NotNull(action);
        Assert.True(action.Success);
    }

    [Fact]
    public async Task Clear_MultipleFields_ClearsEach()
    {
        // Type into two fields
        var findFirst = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        var first = CliRunner.Deserialize<ElementFindResult>(findFirst.Stdout);
        Assert.NotNull(first?.ElementId);

        var findLast = await _fixture.Cli.RunAsync($"elem find --aid LastNameInput {_fixture.SessionArg}");
        var last = CliRunner.Deserialize<ElementFindResult>(findLast.Stdout);
        Assert.NotNull(last?.ElementId);

        await _fixture.Cli.RunAsync($"elem type --id {first.ElementId} --text \"Bob\" {_fixture.SessionArg}");
        await _fixture.Cli.RunAsync($"elem type --id {last.ElementId} --text \"Smith\" {_fixture.SessionArg}");

        // Clear both
        var clear1 = await _fixture.Cli.RunAsync($"elem clear --id {first.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, clear1.ExitCode);

        var clear2 = await _fixture.Cli.RunAsync($"elem clear --id {last.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, clear2.ExitCode);

        // Verify both are empty
        var val1 = CliRunner.Deserialize<GetValueResult>(
            (await _fixture.Cli.RunAsync($"elem get-value --id {first.ElementId} {_fixture.SessionArg}")).Stdout);
        var val2 = CliRunner.Deserialize<GetValueResult>(
            (await _fixture.Cli.RunAsync($"elem get-value --id {last.ElementId} {_fixture.SessionArg}")).Stdout);

        Assert.Equal("", val1?.Value);
        Assert.Equal("", val2?.Value);
    }

    [Fact]
    public async Task Clear_InvalidElementId_ReturnsError()
    {
        var clearResult = await _fixture.Cli.RunAsync($"elem clear --id nonexistent {_fixture.SessionArg}");
        var error = CliRunner.Deserialize<ErrorResult>(clearResult.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
    }
}
