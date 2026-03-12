using System.CommandLine;
using System.Diagnostics;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Session;

public static class NewCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var appOption = new Option<string>("--app") { Description = "Full or relative path to the .exe to launch (e.g. \"C:\\Apps\\MyApp.exe\")" };
        appOption.Required = true;
        var argsOption = new Option<string?>("--args") { Description = "Command-line arguments forwarded to the application (e.g. \"--config prod.json\")" };
        var waitTitleOption = new Option<string?>("--wait-title") { Description = "Wait until a window title contains this text before completing session creation" };
        var waitTimeoutOption = new Option<int>("--wait-timeout")
        {
            Description = "Maximum time in milliseconds to wait for --wait-title (default: 30000)",
            DefaultValueFactory = _ => 30000
        };

        var command = new Command("new", "Launch an application and create a new session");
        command.Add(appOption);
        command.Add(argsOption);
        command.Add(waitTitleOption);
        command.Add(waitTimeoutOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var app = Path.GetFullPath(parseResult.GetValue(appOption)!);
            var args = parseResult.GetValue(argsOption);
            var waitTitle = parseResult.GetValue(waitTitleOption);
            var waitTimeout = parseResult.GetValue(waitTimeoutOption);
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

                Core.AutomationElements.AutomationElement mainWindow;

                if (!string.IsNullOrEmpty(waitTitle))
                {
                    mainWindow = WaitForWindowTitle(engine, waitTitle, waitTimeout);
                }
                else
                {
                    mainWindow = engine.GetMainWindow(TimeSpan.FromSeconds(30));
                }

                var session = new SessionFile
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Application = new ApplicationInfo
                    {
                        Path = app,
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
                    MainWindowTitle: mainWindow.Properties.Name.ValueOrDefault));

                Environment.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                engine.CloseApplication(force: true);
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static Core.AutomationElements.Window WaitForWindowTitle(
        AutomationEngine engine, string title, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var windows = engine.GetAllTopLevelWindows();
            var match = windows.FirstOrDefault(w =>
                w.Properties.Name.ValueOrDefault?.Contains(title, StringComparison.OrdinalIgnoreCase) == true);

            if (match is not null)
                return match;

            Thread.Sleep(100);
        }

        throw new InvalidOperationException(
            $"Timeout after {timeoutMs}ms waiting for window title containing '{title}'.");
    }
}
