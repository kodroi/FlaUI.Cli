using System.CommandLine;
using FlaUI.Core.Definitions;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class SetDockCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID" };
        idOption.Required = true;
        var positionOption = new Option<string>("--position") { Description = "Dock position: top, bottom, left, right, fill, none" };
        positionOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("set-dock", "Set an element's dock position via the Dock pattern");
        command.Add(idOption);
        command.Add(positionOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var positionStr = parseResult.GetValue(positionOption)!;
            var windowHandle = parseResult.GetValue(windowOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            if (!Enum.TryParse<DockPosition>(positionStr, ignoreCase: true, out var position))
            {
                JsonOutput.Write(new ErrorResult(false, $"Invalid dock position '{positionStr}'. Use: top, bottom, left, right, fill, none."));
                Environment.ExitCode = ExitCodes.Error;
                return;
            }

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var targetWindow = CommandHelper.ResolveWindow(engine, mainWindow, windowHandle);
                var element = CommandHelper.ResolveElement(engine, sessionManager, session, targetWindow, elementId);
                if (element is null)
                {
                    JsonOutput.Write(new ErrorResult(false, $"Element '{elementId}' not found."));
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                AutomationEngine.SetDockPosition(element, position);

                CommandHelper.RecordStep(session, "elem set-dock", elementId,
                    new Dictionary<string, object?> { ["position"] = positionStr }, true);
                sessionManager.Save(sessionPath, session);

                var entry = SessionManager.GetElement(session, elementId);
                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: $"Dock position set to {position}.",
                    ElementId: elementId,
                    SelectorQuality: entry?.SelectorQuality));

                Environment.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }
}
