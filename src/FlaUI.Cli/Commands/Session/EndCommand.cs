using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Session;

public static class EndCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var closeAppOption = new Option<bool>("--close-app") { Description = "Also close the target application process when ending the session" };

        var command = new Command("end", "End the session");
        command.Add(closeAppOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var closeApp = parseResult.GetValue(closeAppOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                if (closeApp)
                {
                    using var engine = new AutomationEngine();
                    try
                    {
                        engine.Attach(session.Application.Pid);
                        engine.CloseApplication();
                    }
                    catch
                    {
                        // Process may already be dead
                    }
                }

                File.Delete(sessionPath);

                JsonOutput.Write(new SessionEndResult(
                    Success: true,
                    Message: closeApp ? "Session ended and application closed." : "Session ended."));

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
