using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

public class CommandFailureTests
{
    [Theory]
    [InlineData("session status --session missing-dependability.session.json", "session_error")]
    [InlineData("session status --session timeout.session.json", "session_error")]
    [InlineData("elem scroll --id missing --vertical nonsense", "invalid_arguments")]
    [InlineData("screenshot --unknown-option", "invalid_arguments")]
    public async Task Failure_HasNonzeroExitAndStructuredJson(string arguments, string code)
    {
        var cli = new CliRunner(SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot()));
        var result = await cli.RunAsync(arguments);
        Assert.NotEqual(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(code, json.RootElement.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("command").GetString()));
    }
}
