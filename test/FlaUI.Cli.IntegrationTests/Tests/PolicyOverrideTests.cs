namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class PolicyOverrideTests
{
    private readonly TestAppFixture _fixture;

    public PolicyOverrideTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Find_WithPolicyAcceptable_AllowsStableSelector()
    {
        var result = await _fixture.Cli.RunAsync(
            $"elem find --aid SubmitButton --policy acceptable {_fixture.SessionArg}");
        Assert.Equal(0, result.ExitCode);

        var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.True(found.Success);
        Assert.Equal(SelectorQuality.Stable, found.SelectorQuality);
    }

    [Fact]
    public async Task Find_WithPolicyStable_ByName_ReturnsViolation()
    {
        // Finding by name gives "acceptable" quality, which violates "stable" policy
        var result = await _fixture.Cli.RunAsync(
            $"elem find --name \"Submit\" --policy stable {_fixture.SessionArg}");

        var error = CliRunner.Deserialize<ErrorResult>(result.Stdout);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("violates policy", error.Message);
    }

    [Fact]
    public async Task Find_WithPolicyFragile_AllowsAcceptableSelector()
    {
        // Finding by name gives "acceptable" quality, which is fine under "fragile" policy
        var result = await _fixture.Cli.RunAsync(
            $"elem find --name \"Submit\" --policy fragile {_fixture.SessionArg}");

        var found = CliRunner.Deserialize<ElementFindResult>(result.Stdout);
        Assert.NotNull(found);
        Assert.True(found.Success);
    }
}
