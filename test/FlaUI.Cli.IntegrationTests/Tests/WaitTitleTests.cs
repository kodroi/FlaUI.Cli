namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class WaitTitleTests
{
    private readonly TestAppFixture _fixture;

    public WaitTitleTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WaitTitle_ExistingWindow_SucceedsImmediately()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --title \"Contact Form\" --timeout 5000 {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var wait = CliRunner.Deserialize<WaitResult>(result.Stdout);
        Assert.NotNull(wait);
        Assert.True(wait.Success);
        Assert.True(wait.Elapsed < 5000);
        Assert.NotNull(wait.WindowHandle);
        Assert.NotNull(wait.WindowTitle);
        Assert.Contains("Contact Form", wait.WindowTitle);
    }

    [Fact]
    public async Task WaitTitle_CaseInsensitiveMatch()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --title \"contact form\" --timeout 5000 {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var wait = CliRunner.Deserialize<WaitResult>(result.Stdout);
        Assert.NotNull(wait);
        Assert.True(wait.Success);
    }

    [Fact]
    public async Task WaitTitle_PartialMatch()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --title \"Contact\" --timeout 5000 {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var wait = CliRunner.Deserialize<WaitResult>(result.Stdout);
        Assert.NotNull(wait);
        Assert.True(wait.Success);
    }

    [Fact]
    public async Task WaitTitle_NonExistentWindow_TimesOut()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --title \"NonExistentWindow12345\" --timeout 1000 {_fixture.SessionArg}");

        Assert.NotEqual(0, result.ExitCode);
        var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("Timeout", error.Message);
    }

    [Fact]
    public async Task WaitTitle_ReturnsWindowHandle()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --title \"Contact Form\" --timeout 5000 {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var wait = CliRunner.Deserialize<WaitResult>(result.Stdout);
        Assert.NotNull(wait);
        Assert.NotNull(wait.WindowHandle);
        Assert.StartsWith("0x", wait.WindowHandle);
    }

    [Fact]
    public async Task Wait_NeitherAidNorTitle_ReturnsError()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --timeout 1000 {_fixture.SessionArg}");

        Assert.NotEqual(0, result.ExitCode);
        var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("--aid", error.Message);
    }
}
