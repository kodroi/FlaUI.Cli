using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

internal static class CommandHelper
{
    internal static (Option<string?> Id, Option<string?> Aid) AddActionSelectorOptions(Command command)
    {
        var id = new Option<string?>("--id") { Description = "Short element ID returned by 'elem find'" };
        var aid = new Option<string?>("--aid") { Description = "Unique AutomationId in the target window" };
        command.Add(id);
        command.Add(aid);
        command.Validators.Add(result =>
        {
            if ((result.GetResult(id) is null) == (result.GetResult(aid) is null))
                result.AddError("Specify exactly one of --id or --aid.");
            else if (string.IsNullOrWhiteSpace(result.GetValue(id) ?? result.GetValue(aid)))
                result.AddError("The element selector must not be empty.");
        });
        return (id, aid);
    }

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

    internal static (AutomationElement Element, string ElementId)? ResolveActionTarget(
        AutomationEngine engine, SessionManager sessionManager, SessionFile session, AutomationElement targetWindow,
        string? elementId, string? automationId, out ErrorResult? error)
    {
        error = null;
        if (elementId is not null)
        {
            var remembered = ResolveElement(engine, sessionManager, session, targetWindow, elementId);
            if (remembered is not null) return (remembered, elementId);

            error = new ErrorResult(false, $"Element '{elementId}' not found.", "not_found");
            return null;
        }

        var matches = targetWindow.FindAllDescendants(conditions => conditions.ByAutomationId(automationId!));
        if (matches.Length != 1)
        {
            error = matches.Length == 0
                ? new ErrorResult(false, $"Element with AutomationId '{automationId}' not found in the target window.", "not_found")
                : new ErrorResult(false, $"AutomationId '{automationId}' matches multiple elements in the target window.",
                    "ambiguous_selector", Recovery: "Use an AutomationId that is unique in the target window.");
            return null;
        }

        var element = matches[0];
        var windowHandle = targetWindow.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();
        return (element, RememberElement(session, element, windowHandle, SelectorQuality.Stable));
    }

    internal static string RememberElement(SessionFile session, AutomationElement element, long windowHandle, SelectorQuality quality)
    {
        var elementId = Guid.NewGuid().ToString("N")[..8];
        SessionManager.AddElement(session, elementId, new ElementEntry
        {
            AutomationId = element.Properties.AutomationId.ValueOrDefault,
            Name = element.Properties.Name.ValueOrDefault,
            ControlType = element.Properties.ControlType.ValueOrDefault.ToString(),
            ClassName = element.Properties.ClassName.ValueOrDefault,
            RuntimeId = element.Properties.RuntimeId.ValueOrDefault,
            SelectorQuality = quality,
            WindowHandle = windowHandle,
            LastVerified = DateTime.UtcNow
        });
        return elementId;
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
