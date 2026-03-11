using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class GetCellCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID" };
        idOption.Required = true;
        var rowOption = new Option<int>("--row") { Description = "Row index (0-based)" };
        rowOption.Required = true;
        var columnOption = new Option<int>("--column") { Description = "Column index (0-based)" };
        columnOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("get-cell", "Get the value of a cell in a Grid element by row and column index");
        command.Add(idOption);
        command.Add(rowOption);
        command.Add(columnOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var row = parseResult.GetValue(rowOption);
            var column = parseResult.GetValue(columnOption);
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

                var result = AutomationEngine.GetGridCell(element, elementId, row, column);

                CommandHelper.RecordStep(session, "elem get-cell", elementId,
                    new Dictionary<string, object?> { ["row"] = row, ["column"] = column }, true);
                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(result);
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
