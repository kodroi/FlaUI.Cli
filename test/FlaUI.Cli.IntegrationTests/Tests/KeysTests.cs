namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class KeysTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public KeysTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task Keys_Tab_MoveFocus()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        await _fixture.Cli.RunAsync($"elem click --id {found.ElementId} {_fixture.SessionArg}");

        var keysResult = await _fixture.Cli.RunAsync($"elem keys --keys tab {_fixture.SessionArg}");
        var keys = CliRunner.Deserialize<KeysResult>(keysResult.Stdout);
        Assert.NotNull(keys);
        Assert.True(keys.Success);
        Assert.Equal("tab", keys.Keys);
    }

    [Fact]
    public async Task Keys_WithElementId_SendsToElement()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var keysResult = await _fixture.Cli.RunAsync(
            $"elem keys --keys tab --id {found.ElementId} {_fixture.SessionArg}");
        var keys = CliRunner.Deserialize<KeysResult>(keysResult.Stdout);
        Assert.NotNull(keys);
        Assert.True(keys.Success);
        Assert.Equal(found.ElementId, keys.ElementId);
    }

    [Fact]
    public async Task Keys_WithoutId_BringsWindowToFrontAndSendsKeys()
    {
        // Focus an element first so the app has keyboard state
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        await _fixture.Cli.RunAsync($"elem click --id {found.ElementId} {_fixture.SessionArg}");

        // Send keys without --id — should bring app window to front before sending
        var keysResult = await _fixture.Cli.RunAsync($"elem keys --keys tab {_fixture.SessionArg}");
        Assert.Equal(0, keysResult.ExitCode);
        var keys = CliRunner.Deserialize<KeysResult>(keysResult.Stdout);
        Assert.NotNull(keys);
        Assert.True(keys.Success);
        Assert.Null(keys.ElementId);
    }

    [Fact]
    public async Task Keys_InvalidKey_ReturnsError()
    {
        var keysResult = await _fixture.Cli.RunAsync(
            $"elem keys --keys invalidkey {_fixture.SessionArg}");

        var error = CliRunner.Deserialize<ErrorResult>(keysResult.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("Unknown key token", error.Message);
    }
}
