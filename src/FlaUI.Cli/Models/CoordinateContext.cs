namespace FlaUI.Cli.Models;

internal record CoordinateContext(string CoordinateSpace, string ClientAwareness, BoundsInfo? Bounds,
    PointInfo? InputPoint, string? CaptureMethod, uint? WindowDpi, string? WindowAwareness,
    IReadOnlyList<MonitorContext> Monitors, string? QueryError = null);
internal record PointInfo(int X, int Y);
internal record MonitorContext(string Handle, BoundsInfo Bounds, int? ScalePercent, string? QueryError = null)
{
    internal static MonitorContext FromQuery(IntPtr handle, BoundsInfo bounds, int status, int scale) =>
        new($"0x{handle.ToInt64():X}", bounds, status == 0 && scale > 0 ? scale : null,
            status != 0 ? $"Monitor scale query failed (0x{status:X8})." : scale <= 0 ? "Invalid monitor scale." : null);
}
