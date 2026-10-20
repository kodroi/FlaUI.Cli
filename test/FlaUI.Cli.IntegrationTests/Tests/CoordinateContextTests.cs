using FlaUI.Cli.Infrastructure;
using BoundsInfo = FlaUI.Cli.Models.BoundsInfo;

namespace FlaUI.Cli.IntegrationTests.Tests;

public class CoordinateContextTests
{
    [Fact]
    public void FailedMonitorScaleQuery_DoesNotUseTheApisFallbackValue()
    {
        var monitor = FlaUI.Cli.Models.MonitorContext.FromQuery(new nint(1), new(0, 0, 10, 10), unchecked((int)0x80070057), 100);
        Assert.Null(monitor.ScalePercent);
        Assert.NotNull(monitor.QueryError);
    }

    [Theory]
    [InlineData(-100, -50, true, true)]
    [InlineData(-200, -50, true, false)]
    [InlineData(0, -50, false, true)]
    public void PhysicalBounds_PreserveNegativeOriginsAndSpanningIntersections(int x, int y, bool left, bool right)
    {
        var target = new BoundsInfo(x, y, 150, 100);
        Assert.Equal(left, NativeInterop.Intersects(target, new(-1920, -1080, 1920, 2160)));
        Assert.Equal(right, NativeInterop.Intersects(target, new(0, -1080, 3840, 2160)));
        Assert.Equal(x, target.X);
        Assert.Equal(y, target.Y);
    }

    [Fact]
    public void MetadataFailure_DoesNotInventMonitorScale()
    {
        using var scope = DpiContext.Enter();
        var context = NativeInterop.ReadCoordinateContext(nint.Zero, null);
        Assert.Null(context.WindowDpi);
        Assert.NotNull(context.QueryError);
        Assert.NotEmpty(context.Monitors);
        Assert.All(context.Monitors, monitor => Assert.True(monitor.ScalePercent > 0 || monitor.QueryError is not null));
    }
}
