using System.CommandLine;
using System.Globalization;
using FlaUI.Cli.Commands.Elem;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Screenshot;

/// <summary>
/// Captures a screenshot of a window or element and saves it to a file.
/// </summary>
public static class ScreenshotCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string?>("--id")
        {
            Description = "Element ID returned by 'elem find' — captures that element's bounding rectangle"
        };
        var windowOption = new Option<string?>("--window")
        {
            Description = "Window handle (hex) — captures that window instead of the main window"
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output file path (required). Format inferred from extension (.png, .bmp, .jpg)"
        };
        outputOption.Required = true;

        var command = new Command("screenshot", "Capture a screenshot of a window or element");
        command.Add(idOption);
        command.Add(windowOption);
        command.Add(outputOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption);
            var windowHandle = parseResult.GetValue(windowOption);
            var output = parseResult.GetValue(outputOption)!;
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var target = ResolveTarget(engine, sessionManager, session, mainWindow, elementId, windowHandle);

                AutomationEngine.EnsureInteractable(target);
                Thread.Sleep(200);

                var result = AutomationEngine.CaptureScreenshot(target, output);
                JsonOutput.Write(result);
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

    private static Core.AutomationElements.AutomationElement ResolveTarget(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        Core.AutomationElements.AutomationElement mainWindow,
        string? elementId,
        string? windowHandle)
    {
        if (!string.IsNullOrEmpty(elementId))
        {
            var element = CommandHelper.ResolveElement(engine, sessionManager, session, mainWindow, elementId);
            if (element is null)
                throw new InvalidOperationException($"Element '{elementId}' not found.");
            return element;
        }

        if (!string.IsNullOrEmpty(windowHandle))
        {
            var handle = long.Parse(windowHandle, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return engine.ResolveWindow(handle);
        }

        return mainWindow;
    }
}
