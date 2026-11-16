using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class GetValueCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var saveOption = new Option<string?>("--save") { Description = "Store the retrieved value in the session file under this variable name for later use" };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("get-value", "Get an element's value");
        var (idOption, aidOption) = CommandHelper.AddActionSelectorOptions(command);
        command.Add(saveOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var id = parseResult.GetValue(idOption);
            var aid = parseResult.GetValue(aidOption);
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

                var target = CommandHelper.ResolveActionTarget(engine, sessionManager, session, targetWindow, id, aid, out var error);
                if (target is not { } resolved)
                {
                    JsonOutput.Write(error);
                    return ExitCodes.Unresolvable;
                }

                var (element, elementId) = resolved;
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
