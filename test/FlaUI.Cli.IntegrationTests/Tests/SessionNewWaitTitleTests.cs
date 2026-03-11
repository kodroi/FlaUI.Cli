namespace FlaUI.Cli.IntegrationTests.Tests;

public class SessionNewWaitTitleTests
{
    [Fact]
    public async Task SessionNew_WaitTitle_MatchesWindowByTitle()
    {
        var solutionRoot = SolutionLocator.FindSolutionRoot();
        var cliPath = SolutionLocator.GetCliPath(solutionRoot);
        var testAppPath = SolutionLocator.GetTestAppPath(solutionRoot);
        var cli = new CliRunner(cliPath);

        string? sessionPath = null;

        try
        {
            var result = await cli.RunAsync(
                $"session new --app \"{testAppPath}\" --wait-title \"Contact Form\" --wait-timeout 15000");

            Assert.Equal(0, result.ExitCode);
            var session = CliRunner.Deserialize<SessionNewResult>(result.Stdout);
            Assert.NotNull(session);
            Assert.True(session.Success);
            Assert.Contains("Contact Form", session.MainWindowTitle);
            sessionPath = session.SessionFile;
        }
        finally
        {
            if (sessionPath is not null)
            {
                await cli.RunAsync($"session end --close-app --session \"{sessionPath}\"");
            }
        }
    }

    [Fact]
    public async Task SessionNew_WaitTitle_TimesOutOnWrongTitle()
    {
        var solutionRoot = SolutionLocator.FindSolutionRoot();
        var cliPath = SolutionLocator.GetCliPath(solutionRoot);
        var testAppPath = SolutionLocator.GetTestAppPath(solutionRoot);
        var cli = new CliRunner(cliPath);

        // Use a very short timeout to avoid long waits
        var result = await cli.RunAsync(
            $"session new --app \"{testAppPath}\" --wait-title \"NonExistentTitle\" --wait-timeout 2000");

        Assert.NotEqual(0, result.ExitCode);
        var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("Timeout", error.Message);

        // The app was launched but session creation failed — kill it by process name
        // (best-effort cleanup)
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("FlaUI.Cli.TestApp");
            foreach (var p in processes)
            {
                p.Kill();
                p.Dispose();
            }
        }
        catch
        {
            // best effort
        }
    }
}
