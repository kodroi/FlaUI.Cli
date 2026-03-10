using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Session;

public static class AttachCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var pidOption = new Option<int?>("--pid", "Process ID to attach to");
        var nameOption = new Option<string?>("--name", "Process name to attach to");
        var titleOption = new Option<string?>("--title", "Window title to attach to");

        var command = new Command("attach", "Attach to a running application and create a session");
        command.Add(pidOption);
        command.Add(nameOption);
        command.Add(titleOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var name = parseResult.GetValue(nameOption);
            var title = parseResult.GetValue(titleOption);
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

                var mainWindow = engine.GetMainWindow(TimeSpan.FromSeconds(10));

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
                    MainWindowTitle: mainWindow.Properties.Name.ValueOrDefault,
                    SelectorPolicy: session.SelectorPolicy));

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
