using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class MenuCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var pathOption = new Option<string>("--path") { Description = "Menu path with '>' separators (e.g. \"File > Save As\")" };
        pathOption.Required = true;
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("menu", "Navigate and click a menu item by path");
        command.Add(pathOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var path = parseResult.GetValue(pathOption)!;
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
                var segments = path.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (segments.Length == 0)
                {
                    JsonOutput.Write(new ErrorResult(false, "Menu path cannot be empty."));
                    Environment.ExitCode = ExitCodes.Error;
                    return;
                }

                var clickedItem = engine.NavigateMenu(targetWindow, segments);
                var clickedName = clickedItem?.Properties.Name.ValueOrDefault;

                CommandHelper.RecordStep(session, "elem menu", "",
                    new Dictionary<string, object?> { ["path"] = path }, true, clickedName);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new MenuResult(
                    Success: true,
                    Message: $"Menu item '{clickedName}' clicked.",
                    Path: path,
                    ClickedItemName: clickedName));

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
