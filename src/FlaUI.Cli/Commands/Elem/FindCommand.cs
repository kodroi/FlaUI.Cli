using System.CommandLine;
using System.Diagnostics;
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
            Description = "Total search timeout across all windows and selectors, in milliseconds",
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

            var stopwatch = Stopwatch.StartNew();
            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var remaining = TimeSpan.FromMilliseconds(Math.Max(0, timeout - stopwatch.ElapsedMilliseconds));
                var (_, selectedWindow) = engine.ReattachFromSession(session, remaining);

                Func<IEnumerable<AutomationElement>> getWindows;
                if (!string.IsNullOrEmpty(windowHandle))
                {
                    var targetWindow = CommandHelper.ResolveWindow(engine, selectedWindow, windowHandle);
                    getWindows = () => [targetWindow];
                }
                else
                {
                    getWindows = () => SearchWindows(engine, selectedWindow, session.TargetWindowHandle);
                }

                var resolver = engine.CreateSelectorResolver();
                var found = resolver.ResolveAcrossWindows(getWindows, stopwatch, timeout, aid, name, type, cls);
                if (found is { } match)
                    return WriteFoundResult(match.Result, match.Window, session, sessionPath, sessionManager);

                JsonOutput.Write(new ErrorResult(false, "Element not found."));
                return ExitCodes.Unresolvable;
            }
            catch (Exception ex)
            {
                JsonOutput.Write(CommandErrors.FromException(ex));
                return ExitCodes.Error;
            }
        });

        return command;
    }

    private static IEnumerable<AutomationElement> SearchWindows(
        AutomationEngine engine, AutomationElement selectedWindow, long? selectedHandle)
    {
        yield return selectedWindow;

        foreach (var window in engine.GetAllTopLevelWindows())
        {
            if (window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64() != selectedHandle)
                yield return window;
        }
    }

    private static int WriteFoundResult(
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
            LastVerified = DateTime.UtcNow,
            WindowHandle = winHandle
        });

        sessionManager.Save(sessionPath, session);
        TargetContext.Set(window);
        TargetContext.SetCoordinates(element, bounds);

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

        return ExitCodes.Success;
    }
}
