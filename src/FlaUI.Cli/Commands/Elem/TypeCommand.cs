using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class TypeCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Short element ID returned by 'elem find' (e.g. \"f9d13611\")" };
        idOption.Required = true;
        var textOption = new Option<string>("--text") { Description = "Text string to type into the element via keyboard simulation" };
        textOption.Required = true;

        var command = new Command("type", "Type text into an element");
        command.Add(idOption);
        command.Add(textOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var text = parseResult.GetValue(textOption)!;
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var element = CommandHelper.ResolveElement(engine, sessionManager, session, mainWindow, elementId);
                if (element is null)
                {
                    JsonOutput.Write(new ErrorResult(false, $"Element '{elementId}' not found."));
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                engine.Type(element, text);

                CommandHelper.RecordStep(session, "elem type", elementId,
                    new Dictionary<string, object?> { ["text"] = text }, true);

                sessionManager.Save(sessionPath, session);

                var entry = sessionManager.GetElement(session, elementId);
                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "Text typed.",
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
