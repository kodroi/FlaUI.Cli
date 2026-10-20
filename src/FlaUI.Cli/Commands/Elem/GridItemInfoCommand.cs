using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class GridItemInfoCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID" };
        idOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("grid-item-info", "Get row, column, and span info for a cell via the GridItem pattern");
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

                var result = AutomationEngine.GetGridItemInfo(element, elementId);

                CommandHelper.RecordStep(session, "elem grid-item-info", elementId, null, true);
                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(result);
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
