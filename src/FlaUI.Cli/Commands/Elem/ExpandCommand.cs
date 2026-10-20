using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class ExpandCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Short element ID returned by 'elem find' (e.g. \"ec86bb98\")" };
        idOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("expand", "Expand an element (Expander, TreeViewItem, ComboBox, etc.)");
        command.Add(idOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
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

                AutomationEngine.Expand(element);

                CommandHelper.RecordStep(session, "elem expand", elementId, null, true);
                sessionManager.Save(sessionPath, session);

                var entry = SessionManager.GetElement(session, elementId);
                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "Expanded.",
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
