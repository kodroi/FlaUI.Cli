using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Record;

public static class KeepCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var seqOption = new Option<int>("--seq") { Description = "Sequence number of a previously dropped step to restore (reverses a 'record drop')" };
        seqOption.Required = true;

        var command = new Command("keep", "Re-include a previously dropped recording step");
        command.Add(seqOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var seq = parseResult.GetValue(seqOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var recordingService = new RecordingService();
                RecordingService.Keep(session, seq);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: $"Step {seq} re-included.",
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
