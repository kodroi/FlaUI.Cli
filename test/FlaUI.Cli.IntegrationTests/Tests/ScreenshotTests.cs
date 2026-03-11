using System.Drawing;
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

    [Fact]
    public async Task Screenshot_SameElement_PixelsMatch()
    {
        // Find the Submit button
        var findResult = await _fixture.Cli.RunAsync(
            $"elem find --aid SubmitButton {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        // Take two screenshots of the same element
        var output1 = CreateTempPath("png");
        var output2 = CreateTempPath("png");

        var result1 = await _fixture.Cli.RunAsync(
            $"screenshot --id {found.ElementId} --output \"{output1}\" {_fixture.SessionArg}");
        Assert.Equal(0, result1.ExitCode);
        var screenshot1 = CliRunner.Deserialize<ScreenshotResult>(result1.Stdout);

        var result2 = await _fixture.Cli.RunAsync(
            $"screenshot --id {found.ElementId} --output \"{output2}\" {_fixture.SessionArg}");
        Assert.Equal(0, result2.ExitCode);
        var screenshot2 = CliRunner.Deserialize<ScreenshotResult>(result2.Stdout);

        Assert.NotNull(screenshot1);
        Assert.NotNull(screenshot2);

        // Dimensions must match
        Assert.Equal(screenshot1.Width, screenshot2.Width);
        Assert.Equal(screenshot1.Height, screenshot2.Height);

        // Pixel-level comparison
        using var bmp1 = new Bitmap(output1);
        using var bmp2 = new Bitmap(output2);

        Assert.Equal(bmp1.Width, bmp2.Width);
        Assert.Equal(bmp1.Height, bmp2.Height);

        var totalPixels = bmp1.Width * bmp1.Height;
        var mismatchCount = 0;

        for (var y = 0; y < bmp1.Height; y++)
        {
            for (var x = 0; x < bmp1.Width; x++)
            {
                var pixel1 = bmp1.GetPixel(x, y);
                var pixel2 = bmp2.GetPixel(x, y);
                if (pixel1 != pixel2)
                    mismatchCount++;
            }
        }

        var matchPercentage = (double)(totalPixels - mismatchCount) / totalPixels * 100;
        Assert.True(matchPercentage >= 99.0,
            $"Pixel match {matchPercentage:F1}% is below 99% threshold ({mismatchCount}/{totalPixels} pixels differ)");
    }

    [Fact]
    public async Task Screenshot_DifferentElements_PixelsDiffer()
    {
        // Find two different elements
        var findButton = await _fixture.Cli.RunAsync(
            $"elem find --aid SubmitButton {_fixture.SessionArg}");
        Assert.Equal(0, findButton.ExitCode);
        var button = CliRunner.Deserialize<ElementFindResult>(findButton.Stdout);

        var findInput = await _fixture.Cli.RunAsync(
            $"elem find --aid FirstNameInput {_fixture.SessionArg}");
        Assert.Equal(0, findInput.ExitCode);
        var input = CliRunner.Deserialize<ElementFindResult>(findInput.Stdout);

        Assert.NotNull(button?.ElementId);
        Assert.NotNull(input?.ElementId);

        var output1 = CreateTempPath("png");
        var output2 = CreateTempPath("png");

        await _fixture.Cli.RunAsync(
            $"screenshot --id {button.ElementId} --output \"{output1}\" {_fixture.SessionArg}");
        await _fixture.Cli.RunAsync(
            $"screenshot --id {input.ElementId} --output \"{output2}\" {_fixture.SessionArg}");

        Assert.True(File.Exists(output1));
        Assert.True(File.Exists(output2));

        using var bmp1 = new Bitmap(output1);
        using var bmp2 = new Bitmap(output2);

        // Different elements should have different dimensions or different pixels
        var dimensionsDiffer = bmp1.Width != bmp2.Width || bmp1.Height != bmp2.Height;

        if (!dimensionsDiffer)
        {
            // Same dimensions — check pixel content differs
            var mismatchCount = 0;
            for (var y = 0; y < bmp1.Height; y++)
            {
                for (var x = 0; x < bmp1.Width; x++)
                {
                    if (bmp1.GetPixel(x, y) != bmp2.GetPixel(x, y))
                        mismatchCount++;
                }
            }

            Assert.True(mismatchCount > 0,
                "Screenshots of different elements should have different pixels");
        }
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
