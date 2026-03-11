using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class BatchTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions BatchJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly TestAppFixture _fixture;
    private readonly List<string> _tempFiles = [];

    public BatchTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task Batch_FindAndClick_Succeeds()
    {
        var batchFile = WriteBatchFile(new
        {
            steps = new object[]
            {
                new { cmd = "elem find", args = new Dictionary<string, string> { ["aid"] = "SubmitButton" } },
                new { cmd = "elem click", args = new Dictionary<string, string> { ["id"] = "$prev.elementId" } }
            }
        });

        var result = await _fixture.Cli.RunAsync(
            $"batch --file \"{batchFile}\" {_fixture.SessionArg}");

        var batch = CliRunner.Deserialize<BatchResult>(result.Stdout);
        Assert.NotNull(batch);
        Assert.True(batch.Success);
        Assert.Equal(2, batch.TotalSteps);
        Assert.Equal(2, batch.Succeeded);
        Assert.Equal(0, batch.Failed);
    }

    [Fact]
    public async Task Batch_InvalidCommand_FailsAndStops()
    {
        var batchFile = WriteBatchFile(new
        {
            steps = new object[]
            {
                new { cmd = "nonexistent cmd", args = new Dictionary<string, string>() },
                new { cmd = "elem find", args = new Dictionary<string, string> { ["aid"] = "SubmitButton" } }
            }
        });

        var result = await _fixture.Cli.RunAsync(
            $"batch --file \"{batchFile}\" {_fixture.SessionArg}");

        var batch = CliRunner.Deserialize<BatchResult>(result.Stdout);
        Assert.NotNull(batch);
        Assert.False(batch.Success);
        Assert.Equal(1, batch.TotalSteps);
        Assert.Equal(0, batch.Succeeded);
        Assert.Equal(1, batch.Failed);
    }

    [Fact]
    public async Task Batch_ContinueOnError_ExecutesAllSteps()
    {
        var batchFile = WriteBatchFile(new
        {
            steps = new object[]
            {
                new { cmd = "nonexistent cmd", args = new Dictionary<string, string>() },
                new { cmd = "elem find", args = new Dictionary<string, string> { ["aid"] = "SubmitButton" } }
            }
        });

        var result = await _fixture.Cli.RunAsync(
            $"batch --file \"{batchFile}\" --continue-on-error {_fixture.SessionArg}");

        var batch = CliRunner.Deserialize<BatchResult>(result.Stdout);
        Assert.NotNull(batch);
        Assert.Equal(2, batch.TotalSteps);
        Assert.Equal(1, batch.Succeeded);
        Assert.Equal(1, batch.Failed);
    }

    [Fact]
    public async Task Batch_TypeAndGetValue_WithPrevReference()
    {
        var batchFile = WriteBatchFile(new
        {
            steps = new object[]
            {
                new { cmd = "elem find", args = new Dictionary<string, string> { ["aid"] = "FirstNameInput" } },
                new { cmd = "elem type", args = new Dictionary<string, string> { ["id"] = "$prev.elementId", ["text"] = "BatchTest" } },
                new { cmd = "elem get-value", args = new Dictionary<string, string> { ["id"] = "$steps[0].elementId" } }
            }
        });

        var result = await _fixture.Cli.RunAsync(
            $"batch --file \"{batchFile}\" {_fixture.SessionArg}");

        var batch = CliRunner.Deserialize<BatchResult>(result.Stdout);
        Assert.NotNull(batch);
        Assert.True(batch.Success);
        Assert.Equal(3, batch.TotalSteps);
        Assert.Equal(3, batch.Succeeded);
    }

    [Fact]
    public async Task Batch_WindowList_InBatch()
    {
        var batchFile = WriteBatchFile(new
        {
            steps = new object[]
            {
                new { cmd = "window list", args = new Dictionary<string, string>() }
            }
        });

        var result = await _fixture.Cli.RunAsync(
            $"batch --file \"{batchFile}\" {_fixture.SessionArg}");

        var batch = CliRunner.Deserialize<BatchResult>(result.Stdout);
        Assert.NotNull(batch);
        Assert.True(batch.Success);
        Assert.Equal(1, batch.Succeeded);
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
