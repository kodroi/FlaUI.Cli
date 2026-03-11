using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Window;

public static class ListWindowsCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var command = new Command("list", "List all top-level windows for the attached application");

        command.SetAction((ParseResult parseResult) =>
        {
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                engine.ReattachFromSession(session);

                var windows = engine.GetAllTopLevelWindows();
                var items = new List<WindowInfoItem>();

                foreach (var w in windows)
                {
                    var handle = w.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();
                    var bounds = w.BoundingRectangle;
                    items.Add(new WindowInfoItem(
                        Handle: $"{handle:X}",
                        Title: w.Title,
                        IsModal: w.IsModal,
                        ClassName: w.Properties.ClassName.ValueOrDefault,
                        Bounds: new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height)));
                }

                JsonOutput.Write(new WindowListResult(
                    Success: true,
                    Message: $"Found {items.Count} window(s).",
                    Windows: items));

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
