namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class OwnedWindowTests
{
    private readonly TestAppFixture _fixture;

    public OwnedWindowTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WindowList_ShowsOwnedWindow()
    {
        // Open the About dialog (owned window)
        await _fixture.Cli.RunAsync($"elem menu --path \"Help > About\" {_fixture.SessionArg}");
        await Task.Delay(500);

        try
        {
            var result = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
            Assert.Equal(0, result.ExitCode);

            var windowList = CliRunner.Deserialize<WindowListResult>(result.Stdout);
            Assert.NotNull(windowList?.Windows);

            var aboutWindow = windowList.Windows.FirstOrDefault(w => w.Title == "About");
            Assert.NotNull(aboutWindow);
            Assert.NotNull(aboutWindow.Handle);
        }
        finally
        {
            // Close the About dialog
            await CloseAboutDialog();
        }
    }

    [Fact]
    public async Task Find_ElementInOwnedWindow_WithoutWindowOption()
    {
        // Open the About dialog (owned window)
        await _fixture.Cli.RunAsync($"elem menu --path \"Help > About\" {_fixture.SessionArg}");
        await Task.Delay(500);

        try
        {
            // Find an element in the owned window without specifying --window
            var result = await _fixture.Cli.RunAsync(
                $"elem find --aid AboutTitle --timeout 5000 {_fixture.SessionArg}");

            Assert.Equal(0, result.ExitCode);
            var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
            Assert.NotNull(found);
            Assert.True(found.Success);
            Assert.Equal("AboutTitle", found.AutomationId);
            Assert.NotNull(found.WindowHandle);
        }
        finally
        {
            await CloseAboutDialog();
        }
    }

    [Fact]
    public async Task Find_ElementInOwnedWindow_WithWindowOption()
    {
        // Open the About dialog (owned window)
        await _fixture.Cli.RunAsync($"elem menu --path \"Help > About\" {_fixture.SessionArg}");
        await Task.Delay(500);

        try
        {
            // Get the About window handle
            var listResult = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
            var windowList = CliRunner.Deserialize<WindowListResult>(listResult.Stdout);
            Assert.NotNull(windowList?.Windows);
            var aboutWindow = windowList.Windows.FirstOrDefault(w => w.Title == "About");
            Assert.NotNull(aboutWindow);

            // Find element in specific window
            var result = await _fixture.Cli.RunAsync(
                $"elem find --aid AboutCloseButton --window {aboutWindow.Handle} --timeout 5000 {_fixture.SessionArg}");

            Assert.Equal(0, result.ExitCode);
            var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
            Assert.NotNull(found);
            Assert.True(found.Success);
            Assert.Equal("AboutCloseButton", found.AutomationId);
        }
        finally
        {
            await CloseAboutDialog();
        }
    }

    [Fact]
    public async Task Find_OwnedWindowElement_ByName_WorksWithoutPolicy()
    {
        // Open the About dialog (owned window)
        await _fixture.Cli.RunAsync($"elem menu --path \"Help > About\" {_fixture.SessionArg}");
        await Task.Delay(500);

        try
        {
            // Find element by name in owned window — previously blocked by selector policy
            var result = await _fixture.Cli.RunAsync(
                $"elem find --name \"Close\" --type Button --timeout 5000 {_fixture.SessionArg}");

            Assert.Equal(0, result.ExitCode);
            var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
            Assert.NotNull(found);
            Assert.True(found.Success);
            Assert.Equal(SelectorQuality.Acceptable, found.SelectorQuality);
        }
        finally
        {
            await CloseAboutDialog();
        }
    }

    private async Task CloseAboutDialog()
    {
        try
        {
            var findResult = await _fixture.Cli.RunAsync(
                $"elem find --aid AboutCloseButton --timeout 2000 {_fixture.SessionArg}");
            var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
            if (found?.ElementId is not null)
            {
                await _fixture.Cli.RunAsync($"elem click --id {found.ElementId} {_fixture.SessionArg}");
                await Task.Delay(200);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
