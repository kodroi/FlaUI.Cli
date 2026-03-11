using FlaUI.Cli.IntegrationTests.Infrastructure;
namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class AuditTests
{
    private readonly TestAppFixture _fixture;

    public AuditTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Audit_ReportsElementCounts_WithHighStableRatio()
    {
        var result = await _fixture.Cli.RunAsync($"audit {_fixture.SessionArg}");

        Assert.Equal(0, result.ExitCode);
        var audit = CliRunner.Deserialize<AuditResult>(result.Stdout);
        Assert.NotNull(audit);
        Assert.True(audit.Success);
        Assert.True(audit.TotalElements > 0);
        Assert.True(audit.WithAutomationId > 0);
        Assert.NotNull(audit.SelectorDistribution);
        Assert.True(audit.SelectorDistribution.ContainsKey("stable"));

        // Our test app has AutomationIds on all interactive elements,
        // so the stable ratio should be significant
        var stableCount = audit.SelectorDistribution["stable"];
        Assert.True(stableCount >= 8, $"Expected at least 8 stable elements, got {stableCount}");
    }
}
