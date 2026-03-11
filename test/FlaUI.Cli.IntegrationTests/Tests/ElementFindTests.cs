namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ElementFindTests
{
    private readonly TestAppFixture _fixture;

    public ElementFindTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Find_ByAutomationId_ReturnsStableSelector()
    {
        var result = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.True(found.Success);
        Assert.Equal("FirstNameInput", found.AutomationId);
        Assert.Equal(SelectorQuality.Stable, found.SelectorQuality);
    }

    [Fact]
    public async Task Find_ByAutomationId_SubmitButton_HasCorrectName()
    {
        var result = await _fixture.Cli.RunAsync($"elem find --aid SubmitButton {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.True(found.Success);
        Assert.Equal("Button", found.ControlType);
        Assert.Equal("Submit", found.Name);
    }

    [Fact]
    public async Task Find_ReturnsWindowHandle()
    {
        var result = await _fixture.Cli.RunAsync($"elem find --aid SubmitButton {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.True(found.Success);
        Assert.NotNull(found.WindowHandle);
        Assert.StartsWith("0x", found.WindowHandle);
    }

    [Fact]
    public async Task Find_ByName_WorksWithoutPolicy()
    {
        var result = await _fixture.Cli.RunAsync($"elem find --name \"Submit\" {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.True(found.Success);
        Assert.Equal(SelectorQuality.Acceptable, found.SelectorQuality);
        Assert.NotNull(found.WindowHandle);
    }

    [Fact]
    public async Task Find_NonExistent_ReturnsFailure()
    {
        var result = await _fixture.Cli.RunAsync($"elem find --aid DoesNotExist --timeout 2000 {_fixture.SessionArg}");

        // System.CommandLine v2 doesn't propagate Environment.ExitCode,
        // so we check the JSON response instead
        var found = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.False(found.Success);
        Assert.Contains("not found", found.Message, StringComparison.OrdinalIgnoreCase);
    }
}
