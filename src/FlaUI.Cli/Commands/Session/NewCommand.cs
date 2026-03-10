using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Session;

public static class NewCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var appOption = new Option<string>("--app", "Path to the application executable");
        appOption.Required = true;
        var argsOption = new Option<string?>("--args", "Arguments to pass to the application");
        var policyOption = new Option<string>("--selector-policy", "Selector quality policy: stable, acceptable, fragile")
        {
            DefaultValueFactory = _ => "stable"
        };

        var command = new Command("new", "Launch an application and create a new session");
        command.Add(appOption);
        command.Add(argsOption);
        command.Add(policyOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var app = parseResult.GetValue(appOption)!;
            var args = parseResult.GetValue(argsOption);
            var policy = parseResult.GetValue(policyOption)!;
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = !string.IsNullOrEmpty(sessionFlag)
                    ? Path.GetFullPath(sessionFlag)
                    : SessionManager.CreateSessionPath(app);

                var application = engine.Launch(app, args);
                Thread.Sleep(500);

                var mainWindow = engine.GetMainWindow(TimeSpan.FromSeconds(30));

                var session = new SessionFile
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SelectorPolicy = policy.ToLowerInvariant(),
                    Application = new ApplicationInfo
                    {
                        Path = Path.GetFullPath(app),
                        Args = args,
                        Pid = application.ProcessId,
                        ProcessName = application.Name,
                        MainWindowHandle = mainWindow.Properties.NativeWindowHandle.ValueOrDefault.ToInt64(),
                        MainWindowTitle = mainWindow.Properties.Name.ValueOrDefault
                    }
                };

                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(new SessionNewResult(
                    Success: true,
                    Message: "Session created.",
                    SessionFile: sessionPath,
                    Pid: application.ProcessId,
                    ProcessName: application.Name,
                    MainWindowTitle: mainWindow.Properties.Name.ValueOrDefault,
                    SelectorPolicy: policy.ToLowerInvariant()));

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
