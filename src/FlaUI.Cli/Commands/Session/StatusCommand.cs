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

                var processAlive = sessionManager.IsProcessAlive(session);
                var windowValid = sessionManager.IsWindowValid(session);

                JsonOutput.Write(new SessionStatusResult(
                    Success: true,
                    Message: "Session status retrieved.",
                    Pid: session.Application.Pid,
                    ProcessAlive: processAlive,
                    WindowValid: windowValid,
                    ElementCount: session.Elements.Count,
                    Recording: session.Recording?.Active ?? false));

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
