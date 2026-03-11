using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class ScreenshotTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions BatchJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly TestAppFixture _fixture;
    private readonly List<string> _tempFiles = [];

    public ScreenshotTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { /* best effort */ }
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Screenshot_MainWindow_SavesPng()
    {
        var output = CreateTempPath("png");

        var result = await _fixture.Cli.RunAsync(
            $"screenshot --output \"{output}\" {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var screenshot = CliRunner.Deserialize<ScreenshotResult>(result.Stdout);
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Success);
        Assert.Equal("Screenshot saved.", screenshot.Message);
        Assert.NotNull(screenshot.OutputPath);
        Assert.True(File.Exists(screenshot.OutputPath));
        Assert.True(screenshot.Width > 0);
        Assert.True(screenshot.Height > 0);
    }

    [Fact]
    public async Task Screenshot_Element_SavesPng()
    {
        // Find an element first
        var findResult = await _fixture.Cli.RunAsync(
            $"elem find --aid SubmitButton {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var output = CreateTempPath("png");

        var result = await _fixture.Cli.RunAsync(
            $"screenshot --id {found.ElementId} --output \"{output}\" {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var screenshot = CliRunner.Deserialize<ScreenshotResult>(result.Stdout);
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Success);
        Assert.True(File.Exists(screenshot.OutputPath));
        Assert.True(screenshot.Width > 0);
        Assert.True(screenshot.Height > 0);
    }

    [Fact]
    public async Task Screenshot_InBatch_Succeeds()
    {
        var output = CreateTempPath("png");

        var batchFile = WriteBatchFile(new
        {
            steps = new object[]
            {
                new { cmd = "screenshot", args = new Dictionary<string, string> { ["output"] = output } }
            }
        });

        var result = await _fixture.Cli.RunAsync(
            $"batch --file \"{batchFile}\" {_fixture.SessionArg}");

        var batch = CliRunner.Deserialize<BatchResult>(result.Stdout);
        Assert.NotNull(batch);
        Assert.True(batch.Success);
        Assert.Equal(1, batch.Succeeded);
        Assert.True(File.Exists(output));
    }

    private string CreateTempPath(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"flaui-screenshot-{Guid.NewGuid():N}.{extension}");
        _tempFiles.Add(path);
        return path;
    }

    private string WriteBatchFile(object content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"flaui-batch-{Guid.NewGuid():N}.json");
        var json = JsonSerializer.Serialize(content, BatchJsonOptions);
        File.WriteAllText(path, json);
        _tempFiles.Add(path);
        return path;
    }
}
