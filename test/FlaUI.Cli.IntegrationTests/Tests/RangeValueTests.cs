namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class RangeValueTests : IAsyncLifetime
{
    private readonly TestAppFixture _fixture;

    public RangeValueTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAppStateAsync();
    }

    [Fact]
    public async Task GetRange_Slider_ReturnsMinMaxValue()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestSlider {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var rangeResult = await _fixture.Cli.RunAsync(
            $"elem get-range --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, rangeResult.ExitCode);
        var result = CliRunner.Deserialize<GetRangeResult>(rangeResult.Stdout);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(50, result.Value);
        Assert.Equal(0, result.Minimum);
        Assert.Equal(100, result.Maximum);
    }

    [Fact]
    public async Task SetRange_Slider_ChangesValue()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestSlider {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var setResult = await _fixture.Cli.RunAsync(
            $"elem set-range --id {found.ElementId} --value 75 {_fixture.SessionArg}");
        Assert.Equal(0, setResult.ExitCode);
        var action = CliRunner.Deserialize<ActionResult>(setResult.Stdout);
        Assert.NotNull(action);
        Assert.True(action.Success);

        var rangeResult = await _fixture.Cli.RunAsync(
            $"elem get-range --id {found.ElementId} {_fixture.SessionArg}");
        Assert.Equal(0, rangeResult.ExitCode);
        var result = CliRunner.Deserialize<GetRangeResult>(rangeResult.Stdout);
        Assert.NotNull(result);
        Assert.Equal(75, result.Value);
    }

    [Fact]
    public async Task SetRange_OutOfBounds_ReturnsError()
    {
        var findResult = await _fixture.Cli.RunAsync($"elem find --aid TestSlider {_fixture.SessionArg}");
        Assert.Equal(0, findResult.ExitCode);
        var found = CliRunner.Deserialize<ElementFindResult>(findResult.Stdout);
        Assert.NotNull(found?.ElementId);

        var setResult = await _fixture.Cli.RunAsync(
            $"elem set-range --id {found.ElementId} --value 150 {_fixture.SessionArg}");
        var error = CliRunner.Deserialize<ErrorResult>(setResult.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("out of range", error.Message);
    }
}
