using System.Text.RegularExpressions;
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
    private readonly AutomationElement _mainWindow;
    private readonly List<object?> _stepResults = [];

    public BatchExecutor(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        string sessionPath,
        AutomationElement mainWindow)
    {
        _engine = engine;
        _sessionManager = sessionManager;
        _session = session;
        _sessionPath = sessionPath;
        _mainWindow = mainWindow;
    }

    public BatchResult Execute(List<BatchStep> steps, bool continueOnError)
    {
        var results = new List<BatchStepResult>();
        var succeeded = 0;
        var failed = 0;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var resolvedArgs = ResolveReferences(step.Args);

            try
            {
                var result = Dispatch(step.Cmd, resolvedArgs);
                _stepResults.Add(result);
                results.Add(new BatchStepResult(i, step.Cmd, true, "OK", result));
                succeeded++;
            }
            catch (Exception ex)
            {
                _stepResults.Add(null);
                results.Add(new BatchStepResult(i, step.Cmd, false, ex.Message, null));
                failed++;

                if (!continueOnError)
                    break;
            }
        }

        _sessionManager.Save(_sessionPath, _session);

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
        var windowHandle = args.GetValueOrDefault("window");
        var targetWindow = !string.IsNullOrEmpty(windowHandle)
            ? _engine.ResolveWindow(long.Parse(windowHandle, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture))
            : _mainWindow;

        return cmd switch
        {
            "elem find" => DispatchFind(args, targetWindow),
            "elem click" => DispatchClick(args, targetWindow),
            "elem type" => DispatchType(args, targetWindow),
            "elem select" => DispatchSelect(args, targetWindow),
            "elem set-value" => DispatchSetValue(args, targetWindow),
            "elem get-value" => DispatchGetValue(args, targetWindow),
            "elem get-state" => DispatchGetState(args, targetWindow),
            "elem keys" => DispatchKeys(args, targetWindow),
            "elem menu" => DispatchMenu(args, targetWindow),
            "window list" => DispatchWindowList(),
            "window focus" => DispatchWindowFocus(args),
            "window close" => DispatchWindowClose(args),
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

        SessionManager.AddElement(_session, elementId, new ElementEntry
        {
            AutomationId = element.Properties.AutomationId.ValueOrDefault,
            Name = element.Properties.Name.ValueOrDefault,
            ControlType = element.Properties.ControlType.ValueOrDefault.ToString(),
            ClassName = element.Properties.ClassName.ValueOrDefault,
            RuntimeId = element.Properties.RuntimeId.ValueOrDefault,
            SelectorQuality = result.Quality,
            LastVerified = DateTime.UtcNow
        });

        return new ElementFindResult(true, "Element found.", elementId,
            element.Properties.AutomationId.ValueOrDefault,
            element.Properties.Name.ValueOrDefault,
            element.Properties.ControlType.ValueOrDefault.ToString(),
            result.Quality, result.Strategy,
            new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height));
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

    private ActionResult DispatchType(Dictionary<string, string> args, AutomationElement window)
    {
        var id = args["id"];
        var element = ResolveElement(window, id);
        AutomationEngine.Type(element, args["text"]);

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
        AutomationEngine.SendKeys(keys, target);

        return new KeysResult(true, $"Keys '{keysStr}' sent.", keysStr, id);
    }

    private MenuResult DispatchMenu(Dictionary<string, string> args, AutomationElement window)
    {
        var path = args["path"];
        var segments = path.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var clickedItem = _engine.NavigateMenu(window, segments);
        var clickedName = clickedItem?.Properties.Name.ValueOrDefault;

        return new MenuResult(true, $"Menu item '{clickedName}' clicked.", path, clickedName);
    }

    private WindowListResult DispatchWindowList()
    {
        var windows = _engine.GetAllTopLevelWindows();
        var items = windows.Select(w =>
        {
            var bounds = w.BoundingRectangle;
            return new WindowInfoItem(
                $"{w.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}",
                w.Title, w.IsModal,
                w.Properties.ClassName.ValueOrDefault,
                new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }).ToList();

        return new WindowListResult(true, $"Found {items.Count} window(s).", items);
    }

    private WindowFocusResult DispatchWindowFocus(Dictionary<string, string> args)
    {
        var handle = long.Parse(args["handle"], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
        var window = _engine.GetWindowByHandle(handle)
            ?? throw new InvalidOperationException($"Window with handle 0x{args["handle"]} not found.");

        var hwnd = window.Properties.NativeWindowHandle.ValueOrDefault;
        NativeInterop.BringToFront(hwnd);
        window.Focus();

        return new WindowFocusResult(true, "Window focused.", args["handle"], window.Title);
    }

    private WindowCloseResult DispatchWindowClose(Dictionary<string, string> args)
    {
        FlaUI.Core.AutomationElements.Window? window = null;

        var handleStr = args.GetValueOrDefault("handle");
        var title = args.GetValueOrDefault("title");

        if (!string.IsNullOrEmpty(handleStr))
        {
            var handle = long.Parse(handleStr, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
            window = _engine.GetWindowByHandle(handle);
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
        window.Close();

        return new WindowCloseResult(true, "Window closed.", closedHandle, closedTitle);
    }

    private AutomationElement ResolveElement(AutomationElement window, string elementId)
    {
        var entry = SessionManager.GetElement(_session, elementId)
            ?? throw new InvalidOperationException($"Element '{elementId}' not found in session.");

        var resolver = _engine.CreateSelectorResolver();
        var result = resolver.Resolve(window, entry.AutomationId, entry.Name,
            entry.ControlType, entry.ClassName, 5000);

        return result?.Element
            ?? throw new InvalidOperationException($"Element '{elementId}' not found in UI.");
    }

    private static string? ExtractField(object? result, string fieldName)
    {
        if (result is null) return null;

        var prop = result.GetType().GetProperty(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

        return prop?.GetValue(result)?.ToString();
    }
}
