using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Record;

public static class ExportCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var outOption = new Option<string>("--out") { Description = "Output file path" };
        outOption.Required = true;

        var command = new Command("export", "Export recording steps to JSON");
        command.Add(outOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var outPath = parseResult.GetValue(outOption)!;
            var sessionFlag = parseResult.GetValue(sessionOption);

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var sessionManager = new SessionManager();
                var session = sessionManager.Load(sessionPath);

                var recordingService = new RecordingService();
                var exportResult = recordingService.Export(session);

                File.WriteAllText(Path.GetFullPath(outPath), JsonOutput.Serialize(exportResult));

                JsonOutput.Write(exportResult);
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
