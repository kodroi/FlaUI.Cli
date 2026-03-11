using System.CommandLine;
using System.Text.Json;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Batch;

public static class BatchCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var fileOption = new Option<string?>("--file") { Description = "Path to a JSON file containing batch steps" };
        var stepsOption = new Option<string?>("--steps") { Description = "Inline JSON string with batch steps (e.g. '{\"steps\": [...]}')" };
        var continueOnErrorOption = new Option<bool>("--continue-on-error") { Description = "Continue executing remaining steps after a failure" };

        var command = new Command("batch", "Execute multiple commands in a single session");
        command.Add(fileOption);
        command.Add(stepsOption);
        command.Add(continueOnErrorOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var file = parseResult.GetValue(fileOption);
            var stepsJson = parseResult.GetValue(stepsOption);
            var continueOnError = parseResult.GetValue(continueOnErrorOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            if (string.IsNullOrEmpty(file) && string.IsNullOrEmpty(stepsJson))
            {
                JsonOutput.Write(new ErrorResult(false, "Either --file or --steps must be specified."));
                Environment.ExitCode = ExitCodes.Error;
                return;
            }

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var json = !string.IsNullOrEmpty(file) ? File.ReadAllText(file) : stepsJson!;
                var batchInput = JsonSerializer.Deserialize<BatchInput>(json, JsonOutput.GetOptions());

                if (batchInput?.Steps is null || batchInput.Steps.Count == 0)
                {
                    JsonOutput.Write(new ErrorResult(false, "No steps found in batch input."));
                    Environment.ExitCode = ExitCodes.Error;
                    return;
                }

                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var executor = new BatchExecutor(engine, sessionManager, session, sessionPath, mainWindow);
                var result = executor.Execute(batchInput.Steps, continueOnError);

                JsonOutput.Write(result);
                Environment.ExitCode = result.Success ? ExitCodes.Success : ExitCodes.Error;
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
