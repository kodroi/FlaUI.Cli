namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class SessionStatusTitleTests
{
    private readonly TestAppFixture _fixture;

    public SessionStatusTitleTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SessionStatus_ReturnsMainWindowTitle()
    {
        var result = await _fixture.Cli.RunAsync($"session status {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var status = CliRunner.Deserialize<SessionStatusResult>(result.Stdout);
        Assert.NotNull(status);
        Assert.True(status.Success);
        Assert.NotNull(status.MainWindowTitle);
        Assert.Contains("Contact Form", status.MainWindowTitle);
    }

    [Fact]
    public async Task SessionStatus_ReturnsMainWindowHandle()
    {
        var result = await _fixture.Cli.RunAsync($"session status {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var status = CliRunner.Deserialize<SessionStatusResult>(result.Stdout);
        Assert.NotNull(status);
        Assert.True(status.Success);
        Assert.NotNull(status.MainWindowHandle);
        Assert.StartsWith("0x", status.MainWindowHandle);
    }
}
