using System.CommandLine;
using System.Globalization;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Window;

public static class FocusWindowCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var handleOption = new Option<string>("--handle") { Description = "Window handle as hex string (from 'window list')" };
        handleOption.Required = true;

        var command = new Command("focus", "Bring a window to the foreground");
        command.Add(handleOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var handleStr = parseResult.GetValue(handleOption)!;
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                engine.ReattachFromSession(session);

                var handle = long.Parse(handleStr, NumberStyles.HexNumber);
                var window = engine.GetWindowByHandle(handle);
                if (window is null)
                {
                    JsonOutput.Write(new ErrorResult(false, $"Window with handle 0x{handleStr} not found."));
                    Environment.ExitCode = ExitCodes.Error;
                    return;
                }

                var hwnd = window.Properties.NativeWindowHandle.ValueOrDefault;
                NativeInterop.BringToFront(hwnd);
                window.Focus();

                JsonOutput.Write(new WindowFocusResult(
                    Success: true,
                    Message: "Window focused.",
                    Handle: handleStr,
                    Title: window.Title));

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
