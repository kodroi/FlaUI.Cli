using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class KeysCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var keysOption = new Option<string>("--keys") { Description = "Key combination to send (e.g. \"ctrl+shift+s\", \"tab\", \"alt+f4\")" };
        keysOption.Required = true;
        var idOption = new Option<string?>("--id") { Description = "Optional element ID to focus before sending keys" };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("keys", "Send keyboard input (key combinations or single keys)");
        command.Add(keysOption);
        command.Add(idOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var keysStr = parseResult.GetValue(keysOption)!;
            var elementId = parseResult.GetValue(idOption);
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

                FlaUI.Core.AutomationElements.AutomationElement? target = null;
                if (!string.IsNullOrEmpty(elementId))
                {
                    target = CommandHelper.ResolveElement(engine, sessionManager, session, targetWindow, elementId);
                    if (target is null)
                    {
                        JsonOutput.Write(new ErrorResult(false, $"Element '{elementId}' not found."));
                        Environment.ExitCode = ExitCodes.Unresolvable;
                        return;
                    }
                }

                var keys = KeyParser.Parse(keysStr);
                AutomationEngine.SendKeys(keys, target, window: target is null ? targetWindow : null);

                CommandHelper.RecordStep(session, "elem keys", elementId ?? "",
                    new Dictionary<string, object?> { ["keys"] = keysStr }, true);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new KeysResult(
                    Success: true,
                    Message: $"Keys '{keysStr}' sent.",
                    Keys: keysStr,
                    ElementId: elementId));

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
