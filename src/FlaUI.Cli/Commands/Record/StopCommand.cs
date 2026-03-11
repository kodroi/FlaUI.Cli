using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Record;

public static class StopCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var command = new Command("stop", "Stop recording interactions");

        command.SetAction((ParseResult parseResult) =>
        {
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var recordingService = new RecordingService();
                RecordingService.Stop(session);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "Recording stopped.",
                    ElementId: null,
                    SelectorQuality: null));

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
