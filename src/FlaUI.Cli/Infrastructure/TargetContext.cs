using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;
using FlaUI.Cli.Models;
using System.Drawing;
using System.Text.Json;

namespace FlaUI.Cli.Infrastructure;

internal static class TargetContext
{
    internal static string? Handle { get; private set; }
    internal static string? Title { get; private set; }
    internal static bool Fallback { get; set; }
    internal static bool Explicit { get; set; }
    private static CoordinateContext? _coordinates;

    internal static void Reset()
    {
        Handle = null;
        Title = null;
        Fallback = false;
        Explicit = false;
        _coordinates = null;
    }

    internal static void Set(AutomationElement window)
    {
        Handle = $"0x{window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}";
        Title = window.Properties.Name.ValueOrDefault;
        try
        {
            var bounds = window.BoundingRectangle;
            _coordinates = NativeInterop.ReadCoordinateContext(window.Properties.NativeWindowHandle.ValueOrDefault,
                new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }
        catch (Exception ex)
        {
            _coordinates = new(DpiContext.IsVerified ? "physicalPixels" : "unavailable", DpiContext.IsVerified ? "perMonitorV2" : "unavailable",
                null, null, null, null, null, [], ex.Message);
        }
    }

    internal static void SetCoordinates(AutomationElement element, Rectangle bounds, Point? point = null, string? captureMethod = null)
    {
        var window = Services.AutomationEngine.GetParentWindow(element);
        var handle = window?.Properties.NativeWindowHandle.ValueOrDefault ?? IntPtr.Zero;
        if (window is not null)
        {
            Handle = $"0x{handle.ToInt64():X}";
            Title = window.Properties.Name.ValueOrDefault;
        }
        _coordinates = NativeInterop.ReadCoordinateContext(handle, new(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            point is { } p ? new(p.X, p.Y) : null, captureMethod);
    }

    internal static void AddTo(JsonObject result)
    {
        if (result["errorCode"]?.GetValue<string>() == "invalid_handle") return;
        if (Handle is null) return;
        result["effectiveWindowHandle"] = Handle;
        result["effectiveWindowTitle"] = Title;
        result["targetFallback"] = Fallback;
        if (_coordinates is not null)
            result["coordinateContext"] = JsonSerializer.SerializeToNode(_coordinates, JsonOutput.GetOptions());
    }
}
