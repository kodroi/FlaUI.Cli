using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Services;

public class AutomationEngine : IDisposable
{
    private readonly UIA3Automation _automation;
    private Application? _application;

    public AutomationEngine()
    {
        _automation = new UIA3Automation();
    }

    public SelectorResolver CreateSelectorResolver()
    {
        return new SelectorResolver(_automation.ConditionFactory);
    }

    public Application Launch(string path, string? args = null)
    {
        var psi = new ProcessStartInfo(path);
        if (!string.IsNullOrEmpty(args))
            psi.Arguments = args;

        _application = Application.Launch(psi);
        return _application;
    }

    public Application Attach(int pid)
    {
        _application = Application.Attach(pid);
        return _application;
    }

    public Application AttachByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
            throw new InvalidOperationException($"No process found with name '{processName}'.");

        _application = Application.Attach(processes[0].Id);
        return _application;
    }

    public Application AttachByTitle(string title)
    {
        var processes = Process.GetProcesses();
        var match = processes.FirstOrDefault(p =>
        {
            try { return p.MainWindowTitle.Contains(title, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        });

        if (match is null)
            throw new InvalidOperationException($"No process found with window title containing '{title}'.");

        _application = Application.Attach(match.Id);
        return _application;
    }

    public (Application App, AutomationElement MainWindow) ReattachFromSession(SessionFile session)
    {
        if (!IsProcessAlive(session.Application.Pid))
            throw new InvalidOperationException($"Process {session.Application.Pid} is no longer running.");

        _application = Application.Attach(session.Application.Pid);

        var hwnd = new IntPtr(session.Application.MainWindowHandle);
        if (!NativeInterop.IsWindow(hwnd))
        {
            // Re-discover main window
            var mainWindow = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(10));
            if (mainWindow is null)
                throw new InvalidOperationException("Could not find main window after reattachment.");

            session.Application.MainWindowHandle = mainWindow.Properties.NativeWindowHandle.ValueOrDefault.ToInt64();
            session.Application.MainWindowTitle = mainWindow.Properties.Name.ValueOrDefault;
            return (_application, mainWindow);
        }

        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(10));
        if (window is null)
            throw new InvalidOperationException("Could not find main window.");

        return (_application, window);
    }

    public AutomationElement GetMainWindow(TimeSpan? timeout = null)
    {
        if (_application is null)
            throw new InvalidOperationException("No application attached.");

        var window = _application.GetMainWindow(_automation, timeout ?? TimeSpan.FromSeconds(10));
        return window ?? throw new InvalidOperationException("Could not find main window.");
    }

    public static void EnsureInteractable(AutomationElement element)
    {
        var window = GetParentWindow(element);
        if (window is null) return;

        var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
        if (handle == IntPtr.Zero) return;

        NativeInterop.BringToFront(handle);
        window.Focus();
        Thread.Sleep(100);
    }

    public static void Click(AutomationElement element, bool doubleClick = false, bool rightClick = false)
    {
        EnsureInteractable(element);
        if (doubleClick)
            element.DoubleClick();
        else if (rightClick)
            element.RightClick();
        else
            element.Click();
        Thread.Sleep(100);
    }

    public static void Type(AutomationElement element, string text)
    {
        EnsureInteractable(element);
        element.Focus();
        Thread.Sleep(50);

        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(text);
        }
        else
        {
            element.AsTextBox().Text = text;
        }

        Thread.Sleep(100);
    }

    public static void SetValue(AutomationElement element, string value)
    {
        EnsureInteractable(element);

        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
        }
        else
        {
            throw new InvalidOperationException("Element does not support the Value pattern.");
        }

        Thread.Sleep(100);
    }

    public void Select(AutomationElement element, string item)
    {
        EnsureInteractable(element);

        if (element.Patterns.ExpandCollapse.IsSupported)
        {
            element.Patterns.ExpandCollapse.Pattern.Expand();
            Thread.Sleep(200);
        }

        var itemElement = element.FindFirstDescendant(
            _automation.ConditionFactory.ByName(item));

        if (itemElement is null)
            throw new InvalidOperationException($"Item '{item}' not found in element.");

        if (itemElement.Patterns.SelectionItem.IsSupported)
        {
            itemElement.Patterns.SelectionItem.Pattern.Select();
        }
        else
        {
            itemElement.Click();
        }

        if (element.Patterns.ExpandCollapse.IsSupported)
        {
            element.Patterns.ExpandCollapse.Pattern.Collapse();
        }

        Thread.Sleep(100);
    }

    public static string? GetValue(AutomationElement element)
    {
        if (element.Patterns.Value.IsSupported)
            return element.Patterns.Value.Pattern.Value.Value;

        try { return element.AsTextBox().Text; }
        catch { /* not a textbox */ }

        try { return element.AsLabel().Text; }
        catch { /* not a label */ }

        return element.Name;
    }

    public static GetStateResult GetState(AutomationElement element, string elementId)
    {
        string? toggleState = null;
        if (element.Patterns.Toggle.IsSupported)
            toggleState = element.Patterns.Toggle.Pattern.ToggleState.Value.ToString();

        string? expandState = null;
        if (element.Patterns.ExpandCollapse.IsSupported)
            expandState = element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value.ToString();

        return new GetStateResult(
            Success: true,
            Message: "State retrieved.",
            ElementId: elementId,
            IsEnabled: element.IsEnabled,
            IsOffscreen: element.IsOffscreen,
            IsVisible: !element.IsOffscreen,
            HasFocus: element.Properties.HasKeyboardFocus.ValueOrDefault,
            ToggleState: toggleState,
            ExpandState: expandState);
    }

    public static TreeNode BuildTree(AutomationElement element, int maxDepth, SessionFile session)
    {
        return BuildTreeRecursive(element, 0, maxDepth, session);
    }

    public Window[] GetAllTopLevelWindows()
    {
        if (_application is null)
            throw new InvalidOperationException("No application attached.");

        return _application.GetAllTopLevelWindows(_automation);
    }

    public Window? GetWindowByHandle(long handle)
    {
        var windows = GetAllTopLevelWindows();
        return windows.FirstOrDefault(w =>
            w.Properties.NativeWindowHandle.ValueOrDefault.ToInt64() == handle);
    }

    public AutomationElement ResolveWindow(long? windowHandle)
    {
        if (windowHandle is null)
            return GetMainWindow();

        var window = GetWindowByHandle(windowHandle.Value);
        if (window is null)
            throw new InvalidOperationException($"Window with handle 0x{windowHandle.Value:X} not found.");

        return window;
    }

    public static void SendKeys(VirtualKeyShort[] keys, AutomationElement? target = null)
    {
        if (target is not null)
        {
            EnsureInteractable(target);
            target.Focus();
            Thread.Sleep(50);
        }

        Keyboard.TypeSimultaneously(keys);
        Thread.Sleep(100);
    }

    public AutomationElement? NavigateMenu(AutomationElement window, string[] pathSegments)
    {
        AutomationElement? currentScope = window;
        AutomationElement? lastItem = null;

        // Find MenuBar first
        var menuBar = window.FindFirstDescendant(
            _automation.ConditionFactory.ByControlType(ControlType.MenuBar));

        if (menuBar is not null)
            currentScope = menuBar;

        for (int i = 0; i < pathSegments.Length; i++)
        {
            var segmentName = pathSegments[i].Trim();
            var isLast = i == pathSegments.Length - 1;

            var menuItem = FindMenuItem(currentScope!, segmentName);

            // If not found in current scope, search all top-level windows (popup menus)
            if (menuItem is null && _application is not null)
            {
                foreach (var topWindow in _application.GetAllTopLevelWindows(_automation))
                {
                    menuItem = FindMenuItem(topWindow, segmentName);
                    if (menuItem is not null) break;
                }
            }

            if (menuItem is null)
                throw new InvalidOperationException($"Menu item '{segmentName}' not found.");

            if (isLast)
            {
                if (menuItem.Patterns.Invoke.IsSupported)
                    menuItem.Patterns.Invoke.Pattern.Invoke();
                else
                    menuItem.Click();

                lastItem = menuItem;
            }
            else
            {
                if (menuItem.Patterns.ExpandCollapse.IsSupported)
                    menuItem.Patterns.ExpandCollapse.Pattern.Expand();
                else
                    menuItem.Click();

                Thread.Sleep(200);
                currentScope = menuItem;
            }
        }

        return lastItem;
    }

    public void CloseApplication(bool force = false)
    {
        if (_application is null) return;

        if (force)
            _application.Kill();
        else
            _application.Close();

        _application = null;
    }

    public void Dispose()
    {
        _application?.Dispose();
        _automation.Dispose();
        GC.SuppressFinalize(this);
    }

    private static TreeNode BuildTreeRecursive(AutomationElement element, int depth, int maxDepth, SessionFile session)
    {
        var elementId = GenerateElementId();
        var entry = new ElementEntry
        {
            AutomationId = element.Properties.AutomationId.ValueOrDefault,
            Name = element.Properties.Name.ValueOrDefault,
            ControlType = element.Properties.ControlType.ValueOrDefault.ToString(),
            ClassName = element.Properties.ClassName.ValueOrDefault,
            RuntimeId = element.Properties.RuntimeId.ValueOrDefault,
            SelectorQuality = ClassifyElement(element),
            LastVerified = DateTime.UtcNow
        };
        session.Elements[elementId] = entry;

        var node = new TreeNode
        {
            ElementId = elementId,
            AutomationId = entry.AutomationId,
            Name = entry.Name,
            ControlType = entry.ControlType,
            ClassName = entry.ClassName
        };

        if (depth >= maxDepth) return node;

        try
        {
            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                node.Children.Add(BuildTreeRecursive(child, depth + 1, maxDepth, session));
            }
        }
        catch
        {
            // Some elements don't support children enumeration
        }

        return node;
    }

    private static SelectorQuality ClassifyElement(AutomationElement element)
    {
        var aid = element.Properties.AutomationId.ValueOrDefault;
        if (!string.IsNullOrEmpty(aid))
            return SelectorQuality.Stable;

        var name = element.Properties.Name.ValueOrDefault;
        var ct = element.Properties.ControlType.ValueOrDefault;
        if (!string.IsNullOrEmpty(name) && ct != ControlType.Custom)
            return SelectorQuality.Acceptable;

        if (!string.IsNullOrEmpty(name))
            return SelectorQuality.Acceptable;

        var className = element.Properties.ClassName.ValueOrDefault;
        if (!string.IsNullOrEmpty(className))
            return SelectorQuality.Fragile;

        return SelectorQuality.Unresolvable;
    }

    private static string GenerateElementId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    private static AutomationElement? GetParentWindow(AutomationElement element)
    {
        var current = element;
        while (current is not null)
        {
            if (current.Properties.ControlType.ValueOrDefault == ControlType.Window)
                return current;

            try { current = current.Parent; }
            catch { return null; }
        }

        return null;
    }

    private AutomationElement? FindMenuItem(AutomationElement scope, string name)
    {
        var condition = _automation.ConditionFactory.ByControlType(ControlType.MenuItem)
            .And(_automation.ConditionFactory.ByName(name));

        return scope.FindFirstDescendant(condition);
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
