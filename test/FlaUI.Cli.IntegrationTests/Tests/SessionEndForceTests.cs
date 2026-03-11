namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class SessionEndForceTests
{
    private readonly TestAppFixture _fixture;

    public SessionEndForceTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SessionEnd_HelpShowsForceOption()
    {
        var result = await _fixture.Cli.RunAsync("session end --help");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--force", result.Stdout);
    }
}
