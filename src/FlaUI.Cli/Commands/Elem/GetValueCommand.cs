using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class GetValueCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Short element ID returned by 'elem find'" };
        idOption.Required = true;
        var saveOption = new Option<string?>("--save") { Description = "Store the retrieved value in the session file under this variable name for later use" };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("get-value", "Get an element's value");
        command.Add(idOption);
        command.Add(saveOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var saveName = parseResult.GetValue(saveOption);
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

                var value = AutomationEngine.GetValue(element);

                if (!string.IsNullOrEmpty(saveName) && value is not null)
                    SessionManager.SetVariable(session, saveName, value);

                CommandHelper.RecordStep(session, "elem get-value", elementId,
                    new Dictionary<string, object?> { ["save"] = saveName }, true, value);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new GetValueResult(
                    Success: true,
                    Message: "Value retrieved.",
                    ElementId: elementId,
                    Value: value,
                    SavedAs: saveName));

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
