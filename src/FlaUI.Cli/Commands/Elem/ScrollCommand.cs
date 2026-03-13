using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class ScrollCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID" };
        idOption.Required = true;
        var horizontalOption = new Option<double?>("--horizontal") { Description = "Horizontal scroll percent (0-100, or -1 for no change)" };
        var verticalOption = new Option<double?>("--vertical") { Description = "Vertical scroll percent (0-100, or -1 for no change)" };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("scroll", "Scroll a container to a position via the Scroll pattern");
        command.Add(idOption);
        command.Add(horizontalOption);
        command.Add(verticalOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var horizontal = parseResult.GetValue(horizontalOption);
            var vertical = parseResult.GetValue(verticalOption);
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

                AutomationEngine.SetScroll(element, horizontal, vertical);

                CommandHelper.RecordStep(session, "elem scroll", elementId,
                    new Dictionary<string, object?> { ["horizontal"] = horizontal, ["vertical"] = vertical }, true);
                sessionManager.Save(sessionPath, session);

                var entry = SessionManager.GetElement(session, elementId);
                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "Scrolled.",
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
