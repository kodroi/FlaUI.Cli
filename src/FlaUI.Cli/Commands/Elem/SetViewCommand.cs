using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class SetViewCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID" };
        idOption.Required = true;
        var viewOption = new Option<int>("--view") { Description = "View ID to set (from get-views)" };
        viewOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("set-view", "Set the current view via the MultipleView pattern");
        command.Add(idOption);
        command.Add(viewOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var viewId = parseResult.GetValue(viewOption);
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
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                AutomationEngine.SetMultipleView(element, viewId);

                CommandHelper.RecordStep(session, "elem set-view", elementId,
                    new Dictionary<string, object?> { ["view"] = viewId }, true);
                sessionManager.Save(sessionPath, session);

                var entry = SessionManager.GetElement(session, elementId);
                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "View changed.",
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
