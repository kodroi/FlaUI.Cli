using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.IntegrationTests.Tests;

public class DpiContextTests
{
    [Fact]
    public void NestedScopes_RestoreEachPreviousContext_WhenAnOperationThrows()
    {
        nint native = new(123);
        nint Set(nint next) { var previous = native; native = next; return previous; }
        using (var outer = new DpiContext(Set, () => native == DpiContext.PerMonitorV2))
        {
            Assert.True(DpiContext.IsVerified);
            Assert.Throws<InvalidOperationException>((Action)(() =>
            {
                using var inner = new DpiContext(Set, () => native == DpiContext.PerMonitorV2);
                DpiContext.RequireCoordinates();
                throw new InvalidOperationException("operation failed");
            }));
            Assert.True(DpiContext.IsVerified);
        }
        Assert.Equal(new nint(123), native);
        Assert.False(DpiContext.IsVerified);
    }

    [Fact]
    public void UnavailableScope_BlocksCaptureBeforeCreatingOrReplacingOutput()
    {
        var output = Path.Combine(Path.GetTempPath(), $"dpi-guard-{Guid.NewGuid():N}.png");
        File.WriteAllText(output, "keep existing output");
        try
        {
            using var scope = new DpiContext(_ => nint.Zero, () => false);
            var error = Assert.Throws<DpiUnavailableException>(() => AutomationEngine.CaptureScreenshot(null!, output));
            var result = CommandErrors.FromException(error);
            Assert.False(result.Success);
            Assert.Equal("dpi_unavailable", result.ErrorCode);
            Assert.NotNull(result.Recovery);
            Assert.Equal("keep existing output", File.ReadAllText(output));
        }
        finally { File.Delete(output); }
    }

    [Fact]
    public void LostContext_IsRecheckedBeforeCoordinateWork()
    {
        var valid = true;
        using var scope = new DpiContext(_ => new nint(123), () => valid);
        DpiContext.RequireCoordinates();
        valid = false;
        Assert.Throws<DpiUnavailableException>(DpiContext.RequireCoordinates);
    }

    [Fact]
    public void NativeScope_VerifiesAndRestoresTheExecutingThread()
    {
        using var outer = DpiContext.Enter();
        Assert.True(DpiContext.IsVerified);
        using (var unavailable = new DpiContext(_ => nint.Zero, () => false))
            Assert.False(DpiContext.IsVerified);
        Assert.True(DpiContext.IsVerified);
    }
}
