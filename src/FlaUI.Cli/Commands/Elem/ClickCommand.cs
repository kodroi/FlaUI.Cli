using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class ClickCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Short element ID returned by 'elem find' (e.g. \"ec86bb98\")" };
        idOption.Required = true;
        var doubleOption = new Option<bool>("--double") { Description = "Perform a double-click instead of a single click" };
        var rightOption = new Option<bool>("--right") { Description = "Perform a right-click (context menu) instead of a left click" };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("click", "Click an element");
        command.Add(idOption);
        command.Add(doubleOption);
        command.Add(rightOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var doubleClick = parseResult.GetValue(doubleOption);
            var rightClick = parseResult.GetValue(rightOption);
            var windowHandle = parseResult.GetValue(windowOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

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
                    return ExitCodes.Unresolvable;
                }

                AutomationEngine.Click(element, doubleClick, rightClick);

                CommandHelper.RecordStep(session, "elem click", elementId,
                    new Dictionary<string, object?> { ["double"] = doubleClick, ["right"] = rightClick },
                    true);

                sessionManager.Save(sessionPath, session);

                var entry = SessionManager.GetElement(session, elementId);
                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "Clicked.",
                    ElementId: elementId,
                    SelectorQuality: entry?.SelectorQuality));

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                JsonOutput.Write(CommandErrors.FromException(ex));
                return ExitCodes.Error;
            }
        });

        return command;
    }
}
