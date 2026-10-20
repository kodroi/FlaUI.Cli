using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;

namespace FlaUI.Cli.Infrastructure;

internal static class TargetContext
{
    internal static string? Handle { get; private set; }
    internal static string? Title { get; private set; }
    internal static bool Fallback { get; set; }
    internal static bool Explicit { get; set; }

    internal static void Reset()
    {
        Handle = null;
        Title = null;
        Fallback = false;
        Explicit = false;
    }

    internal static void Set(AutomationElement window)
    {
        Handle = $"0x{window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}";
        Title = window.Properties.Name.ValueOrDefault;
    }

    internal static void AddTo(JsonObject result)
    {
        if (result["errorCode"]?.GetValue<string>() == "invalid_handle") return;
        if (Handle is null) return;
        result["effectiveWindowHandle"] = Handle;
        result["effectiveWindowTitle"] = Title;
        result["targetFallback"] = Fallback;
    }
}
