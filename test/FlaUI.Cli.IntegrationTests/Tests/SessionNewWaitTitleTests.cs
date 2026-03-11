namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class SessionNewWaitTitleTests
{
    private readonly TestAppFixture _fixture;

    public SessionNewWaitTitleTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SessionNew_WaitTitle_MatchesWindowByTitle()
    {
        var solutionRoot = SolutionLocator.FindSolutionRoot();
        var testAppPath = SolutionLocator.GetTestAppPath(solutionRoot);
        var sessionPath = Path.Combine(Path.GetTempPath(), $"flaui-wait-title-{Guid.NewGuid():N}.session.json");

        try
        {
            var result = await _fixture.Cli.RunAsync(
                $"session new --app \"{testAppPath}\" --wait-title \"Contact Form\" --wait-timeout 15000 --session \"{sessionPath}\"");

            Assert.Equal(0, result.ExitCode);
            var session = CliRunner.Deserialize<SessionNewResult>(result.Stdout);
            Assert.NotNull(session);
            Assert.True(session.Success);
            Assert.Contains("Contact Form", session.MainWindowTitle);
        }
        finally
        {
            if (File.Exists(sessionPath))
            {
                await _fixture.Cli.RunAsync($"session end --close-app --session \"{sessionPath}\"");
                try { File.Delete(sessionPath); } catch { /* best effort */ }
            }
        }
    }

    [Fact]
    public async Task SessionNew_WaitTitle_TimesOutOnWrongTitle()
    {
        var solutionRoot = SolutionLocator.FindSolutionRoot();
        var testAppPath = SolutionLocator.GetTestAppPath(solutionRoot);
        var sessionPath = Path.Combine(Path.GetTempPath(), $"flaui-wait-title-{Guid.NewGuid():N}.session.json");

        try
        {
            var result = await _fixture.Cli.RunAsync(
                $"session new --app \"{testAppPath}\" --wait-title \"NonExistentTitle99\" --wait-timeout 2000 --session \"{sessionPath}\"",
                timeoutMs: 15000);

            var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
            Assert.NotNull(error);
            Assert.False(error.Success);
            Assert.Contains("Timeout", error.Message);
        }
        finally
        {
            if (File.Exists(sessionPath))
            {
                await _fixture.Cli.RunAsync($"session end --close-app --session \"{sessionPath}\"");
                try { File.Delete(sessionPath); } catch { /* best effort */ }
            }
        }
    }
}
