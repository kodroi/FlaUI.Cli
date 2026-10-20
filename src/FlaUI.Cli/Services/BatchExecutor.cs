using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Services;

public class BatchExecutor
{
    private readonly AutomationEngine _engine;
    private readonly SessionManager _sessionManager;
    private readonly SessionFile _session;
    private readonly string _sessionPath;

    private readonly List<JsonNode?> _stepResults = [];

    public BatchExecutor(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        string sessionPath)
    {
        _engine = engine;
        _sessionManager = sessionManager;
        _session = session;
        _sessionPath = sessionPath;

    }

    public BatchResult Execute(List<BatchStep> steps, bool continueOnError)
    {
        var results = new List<BatchStepResult>();
        var succeeded = 0;
        var failed = 0;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            try
            {
                TargetContext.Reset();
                var resolvedArgs = ResolveReferences(step.Args);
                var result = Dispatch(step.Cmd, resolvedArgs);
                var resultNode = JsonSerializer.SerializeToNode(result, JsonOutput.GetOptions());
                if (resultNode is JsonObject resultObject) TargetContext.AddTo(resultObject);
                _stepResults.Add(resultNode);
                results.Add(new BatchStepResult(i, step.Cmd, true, "OK", resultNode));
                succeeded++;
            }
            catch (Exception ex)
            {
                var error = CommandErrors.FromException(ex) with { Command = step.Cmd, Input = step.Args };
                var errorNode = JsonSerializer.SerializeToNode(error, JsonOutput.GetOptions())!.AsObject();
                TargetContext.AddTo(errorNode);
                _stepResults.Add(null);
                results.Add(new BatchStepResult(i, step.Cmd, false, ex.Message, null,
                    errorNode.Deserialize<ErrorResult>(JsonOutput.GetOptions())));
                failed++;

                if (!continueOnError)
                    break;
            }
        }

        _sessionManager.Save(_sessionPath, _session);
        TargetContext.Reset();

        return new BatchResult(
            Success: failed == 0,
            Message: $"{succeeded} succeeded, {failed} failed.",
            TotalSteps: results.Count,
            Succeeded: succeeded,
            Failed: failed,
            Steps: results);
    }

    private Dictionary<string, string> ResolveReferences(Dictionary<string, string> args)
    {
        var resolved = new Dictionary<string, string>();
        foreach (var (key, value) in args)
        {
            resolved[key] = ResolveValue(value);
        }
        return resolved;
    }

    private string ResolveValue(string value)
    {
        // $prev.field -> reference previous step result
        var prevMatch = Regex.Match(value, @"^\$prev\.(\w+)$");
        if (prevMatch.Success && _stepResults.Count > 0)
        {
            return ExtractField(_stepResults[^1], prevMatch.Groups[1].Value) ?? value;
        }

        // $steps[N].field -> reference specific step result
        var stepMatch = Regex.Match(value, @"^\$steps\[(\d+)\]\.(\w+)$");
        if (stepMatch.Success)
        {
            var index = int.Parse(stepMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (index >= 0 && index < _stepResults.Count)
            {
                return ExtractField(_stepResults[index], stepMatch.Groups[2].Value) ?? value;
            }
        }

        return value;
    }

    private object? Dispatch(string cmd, Dictionary<string, string> args)
    {
        if (cmd == "window list") return _engine.ListWindows();
        var windowHandle = args.GetValueOrDefault("window");
        var targetWindow = !string.IsNullOrEmpty(windowHandle)
            ? _engine.ResolveWindow(WindowHandleParser.Parse(windowHandle))
            : _engine.ReattachFromSession(_session).MainWindow;

        return cmd switch
        {
            "elem find" => DispatchFind(args, targetWindow),
            "elem click" => DispatchClick(args, targetWindow),
            "elem clear" => DispatchClear(args, targetWindow),
            "elem type" => DispatchType(args, targetWindow),
            "elem select" => DispatchSelect(args, targetWindow),
            "elem set-value" => DispatchSetValue(args, targetWindow),
            "elem get-value" => DispatchGetValue(args, targetWindow),
            "elem get-state" => DispatchGetState(args, targetWindow),
            "elem keys" => DispatchKeys(args, targetWindow),
            "elem scroll-into-view" => DispatchScrollIntoView(args, targetWindow),
            "elem menu" => DispatchMenu(args, targetWindow),
            "window focus" => DispatchWindowFocus(args),
            "window close" => DispatchWindowClose(args),
            "screenshot" => DispatchScreenshot(args, targetWindow),
            _ => throw new InvalidOperationException($"Unknown batch command: '{cmd}'")
        };
    }

    private ElementFindResult DispatchFind(Dictionary<string, string> args, AutomationElement window)
    {
        var resolver = _engine.CreateSelectorResolver();
        var timeout = int.TryParse(args.GetValueOrDefault("timeout"), out var t) ? t : 10000;
        var result = resolver.Resolve(window,
            args.GetValueOrDefault("aid"),
            args.GetValueOrDefault("name"),
            args.GetValueOrDefault("type"),
            args.GetValueOrDefault("class"),
            timeout);

        if (result is null)
            throw new InvalidOperationException("Element not found.");

        var elementId = Guid.NewGuid().ToString("N")[..8];
        var element = result.Element;
        var bounds = element.BoundingRectangle;
        TargetContext.SetCoordinates(element, bounds);

        SessionManager.AddElement(_session, elementId, new ElementEntry
        {
            AutomationId = element.Properties.AutomationId.ValueOrDefault,
            Name = element.Properties.Name.ValueOrDefault,
            ControlType = element.Properties.ControlType.ValueOrDefault.ToString(),
            ClassName = element.Properties.ClassName.ValueOrDefault,
            RuntimeId = element.Properties.RuntimeId.ValueOrDefault,
            SelectorQuality = result.Quality,
            WindowHandle = window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64(),
            LastVerified = DateTime.UtcNow
        });

        return new ElementFindResult(true, "Element found.", elementId,
            element.Properties.AutomationId.ValueOrDefault,
            element.Properties.Name.ValueOrDefault,
            element.Properties.ControlType.ValueOrDefault.ToString(),
            result.Quality, result.Strategy,
            new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            WindowHandle: $"0x{window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}");
    }

    private ActionResult DispatchClick(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        var dbl = args.GetValueOrDefault("double") == "true";
        var right = args.GetValueOrDefault("right") == "true";
        AutomationEngine.Click(element, dbl, right);

        var entry = SessionManager.GetElement(_session, id);
        return new ActionResult(true, "Clicked.", id, entry?.SelectorQuality);
    }

    private ActionResult DispatchClear(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        AutomationEngine.Clear(element);

        var entry = SessionManager.GetElement(_session, id);
        return new ActionResult(true, "Element cleared.", id, entry?.SelectorQuality);
    }

    private ActionResult DispatchType(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        _engine.Type(element, args["text"]);

        var entry = SessionManager.GetElement(_session, id);
        return new ActionResult(true, "Text typed.", id, entry?.SelectorQuality);
    }

    private ActionResult DispatchSelect(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        _engine.Select(element, args["item"]);

        var entry = SessionManager.GetElement(_session, id);
        return new ActionResult(true, $"Selected '{args["item"]}'.", id, entry?.SelectorQuality);
    }

    private ActionResult DispatchSetValue(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        AutomationEngine.SetValue(element, args["value"]);

        var entry = SessionManager.GetElement(_session, id);
        return new ActionResult(true, "Value set.", id, entry?.SelectorQuality);
    }

    private GetValueResult DispatchGetValue(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        var value = AutomationEngine.GetValue(element);

        var saveName = args.GetValueOrDefault("save");
        if (!string.IsNullOrEmpty(saveName) && value is not null)
            SessionManager.SetVariable(_session, saveName, value);

        return new GetValueResult(true, "Value retrieved.", id, value, saveName);
    }

    private GetStateResult DispatchGetState(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        return AutomationEngine.GetState(element, id);
    }

    private KeysResult DispatchKeys(Dictionary<string, string> args, AutomationElement window)
    {
        var keysStr = args["keys"];
        AutomationElement? target = null;
        var id = args.GetValueOrDefault("id");

        if (!string.IsNullOrEmpty(id))
            target = ResolveElement(window, id);

        var keys = KeyParser.Parse(keysStr);
        AutomationEngine.SendKeys(keys, target, window: target is null ? window : null);

        return new KeysResult(true, $"Keys '{keysStr}' sent.", keysStr, id);
    }

    private ScrollIntoViewResult DispatchScrollIntoView(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        var scrolled = AutomationEngine.ScrollIntoView(element);

        return new ScrollIntoViewResult(true,
            scrolled ? "Element scrolled into view." : "Element does not support ScrollItem pattern.",
            id, scrolled);
    }

    private MenuResult DispatchMenu(Dictionary<string, string> args, AutomationElement window)
    {
        var path = args["path"];
        var segments = path.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var clickedItem = _engine.NavigateMenu(window, segments);
        var clickedName = clickedItem?.Properties.Name.ValueOrDefault;

        return new MenuResult(true, $"Menu item '{clickedName}' clicked.", path, clickedName);
    }


    private WindowFocusResult DispatchWindowFocus(Dictionary<string, string> args)
    {
        var handle = WindowHandleParser.Parse(args["handle"]);
        var window = _engine.ResolveWindow(handle);

        var hwnd = window.Properties.NativeWindowHandle.ValueOrDefault;
        NativeInterop.BringToFront(hwnd);

        return new WindowFocusResult(true, "Window focused.", args["handle"], window.Title);
    }

    private WindowCloseResult DispatchWindowClose(Dictionary<string, string> args)
    {
        FlaUI.Core.AutomationElements.Window? window = null;

        var handleStr = args.GetValueOrDefault("handle");
        var title = args.GetValueOrDefault("title");

        if (!string.IsNullOrEmpty(handleStr))
        {
            var handle = WindowHandleParser.Parse(handleStr);
            window = _engine.ResolveWindow(handle);
        }
        else if (!string.IsNullOrEmpty(title))
        {
            window = _engine.GetAllTopLevelWindows()
                .FirstOrDefault(w => w.Title?.Contains(title, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (window is null)
            throw new InvalidOperationException("Window not found.");

        var closedHandle = $"{window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}";
        var closedTitle = window.Title;
        TargetContext.Set(window);
        TargetContext.Fallback = false;
        if (window.Patterns.Window.IsSupported) window.Close();
        else NativeInterop.CloseWindow(window.Properties.NativeWindowHandle.ValueOrDefault);

        return new WindowCloseResult(true, "Window closed.", closedHandle, closedTitle);
    }

    private ScreenshotResult DispatchScreenshot(Dictionary<string, string> args, AutomationElement window)
    {
        var output = args.GetValueOrDefault("output")
            ?? throw new InvalidOperationException("'output' is required for screenshot command.");

        AutomationElement target = window;
        var id = args.GetValueOrDefault("id");
        if (!string.IsNullOrEmpty(id))
            target = ResolveElement(window, id);

        return AutomationEngine.CaptureScreenshot(target, output);
    }

    private AutomationElement ResolveElement(AutomationElement window, string elementId)
    {
        return Commands.Elem.CommandHelper.ResolveElement(_engine, _sessionManager, _session, window, elementId)
            ?? throw new InvalidOperationException($"Element '{elementId}' not found in UI.");
    }

    private static string? ExtractField(JsonNode? result, string fieldName)
    {
        if (result is JsonObject json)
            return json.FirstOrDefault(property => property.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).Value?.ToString();
        return null;
    }
}
