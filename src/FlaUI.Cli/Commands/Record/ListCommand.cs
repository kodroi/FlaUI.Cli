using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Record;

public static class ListCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var command = new Command("list", "List recording steps");

        command.SetAction((ParseResult parseResult) =>
        {
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var recordingService = new RecordingService();
                var steps = recordingService.List(session);

                JsonOutput.Write(new RecordListResult(
                    Success: true,
                    Message: $"{steps.Count} step(s).",
                    Steps: steps));

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
