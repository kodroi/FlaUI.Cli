namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class MenuTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public MenuTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Menu_NavigateSingleLevel_ClicksMenuItem()
    {
        var result = await _fixture.Cli.RunAsync(
            $"elem menu --path \"File > Save\" {_fixture.SessionArg}");

        var menu = CliRunner.Deserialize<MenuResult>(result.Stdout);
        Assert.NotNull(menu);
        Assert.True(menu.Success);
        Assert.Equal("Save", menu.ClickedItemName);

        // Verify status label was updated by menu click handler
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid StatusLabel {_fixture.SessionArg}");
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var valueResult = await _fixture.Cli.RunAsync(
            $"elem get-value --id {found.ElementId} {_fixture.SessionArg}");
        var value = CliRunner.Deserialize<GetValueResult>(valueResult.Stdout);
        Assert.Equal("Menu: Save", value?.Value);
    }

    [Fact]
    public async Task Menu_NavigateEditCopy_ClicksMenuItem()
    {
        var result = await _fixture.Cli.RunAsync(
            $"elem menu --path \"Edit > Copy\" {_fixture.SessionArg}");

        var menu = CliRunner.Deserialize<MenuResult>(result.Stdout);
        Assert.NotNull(menu);
        Assert.True(menu.Success);
        Assert.Equal("Copy", menu.ClickedItemName);
    }

    [Fact]
    public async Task Menu_InvalidPath_ReturnsError()
    {
        var result = await _fixture.Cli.RunAsync(
            $"elem menu --path \"NonExistent > Item\" {_fixture.SessionArg}");

        var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("not found", error.Message);
    }
}
