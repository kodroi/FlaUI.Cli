using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Record;

public static class StartCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var descOption = new Option<string?>("--description") { Description = "Human-readable label for this recording session (e.g. \"Login flow test\")" };

        var command = new Command("start", "Start recording interactions");
        command.Add(descOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var description = parseResult.GetValue(descOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var recordingService = new RecordingService();
                RecordingService.Start(session, description);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: "Recording started.",
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
