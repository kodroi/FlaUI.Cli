using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Session;

public static class AttachCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var pidOption = new Option<int?>("--pid") { Description = "OS process ID to attach to (e.g. 12345). Use one of --pid, --name, or --title" };
        var nameOption = new Option<string?>("--name") { Description = "Process name without .exe (e.g. \"notepad\"). Attaches to the first match" };
        var titleOption = new Option<string?>("--title") { Description = "Main window title text to match (e.g. \"Untitled - Notepad\")" };
        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "Maximum time in milliseconds to wait for the main window (default: 10000)",
            DefaultValueFactory = _ => 10000
        };

        var command = new Command("attach", "Attach to a running application and create a session");
        command.Add(pidOption);
        command.Add(nameOption);
        command.Add(titleOption);
        command.Add(timeoutOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var name = parseResult.GetValue(nameOption);
            var title = parseResult.GetValue(titleOption);
            var timeout = parseResult.GetValue(timeoutOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            if (pid is null && name is null && title is null)
            {
                JsonOutput.Write(new ErrorResult(false, "Provide --pid, --name, or --title."));
                Environment.ExitCode = ExitCodes.Error;
                return;
            }

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                FlaUI.Core.Application application;
                if (pid.HasValue)
                    application = engine.Attach(pid.Value);
                else if (name is not null)
                    application = engine.AttachByName(name);
                else
                    application = engine.AttachByTitle(title!);

                var mainWindow = engine.GetMainWindow(TimeSpan.FromMilliseconds(timeout));

                var sessionPath = !string.IsNullOrEmpty(sessionFlag)
                    ? Path.GetFullPath(sessionFlag)
                    : SessionManager.CreateSessionPath(application.Name ?? "app");

                var session = new SessionFile
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Application = new ApplicationInfo
                    {
                        Pid = application.ProcessId,
                        ProcessName = application.Name,
                        MainWindowHandle = mainWindow.Properties.NativeWindowHandle.ValueOrDefault.ToInt64(),
                        MainWindowTitle = mainWindow.Properties.Name.ValueOrDefault
                    }
                };

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new SessionAttachResult(
                    Success: true,
                    Message: "Attached to process.",
                    SessionFile: sessionPath,
                    Pid: application.ProcessId,
                    ProcessName: application.Name,
                    MainWindowTitle: mainWindow.Properties.Name.ValueOrDefault));

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
