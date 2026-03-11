using System.CommandLine;
using System.Globalization;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Window;

public static class CloseWindowCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var handleOption = new Option<string?>("--handle") { Description = "Window handle as hex string (from 'window list')" };
        var titleOption = new Option<string?>("--title") { Description = "Window title to match (partial, case-insensitive)" };
        var forceOption = new Option<bool>("--force") { Description = "Force-kill the window's owning process if graceful close fails" };

        var command = new Command("close", "Close a window by handle or title");
        command.Add(handleOption);
        command.Add(titleOption);
        command.Add(forceOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var handleStr = parseResult.GetValue(handleOption);
            var title = parseResult.GetValue(titleOption);
            var force = parseResult.GetValue(forceOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            if (string.IsNullOrEmpty(handleStr) && string.IsNullOrEmpty(title))
            {
                JsonOutput.Write(new ErrorResult(false, "Either --handle or --title must be specified."));
                Environment.ExitCode = ExitCodes.Error;
                return;
            }

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                engine.ReattachFromSession(session);

                FlaUI.Core.AutomationElements.Window? window = null;

                if (!string.IsNullOrEmpty(handleStr))
                {
                    var handle = long.Parse(handleStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    window = engine.GetWindowByHandle(handle);
                }
                else if (!string.IsNullOrEmpty(title))
                {
                    var windows = engine.GetAllTopLevelWindows();
                    window = windows.FirstOrDefault(w =>
                        w.Title?.Contains(title, StringComparison.OrdinalIgnoreCase) == true);
                }

                if (window is null)
                {
                    JsonOutput.Write(new ErrorResult(false, "Window not found."));
                    Environment.ExitCode = ExitCodes.Error;
                    return;
                }

                var closedHandle = $"{window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}";
                var closedTitle = window.Title;

                window.Close();

                if (force)
                {
                    Thread.Sleep(500);
                    var hwnd = new IntPtr(long.Parse(closedHandle, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    if (NativeInterop.IsWindow(hwnd))
                    {
                        engine.CloseApplication(force: true);
                    }
                }

                JsonOutput.Write(new WindowCloseResult(
                    Success: true,
                    Message: "Window closed.",
                    Handle: closedHandle,
                    Title: closedTitle));

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
