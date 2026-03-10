using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Record;

public static class DropCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var seqOption = new Option<int?>("--seq", "Step sequence number to drop");
        var lastOption = new Option<int?>("--last", "Drop last N steps")
        {
            DefaultValueFactory = _ => 1
        };

        var command = new Command("drop", "Exclude a recording step");
        command.Add(seqOption);
        command.Add(lastOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var seq = parseResult.GetValue(seqOption);
            var last = parseResult.GetValue(lastOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var recordingService = new RecordingService();

                if (seq.HasValue)
                    recordingService.Drop(session, seq.Value);
                else
                    recordingService.DropLast(session, last ?? 1);

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new ActionResult(
                    Success: true,
                    Message: seq.HasValue ? $"Step {seq} dropped." : $"Last {last ?? 1} step(s) dropped.",
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
