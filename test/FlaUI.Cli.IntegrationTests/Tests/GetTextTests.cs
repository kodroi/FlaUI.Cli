namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class GetTextTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public GetTextTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task GetText_RichTextBox_ReturnsFullText()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestRichText {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var textResult = await _fixture.Cli.RunAsync(
            $"elem get-text --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, textResult.ExitCode);
        var result = CliRunner.Deserialize<GetTextResult>(textResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Text);
        Assert.Contains("sample rich text content", result.Text);
    }
}
