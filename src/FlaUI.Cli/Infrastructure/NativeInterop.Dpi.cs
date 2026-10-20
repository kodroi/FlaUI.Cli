using System.ComponentModel;
using System.Drawing;
using FlaUI.Cli.Models;
using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

public static partial class NativeInterop
{
    private delegate bool MonitorProc(IntPtr monitor, IntPtr dc, ref NativeRect rect, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorProc callback, IntPtr data);
    [DllImport("shcore.dll")]
    private static extern int GetScaleFactorForMonitor(IntPtr monitor, out int scale);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll")]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPhysicalCursorPos(out Point point);

    internal static CoordinateContext ReadCoordinateContext(IntPtr window, BoundsInfo? bounds,
        PointInfo? point = null, string? captureMethod = null)
    {
        if (!DpiContext.IsVerified)
            return new("unavailable", "unavailable", null, null, captureMethod, null, null, [],
                "The client thread's DPI context is unavailable.");
        uint? dpi = null;
        string? awareness = null;
        string? error = null;
        var monitors = new List<MonitorContext>();
        try
        {
            var value = GetDpiForWindow(window);
            if (value != 0) dpi = value;
            else error = "Window DPI unavailable.";
            var context = GetWindowDpiAwarenessContext(window);
            awareness = AreDpiAwarenessContextsEqual(context, DpiContext.PerMonitorV2) ? "perMonitorV2"
                : GetAwarenessFromDpiAwarenessContext(context) switch
                { 0 => "unaware", 1 => "system", 2 => "perMonitor", _ => "unknown" };
            if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Visit, IntPtr.Zero))
                error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        { error = ex.Message; }
        return new("physicalPixels", "perMonitorV2", bounds, point, captureMethod, dpi, awareness, monitors, error);

        bool Visit(IntPtr monitor, IntPtr dc, ref NativeRect rect, IntPtr data)
        {
            var monitorBounds = new BoundsInfo(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            if (bounds is not null && !Intersects(bounds, monitorBounds)) return true;
            monitors.Add(ReadMonitorContext(monitor, monitorBounds));
            return true;
        }
    }

    internal static MonitorContext ReadMonitorContext(IntPtr monitor, BoundsInfo bounds)
    {
        try
        {
            var status = GetScaleFactorForMonitor(monitor, out var scale);
            return MonitorContext.FromQuery(monitor, bounds, status, scale);
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        { return new($"0x{monitor.ToInt64():X}", bounds, null, ex.Message); }
    }

    internal static bool Intersects(BoundsInfo first, BoundsInfo second) =>
        first.X < second.X + second.Width && first.X + first.Width > second.X &&
        first.Y < second.Y + second.Height && first.Y + first.Height > second.Y;
}
