using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

internal static class CommandHelper
{
    internal static Option<string?> CreateWindowOption()
    {
        return new Option<string?>("--window")
        {
            Description = "Window handle (hex string from 'window list') to target instead of the main window"
        };
    }

    internal static AutomationElement ResolveWindow(
        AutomationEngine engine,
        AutomationElement mainWindow,
        string? windowHandle)
    {
        if (string.IsNullOrEmpty(windowHandle))
            return mainWindow;

        var handle = WindowHandleParser.Parse(windowHandle);
        return engine.ResolveWindow(handle);
    }

    internal static AutomationElement? ResolveElement(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        AutomationElement mainWindow,
        string elementId)
    {
        var entry = SessionManager.GetElement(session, elementId);
        if (entry is null) return null;

        if (!TargetContext.Explicit && entry.WindowHandle is { } storedHandle &&
            NativeInterop.BelongsToProcess(new IntPtr(storedHandle), session.Application.Pid))
        {
            var storedWindow = engine.GetWindowByHandle(storedHandle);
            if (storedWindow is not null) mainWindow = storedWindow;
        }

        // Try main window first with short timeout
        var resolver = engine.CreateSelectorResolver();
        var result = ResolveEntry(resolver, mainWindow, entry);
        if (result is not null)
        {
            TargetContext.Set(mainWindow);
            return result.Element;
        }
        if (TargetContext.Explicit) return null;

        // Search all windows
        var mainHandle = mainWindow.Properties.NativeWindowHandle.ValueOrDefault;
        foreach (var window in engine.GetAllTopLevelWindows())
        {
            if (window.Properties.NativeWindowHandle.ValueOrDefault == mainHandle) continue;

            resolver = engine.CreateSelectorResolver();
            result = ResolveEntry(resolver, window, entry);
            if (result is not null)
            {
                TargetContext.Set(window);
                return result.Element;
            }
        }

        return null;
    }

    private static SelectorResult? ResolveEntry(SelectorResolver resolver, AutomationElement window, ElementEntry entry)
    {
        // A stored stable ID must not fall back to an unrelated control with the same class.
        if (!string.IsNullOrEmpty(entry.AutomationId))
            return resolver.Resolve(window, automationId: entry.AutomationId, timeoutMs: 2000);
        if (!string.IsNullOrEmpty(entry.Name))
            return resolver.Resolve(window, name: entry.Name, controlType: entry.ControlType, timeoutMs: 2000);
        return resolver.Resolve(window, controlType: entry.ControlType, className: entry.ClassName, timeoutMs: 2000);
    }

    internal static void RecordStep(
        SessionFile session,
        string command,
        string elementId,
        Dictionary<string, object?>? parameters,
        bool success,
        string? value = null,
        string? error = null)
    {
        if (session.Recording is not { Active: true }) return;

        var entry = session.Elements.GetValueOrDefault(elementId);
        var recordingService = new RecordingService();

        RecordingService.AddStep(session, new RecordingStep
        {
            Command = command,
            Target = entry is not null
                ? new RecordingTarget
                {
                    AutomationId = entry.AutomationId,
                    Name = entry.Name,
                    ControlType = entry.ControlType,
                    SelectorQuality = entry.SelectorQuality,
                    Strategy = entry.SelectorQuality == SelectorQuality.Stable ? "AutomationId" : "Name"
                }
                : null,
            Params = parameters,
            Result = new RecordingStepResult { Success = success, Value = value, Error = error }
        });
    }
}
