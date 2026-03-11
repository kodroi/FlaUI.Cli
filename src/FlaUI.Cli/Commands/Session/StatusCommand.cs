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

                if (processAlive && windowValid)
                {
                    try
                    {
                        using var engine = new AutomationEngine();
                        var (_, mainWindow) = engine.ReattachFromSession(session);
                        mainWindowTitle = mainWindow.Properties.Name.ValueOrDefault;
                        mainWindowHandle = $"0x{mainWindow.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}";

                        session.Application.MainWindowTitle = mainWindowTitle;
                        sessionManager.Save(sessionPath, session);
                    }
                    catch
                    {
                        // Fall back to stored values
                    }
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
