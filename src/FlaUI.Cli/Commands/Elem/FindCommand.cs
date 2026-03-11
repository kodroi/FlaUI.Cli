using System.CommandLine;
using FlaUI.Core.AutomationElements;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class FindCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var aidOption = new Option<string?>("--aid") { Description = "AutomationId property to match (e.g. \"SubmitButton\"). Preferred — gives 'stable' selector quality" };
        var nameOption = new Option<string?>("--name") { Description = "Element Name property to match (e.g. \"Submit\"). Gives 'acceptable' selector quality" };
        var typeOption = new Option<string?>("--type") { Description = "UIA ControlType to match (e.g. \"Button\", \"Edit\", \"ComboBox\")" };
        var classOption = new Option<string?>("--class") { Description = "WPF/WinForms ClassName to match (e.g. \"TextBox\"). Gives 'fragile' selector quality" };
        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "How long to search before giving up, in milliseconds",
            DefaultValueFactory = _ => 10000
        };

        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("find", "Find an element by properties");
        command.Add(aidOption);
        command.Add(nameOption);
        command.Add(typeOption);
        command.Add(classOption);
        command.Add(timeoutOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var aid = parseResult.GetValue(aidOption);
            var name = parseResult.GetValue(nameOption);
            var type = parseResult.GetValue(typeOption);
            var cls = parseResult.GetValue(classOption);
            var timeout = parseResult.GetValue(timeoutOption);
            var windowHandle = parseResult.GetValue(windowOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                // If a specific window is targeted, search only that window
                if (!string.IsNullOrEmpty(windowHandle))
                {
                    var targetWindow = CommandHelper.ResolveWindow(engine, mainWindow, windowHandle);
                    var resolver = engine.CreateSelectorResolver();
                    var result = resolver.Resolve(targetWindow, aid, name, type, cls, timeout);
                    if (result is not null)
                    {
                        WriteFoundResult(result, targetWindow, session, sessionPath, sessionManager);
                        return;
                    }

                    JsonOutput.Write(new ErrorResult(false, "Element not found."));
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                // Multi-window search: try main window first, then all others
                var allWindows = engine.GetAllTopLevelWindows();
                var windowCount = Math.Max(allWindows.Length, 1);
                var perWindowTimeout = Math.Max(timeout / windowCount, 2000);

                // Fast path: main window first with short timeout
                {
                    var resolver = engine.CreateSelectorResolver();
                    var result = resolver.Resolve(mainWindow, aid, name, type, cls, perWindowTimeout);
                    if (result is not null)
                    {
                        WriteFoundResult(result, mainWindow, session, sessionPath, sessionManager);
                        return;
                    }
                }

                // Search remaining windows
                var mainHandle = mainWindow.Properties.NativeWindowHandle.ValueOrDefault;
                foreach (var window in allWindows)
                {
                    var winHandle = window.Properties.NativeWindowHandle.ValueOrDefault;
                    if (winHandle == mainHandle) continue;

                    var resolver = engine.CreateSelectorResolver();
                    var result = resolver.Resolve(window, aid, name, type, cls, perWindowTimeout);
                    if (result is not null)
                    {
                        WriteFoundResult(result, window, session, sessionPath, sessionManager);
                        return;
                    }
                }

                JsonOutput.Write(new ErrorResult(false, "Element not found."));
                Environment.ExitCode = ExitCodes.Unresolvable;
            }
            catch (Exception ex)
            {
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static void WriteFoundResult(
        SelectorResult result,
        AutomationElement window,
        SessionFile session,
        string sessionPath,
        SessionManager sessionManager)
    {
        var elementId = Guid.NewGuid().ToString("N")[..8];
        var element = result.Element;
        var bounds = element.BoundingRectangle;
        var winHandle = window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();

        SessionManager.AddElement(session, elementId, new ElementEntry
        {
            AutomationId = element.Properties.AutomationId.ValueOrDefault,
            Name = element.Properties.Name.ValueOrDefault,
            ControlType = element.Properties.ControlType.ValueOrDefault.ToString(),
            ClassName = element.Properties.ClassName.ValueOrDefault,
            RuntimeId = element.Properties.RuntimeId.ValueOrDefault,
            SelectorQuality = result.Quality,
            LastVerified = DateTime.UtcNow
        });

        sessionManager.Save(sessionPath, session);

        JsonOutput.Write(new ElementFindResult(
            Success: true,
            Message: "Element found.",
            ElementId: elementId,
            AutomationId: element.Properties.AutomationId.ValueOrDefault,
            Name: element.Properties.Name.ValueOrDefault,
            ControlType: element.Properties.ControlType.ValueOrDefault.ToString(),
            SelectorQuality: result.Quality,
            SelectorStrategy: result.Strategy,
            Bounds: new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            WindowHandle: $"0x{winHandle:X}"));

        Environment.ExitCode = ExitCodes.Success;
    }
}
