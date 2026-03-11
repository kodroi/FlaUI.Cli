namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class SessionTests
{
    private readonly TestAppFixture _fixture;

    public SessionTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Session_Status_ShowsProcessAlive()
    {
        var result = await _fixture.Cli.RunAsync($"session status {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var status = CliRunner.Deserialize<SessionStatusResult>(result.Stdout);
        Assert.NotNull(status);
        Assert.True(status.Success);
        Assert.True(status.ProcessAlive);
        Assert.True(status.WindowValid);
    }

    [Fact]
    public async Task Session_Attach_ByPid_Works()
    {
        var result = await _fixture.Cli.RunAsync($"session attach --pid {_fixture.AppPid}");

        Assert.Equal(0, result.ExitCode);
        var attached = CliRunner.Deserialize<SessionAttachResult>(result.Stdout);
        Assert.NotNull(attached);
        Assert.True(attached.Success);
        Assert.Equal(_fixture.AppPid, attached.Pid);

        // Clean up the extra session file
        if (attached.SessionFile is not null && File.Exists(attached.SessionFile))
            File.Delete(attached.SessionFile);
    }
}
