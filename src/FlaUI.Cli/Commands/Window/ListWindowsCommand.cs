using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Window;

public static class ListWindowsCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var command = new Command("list", "List all top-level windows for the attached application");

        command.SetAction((ParseResult parseResult) =>
        {
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                engine.Attach(session.Application.Pid);
                JsonOutput.Write(engine.ListWindows());

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
