using FlaUI.Cli.IntegrationTests.Infrastructure;
namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class WaitTests
{
    private readonly TestAppFixture _fixture;

    public WaitTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Wait_ForVisibleElement_Succeeds()
    {
        var result = await _fixture.Cli.RunAsync(
            $"wait --aid StatusLabel --state visible --timeout 5000 {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var wait = CliRunner.Deserialize<WaitResult>(result.Stdout);
        Assert.NotNull(wait);
        Assert.True(wait.Success);
        Assert.True(wait.Elapsed < 5000);
    }
}
