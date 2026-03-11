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
        var aidOption = new Option<string?>("--aid") { Description = "AutomationId of the element to wait for (e.g. \"StatusLabel\")" };
        var titleOption = new Option<string?>("--title") { Description = "Wait until a window title contains this text (case-insensitive)" };
        var valueOption = new Option<string?>("--value") { Description = "Wait until the element's value equals this text (case-insensitive comparison)" };
        var stateOption = new Option<string?>("--state") { Description = "Wait for a UI state: 'visible' (on-screen), 'hidden' (off-screen or gone), or 'enabled' (interactable)" };
        var timeoutOption = new Option<int>("--timeout") { Description = "Maximum time to wait in milliseconds. Fails with exit code 1 if exceeded" };
        timeoutOption.Required = true;

        var command = new Command("wait", "Wait for an element condition or window title");
        command.Add(aidOption);
        command.Add(titleOption);
        command.Add(valueOption);
        command.Add(stateOption);
        command.Add(timeoutOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var aid = parseResult.GetValue(aidOption);
            var title = parseResult.GetValue(titleOption);
            var expectedValue = parseResult.GetValue(valueOption);
            var expectedState = parseResult.GetValue(stateOption);
            var timeout = parseResult.GetValue(timeoutOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            if (string.IsNullOrEmpty(aid) && string.IsNullOrEmpty(title))
            {
                JsonOutput.Write(new ErrorResult(false, "Either --aid or --title must be specified."));
                Environment.ExitCode = ExitCodes.Error;
                return;
            }

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var sw = Stopwatch.StartNew();

                if (!string.IsNullOrEmpty(title))
                {
                    WaitForWindowTitle(engine, sessionManager, session, sessionPath, title, timeout, sw);
                }
                else
                {
                    WaitForElement(engine, mainWindow, aid!, expectedValue, expectedState, timeout, sw);
                }
            }
            catch (Exception ex)
            {
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static void WaitForElement(
        AutomationEngine engine,
        Core.AutomationElements.AutomationElement mainWindow,
        string aid,
        string? expectedValue,
        string? expectedState,
        int timeout,
        Stopwatch sw)
    {
        var resolver = engine.CreateSelectorResolver();

        while (sw.ElapsedMilliseconds < timeout)
        {
            var result = resolver.Resolve(mainWindow, automationId: aid, timeoutMs: 500);
            if (result is not null)
            {
                var element = result.Element;
                var conditionMet = true;

                if (expectedValue is not null)
                {
                    var currentValue = AutomationEngine.GetValue(element);
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

    private static void WaitForWindowTitle(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        string sessionPath,
        string title,
        int timeout,
        Stopwatch sw)
    {
        while (sw.ElapsedMilliseconds < timeout)
        {
            var windows = engine.GetAllTopLevelWindows();
            var match = windows.FirstOrDefault(w =>
                w.Properties.Name.ValueOrDefault?.Contains(title, StringComparison.OrdinalIgnoreCase) == true);

            if (match is not null)
            {
                var handle = match.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();
                var windowTitle = match.Properties.Name.ValueOrDefault;
                var handleHex = $"0x{handle:X}";

                session.Application.MainWindowHandle = handle;
                session.Application.MainWindowTitle = windowTitle;
                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new WaitResult(
                    Success: true,
                    Message: $"Window with title containing '{title}' found.",
                    Elapsed: sw.ElapsedMilliseconds,
                    WindowHandle: handleHex,
                    WindowTitle: windowTitle));

                Environment.ExitCode = ExitCodes.Success;
                return;
            }

            Thread.Sleep(100);
        }

        JsonOutput.Write(new ErrorResult(false, $"Timeout after {timeout}ms waiting for window title containing '{title}'."));
        Environment.ExitCode = ExitCodes.Error;
    }
}
