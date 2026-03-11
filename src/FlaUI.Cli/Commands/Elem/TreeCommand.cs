using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class TreeCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var rootOption = new Option<string?>("--root") { Description = "Element ID to use as tree root. Omit to start from the main window" };
        var depthOption = new Option<int>("--depth")
        {
            Description = "Maximum levels to descend into the element hierarchy",
            DefaultValueFactory = _ => 3
        };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("tree", "Dump the element tree as JSON");
        command.Add(rootOption);
        command.Add(depthOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var rootId = parseResult.GetValue(rootOption);
            var depth = parseResult.GetValue(depthOption);
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

                FlaUI.Core.AutomationElements.AutomationElement rootElement = targetWindow;

                if (!string.IsNullOrEmpty(rootId))
                {
                    var entry = SessionManager.GetElement(session, rootId);
                    if (entry is null)
                    {
                        JsonOutput.Write(new ErrorResult(false, $"Element '{rootId}' not found in session."));
                        Environment.ExitCode = ExitCodes.Error;
                        return;
                    }

                    var resolver = engine.CreateSelectorResolver();
                    var resolved = resolver.Resolve(targetWindow, entry.AutomationId, entry.Name,
                        entry.ControlType, entry.ClassName);
                    if (resolved is null)
                    {
                        JsonOutput.Write(new ErrorResult(false, $"Could not re-find element '{rootId}'."));
                        Environment.ExitCode = ExitCodes.Unresolvable;
                        return;
                    }

                    rootElement = resolved.Element;
                }

                var tree = AutomationEngine.BuildTree(rootElement, depth, session);
                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new ElementTreeResult(
                    Success: true,
                    Message: "Tree retrieved.",
                    Root: tree));

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
