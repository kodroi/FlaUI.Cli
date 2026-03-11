namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class TypeComboBoxTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public TypeComboBoxTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Type_OnComboBox_SelectsMatchingItem()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid CountryCombo {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Use 'elem type' on a ComboBox — should auto-redirect to select
        var typeResult = await _fixture.Cli.RunAsync(
            $"elem type --id {found.ElementId} --text \"Norway\" {_fixture.SessionArg}");
        Assert.Equal(0, typeResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(typeResult.Stdout);
        Assert.NotNull(action);
        Assert.True(action.Success);

        // Verify the selection took effect
        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, valueResult.ExitCode);
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.NotNull(value);
        Assert.Contains("Norway", value.Value);
    }

    [Fact]
    public async Task Type_OnComboBox_InvalidItem_ReturnsError()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid CountryCombo {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var typeResult = await _fixture.Cli.RunAsync(
            $"elem type --id {found.ElementId} --text \"Nonexistentland\" {_fixture.SessionArg}");
        var error = CliRunner.Deserialize<ErrorResult>(typeResult.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("not found", error.Message);
    }
}
