namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ElementActionTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public ElementActionTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Type_IntoTextBox_ThenGetValue_ReturnsTypedText()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var typeResult = await _fixture.Cli.RunAsync(
            $"elem type --id {found.ElementId} --text \"John\" {_fixture.SessionArg}");
        Assert.Equal(0, typeResult.ExitCode);

        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, valueResult.ExitCode);
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.Equal("John", value?.Value);
    }

    [Fact]
    public async Task SetValue_SetsTextBoxValue()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var setResult = await _fixture.Cli.RunAsync(
            $"elem set-value --id {found.ElementId} --value \"Jane\" {_fixture.SessionArg}");
        Assert.Equal(0, setResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(setResult.Stdout);
        Assert.True(action?.Success);

        // Verify get-value reflects the set value
        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.Equal("Jane", value?.Value);
    }

    [Fact]
    public async Task Click_Button_ReturnsSuccess()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid SubmitButton {_fixture.SessionArg}");
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var clickResult = await _fixture.Cli.RunAsync(
            $"elem click --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, clickResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(clickResult.Stdout);
        Assert.True(action?.Success);
    }

    [Fact]
    public async Task Select_ComboBoxItem_Succeeds()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid CountryCombo {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var selectResult = await _fixture.Cli.RunAsync(
            $"elem select --id {found.ElementId} --item \"Finland\" {_fixture.SessionArg}");
        Assert.Equal(0, selectResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(selectResult.Stdout);
        Assert.NotNull(action);
        Assert.True(action.Success);
    }

    [Fact]
    public async Task GetState_Checkbox_ReturnsToggleState()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid AgreeCheckbox {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var stateResult = await _fixture.Cli.RunAsync(
            $"elem get-state --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, stateResult.ExitCode);
        var state = CliRunner.Deserialize<GetStateResult>(stateResult.Stdout);
        Assert.NotNull(state);
        Assert.True(state.Success);
        Assert.True(state.IsEnabled);
        Assert.NotNull(state.ToggleState);
    }
}
