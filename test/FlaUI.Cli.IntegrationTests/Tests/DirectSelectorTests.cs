using System.Text.Json;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class DirectSelectorTests(TestAppFixture fixture)
{
    [Fact]
    public async Task DirectSelectors_ClickReadAndReturnAReusableElementId()
    {
        await fixture.ResetAppStateAsync();

        try
        {
            var found = await fixture.Cli.RunAsync($"elem find --aid StatusLabel {fixture.SessionArg}");
            var status = CliRunner.Deserialize<ElementFindResult>(found.Stdout);
            Assert.NotNull(status?.WindowHandle);

            var click = await fixture.Cli.RunAsync($"elem click --aid SubmitButton --window {status.WindowHandle} {fixture.SessionArg}");
            Assert.Equal(0, click.ExitCode);
            var action = CliRunner.Deserialize<ActionResult>(click.Stdout);
            Assert.NotNull(action);
            Assert.True(action.Success);
            Assert.NotNull(action.ElementId);

            var read = await fixture.Cli.RunAsync($"elem get-value --aid StatusLabel --save directStatus {fixture.SessionArg}");
            Assert.Equal(0, read.ExitCode);
            var value = CliRunner.Deserialize<GetValueResult>(read.Stdout);
            Assert.NotNull(value?.ElementId);
            Assert.Equal("Error: Required fields missing", value.Value);
            Assert.Equal(value.Value, new SessionManager().Load(fixture.SessionPath).Variables["directStatus"]);

            var readById = await fixture.Cli.RunAsync($"elem get-value --id {value.ElementId} {fixture.SessionArg}");
            Assert.Equal(0, readById.ExitCode);
            Assert.Equal(value.Value, CliRunner.Deserialize<GetValueResult>(readById.Stdout)?.Value);
        }
        finally
        {
            await fixture.ResetAppStateAsync();
        }
    }

    [Fact]
    public async Task DirectClick_WithDuplicateAutomationId_DoesNotInvokeEitherButton()
    {
        var root = SolutionLocator.FindSolutionRoot();
        var app = SolutionLocator.GetTestAppPath(root);
        var sessionPath = Path.Combine(Path.GetTempPath(), $"flaui-duplicate-{Guid.NewGuid():N}.session.json");
        var sessionArg = $"--session \"{sessionPath}\"";

        try
        {
            var launch = await fixture.Cli.RunAsync($"session new --app \"{app}\" --args flaui-fixture-duplicate-aid {sessionArg}");
            Assert.Equal(0, launch.ExitCode);
            var menu = await fixture.Cli.RunAsync($"elem menu --path \"Edit > Copy\" {sessionArg}");
            Assert.Equal(0, menu.ExitCode);
            var find = await fixture.Cli.RunAsync($"elem find --aid StatusLabel {sessionArg}");
            var status = CliRunner.Deserialize<ElementFindResult>(find.Stdout);
            Assert.NotNull(status?.ElementId);

            var click = await fixture.Cli.RunAsync($"elem click --aid DuplicateAction {sessionArg}");
            Assert.Equal(3, click.ExitCode);
            using var json = JsonDocument.Parse(click.Stdout);
            Assert.Equal("ambiguous_selector", json.RootElement.GetProperty("errorCode").GetString());

            var read = await fixture.Cli.RunAsync($"elem get-value --id {status.ElementId} {sessionArg}");
            Assert.Equal(0, read.ExitCode);
            Assert.Equal("Menu: Copy", CliRunner.Deserialize<GetValueResult>(read.Stdout)?.Value);
        }
        finally
        {
            if (File.Exists(sessionPath))
                await fixture.Cli.RunAsync($"session end --close-app --force {sessionArg}");
            File.Delete(sessionPath);
        }
    }
}
