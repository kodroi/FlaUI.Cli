using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Session;

public static class StatusCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var command = new Command("status", "Show session status");

        command.SetAction((ParseResult parseResult) =>
        {
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var processAlive = SessionManager.IsProcessAlive(session);
                var windowValid = SessionManager.IsWindowValid(session);

                string? mainWindowTitle = session.Application.MainWindowTitle;
                string? mainWindowHandle = null;

                if (processAlive)
                {
                    using var engine = new AutomationEngine();
                    engine.ReattachFromSession(session);
                    var mainWindow = engine.GetMainWindow();
                    mainWindowTitle = mainWindow.Properties.Name.ValueOrDefault;
                    var handle = mainWindow.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();
                    mainWindowHandle = $"0x{handle:X}";
                    windowValid = true;
                    session.Application.MainWindowHandle = handle;
                    session.Application.MainWindowTitle = mainWindowTitle;
                    sessionManager.Save(sessionPath, session);
                }
                JsonOutput.Write(new SessionStatusResult(
                    Success: true,
                    Message: "Session status retrieved.",
                    Pid: session.Application.Pid,
                    ProcessAlive: processAlive,
                    WindowValid: windowValid,
                    ElementCount: session.Elements.Count,
                    Recording: session.Recording?.Active ?? false,
                    MainWindowTitle: mainWindowTitle,
                    MainWindowHandle: mainWindowHandle));

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
