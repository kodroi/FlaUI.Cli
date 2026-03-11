using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class ScrollIntoViewCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID to scroll into view" };
        idOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("scroll-into-view", "Scroll an element into view using the ScrollItem pattern");
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
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                var scrolled = AutomationEngine.ScrollIntoView(element);

                CommandHelper.RecordStep(session, "elem scroll-into-view", elementId,
                    null, true);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new ScrollIntoViewResult(
                    Success: true,
                    Message: scrolled ? "Element scrolled into view." : "Element does not support ScrollItem pattern.",
                    ElementId: elementId,
                    Scrolled: scrolled));

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
