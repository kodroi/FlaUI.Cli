using FlaUI.Core.AutomationElements;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

internal static class CommandHelper
{
    internal static AutomationElement? ResolveElement(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        AutomationElement mainWindow,
        string elementId)
    {
        var entry = sessionManager.GetElement(session, elementId);
        if (entry is null) return null;

        var resolver = engine.CreateSelectorResolver();
        var result = resolver.Resolve(mainWindow, entry.AutomationId, entry.Name,
            entry.ControlType, entry.ClassName, 5000);

        return result?.Element;
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

        recordingService.AddStep(session, new RecordingStep
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
