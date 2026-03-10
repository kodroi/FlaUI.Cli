using System.CommandLine;
using System.Diagnostics;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Wait;

public static class WaitCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var aidOption = new Option<string>("--aid", "AutomationId to wait for");
        aidOption.Required = true;
        var valueOption = new Option<string?>("--value", "Wait until element has this value");
        var stateOption = new Option<string?>("--state", "Wait for state: hidden, visible, enabled");
        var timeoutOption = new Option<int>("--timeout", "Timeout in milliseconds");
        timeoutOption.Required = true;

        var command = new Command("wait", "Wait for an element condition");
        command.Add(aidOption);
        command.Add(valueOption);
        command.Add(stateOption);
        command.Add(timeoutOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var aid = parseResult.GetValue(aidOption)!;
            var expectedValue = parseResult.GetValue(valueOption);
            var expectedState = parseResult.GetValue(stateOption);
            var timeout = parseResult.GetValue(timeoutOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var resolver = engine.CreateSelectorResolver();
                var sw = Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < timeout)
                {
                    var result = resolver.Resolve(mainWindow, automationId: aid, timeoutMs: 500);
                    if (result is not null)
                    {
                        var element = result.Element;
                        var conditionMet = true;

                        if (expectedValue is not null)
                        {
                            var currentValue = engine.GetValue(element);
                            conditionMet = string.Equals(currentValue, expectedValue,
                                StringComparison.OrdinalIgnoreCase);
                        }

                        if (expectedState is not null && conditionMet)
                        {
                            conditionMet = expectedState.ToLowerInvariant() switch
                            {
                                "hidden" => element.IsOffscreen,
                                "visible" => !element.IsOffscreen,
                                "enabled" => element.IsEnabled,
                                _ => true
                            };
                        }

                        if (conditionMet)
                        {
                            JsonOutput.Write(new WaitResult(
                                Success: true,
                                Message: "Condition met.",
                                Elapsed: sw.ElapsedMilliseconds));

                            Environment.ExitCode = ExitCodes.Success;
                            return;
                        }
                    }
                    else if (expectedState?.ToLowerInvariant() == "hidden")
                    {
                        // Element not found = hidden
                        JsonOutput.Write(new WaitResult(
                            Success: true,
                            Message: "Element is hidden (not found).",
                            Elapsed: sw.ElapsedMilliseconds));

                        Environment.ExitCode = ExitCodes.Success;
                        return;
                    }

                    Thread.Sleep(100);
                }

                JsonOutput.Write(new ErrorResult(false, $"Timeout after {timeout}ms."));
                Environment.ExitCode = ExitCodes.Error;
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
