using FlaUI.Cli.IntegrationTests.Infrastructure;
namespace FlaUI.Cli.IntegrationTests;

public sealed class TestAppFixture : IAsyncLifetime
{
    public CliRunner Cli { get; private set; } = null!;
    public string SessionPath { get; private set; } = string.Empty;
    public int AppPid { get; private set; }

    private string _solutionRoot = string.Empty;

    public async Task InitializeAsync()
    {
        Log("=== TestAppFixture.InitializeAsync START ===");

        _solutionRoot = SolutionLocator.FindSolutionRoot();
        Log($"Solution root: {_solutionRoot}");

        var skipReason = SolutionLocator.GetSkipReason(_solutionRoot);
        if (skipReason is not null)
            throw new InvalidOperationException(skipReason);

        var cliPath = SolutionLocator.GetCliPath(_solutionRoot);
        var testAppPath = SolutionLocator.GetTestAppPath(_solutionRoot);
        Log($"CLI path: {cliPath} (exists={File.Exists(cliPath)})");
        Log($"TestApp path: {testAppPath} (exists={File.Exists(testAppPath)})");

        Cli = new CliRunner(cliPath);

        Log("Launching session new...");
        var result = await Cli.RunAsync($"session new --app \"{testAppPath}\"");
        Log($"session new exit={result.ExitCode}");
        Log($"session new stdout={result.Stdout}");
        if (!string.IsNullOrEmpty(result.Stderr))
            Log($"session new stderr={result.Stderr}");

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"Failed to create session (exit {result.ExitCode}): {result.Stdout} {result.Stderr}");

        var session = CliRunner.Deserialize<SessionNewResult>(result.Stdout);
        SessionPath = session?.SessionFile
            ?? throw new InvalidOperationException("Session file path not returned.");
        AppPid = session.Pid;

        Log($"Session created: path={SessionPath}, pid={AppPid}");
        Log("=== TestAppFixture.InitializeAsync DONE ===");
    }

    public async Task DisposeAsync()
    {
        Log("=== TestAppFixture.DisposeAsync START ===");
        if (!string.IsNullOrEmpty(SessionPath))
        {
            try
            {
                await Cli.RunAsync($"session end --close-app --session \"{SessionPath}\"");
            }
            catch (Exception ex)
            {
                Log($"Dispose cleanup error: {ex.Message}");
            }
        }
        Log("=== TestAppFixture.DisposeAsync DONE ===");
    }

    public string SessionArg => $"--session \"{SessionPath}\"";

    public async Task ResetAppStateAsync()
    {
        Log("ResetAppState: finding ClearButton...");
        var findResult = await Cli.RunAsync($"elem find --aid ClearButton {SessionArg}");
        if (findResult.ExitCode != 0)
        {
            Log($"ResetAppState: ClearButton not found (exit={findResult.ExitCode})");
            return;
        }

        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        if (found?.ElementId is null)
        {
            Log("ResetAppState: ClearButton elementId is null");
            return;
        }

        Log($"ResetAppState: clicking ClearButton (id={found.ElementId})...");
        await Cli.RunAsync($"elem click --id {found.ElementId} {SessionArg}");
        await Task.Delay(200);
        Log("ResetAppState: done");
    }

    private static void Log(string message)
    {
        Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}

[CollectionDefinition("TestApp")]
public class TestAppCollection : ICollectionFixture<TestAppFixture>;
