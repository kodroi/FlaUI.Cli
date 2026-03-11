using System.Diagnostics;

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

        // Snapshot existing TestApp PIDs so we only kill the one we spawn
        var existingPids = Process.GetProcessesByName("FlaUI.Cli.TestApp")
            .Select(p => p.Id)
            .ToHashSet();

        var result = await cli.RunAsync(
            $"session new --app \"{testAppPath}\" --wait-title \"NonExistentTitle\" --wait-timeout 2000");

        Assert.NotEqual(0, result.ExitCode);
        var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("Timeout", error.Message);

        // Kill only the newly spawned TestApp process
        try
        {
            foreach (var p in Process.GetProcessesByName("FlaUI.Cli.TestApp"))
            {
                if (!existingPids.Contains(p.Id))
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
