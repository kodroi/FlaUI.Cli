using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
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
        Thread.Sleep(100);

        ScrollIntoView(element);
    }

    public static void Click(AutomationElement element, bool doubleClick = false, bool rightClick = false)
    {
        EnsureInteractable(element);

        // For single left-clicks, prefer UIA patterns over mouse simulation.
        // This avoids the "show desktop" race condition where FlaUI's
        // mouse-based Click() moves the cursor to cached screen coordinates
        // but the terminal steals focus before the click lands.
        if (!doubleClick && !rightClick)
        {
            if (element.Patterns.ExpandCollapse.IsSupported)
            {
                var pattern = element.Patterns.ExpandCollapse.Pattern;
                if (pattern.ExpandCollapseState.Value == FlaUI.Core.Definitions.ExpandCollapseState.Expanded)
                    pattern.Collapse();
                else
                    pattern.Expand();
                Thread.Sleep(100);
                return;
            }

            if (element.Patterns.Toggle.IsSupported)
            {
                element.Patterns.Toggle.Pattern.Toggle();
                Thread.Sleep(100);
                return;
            }

            // Use Invoke for clickable controls — more reliable than mouse simulation.
            // Skip toolbar buttons which may show tooltips instead of firing.
            if (element.Patterns.Invoke.IsSupported
                && element.ControlType is ControlType.Button
                    or ControlType.Hyperlink
                    or ControlType.SplitButton
                    or ControlType.MenuItem)
            {
                element.Patterns.Invoke.Pattern.Invoke();
                Thread.Sleep(100);
                return;
            }
        }

        FocusAndClick(element, doubleClick, rightClick);
        Thread.Sleep(100);
    }

    public void Type(AutomationElement element, string text)
    {
        if (element.Properties.ControlType.ValueOrDefault == ControlType.ComboBox)
        {
            Select(element, text);
            return;
        }

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

    public static void Clear(AutomationElement element)
    {
        EnsureInteractable(element);

        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(string.Empty);
        }
        else
        {
            element.Focus();
            Thread.Sleep(50);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Thread.Sleep(50);
            Keyboard.TypeSimultaneously(VirtualKeyShort.DELETE);
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
        }

        AutomationElement? itemElement = null;
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 2000)
        {
            Thread.Sleep(200);
            itemElement = element.FindFirstDescendant(
                _automation.ConditionFactory.ByName(item));
            if (itemElement is not null) break;
        }

        // Fallback: try ItemContainerPattern for virtualized items
        if (itemElement is null && element.Patterns.ItemContainer.IsSupported)
        {
            var container = element.Patterns.ItemContainer.Pattern;
            var virtualized = container.FindItemByProperty(null,
                _automation.PropertyLibrary.Element.Name, item);

            if (virtualized is not null)
            {
                if (virtualized.Patterns.VirtualizedItem.IsSupported)
                    virtualized.Patterns.VirtualizedItem.Pattern.Realize();

                Thread.Sleep(100);
                itemElement = virtualized;
            }
        }

        if (itemElement is null)
            throw new InvalidOperationException($"Item '{item}' not found in element.");

        if (itemElement.Patterns.SelectionItem.IsSupported)
        {
            itemElement.Patterns.SelectionItem.Pattern.Select();
        }
        else
        {
            FocusAndClick(itemElement);
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

        if (element.Patterns.Selection.IsSupported)
        {
            var selection = element.Patterns.Selection.Pattern.Selection.Value;
            if (selection.Length > 0)
                return selection[0].Name;
        }

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

    public static GetRangeResult GetRangeValue(AutomationElement element, string elementId)
    {
        if (!element.Patterns.RangeValue.IsSupported)
            throw new InvalidOperationException("Element does not support the RangeValue pattern.");

        var pattern = element.Patterns.RangeValue.Pattern;
        return new GetRangeResult(
            Success: true,
            Message: "Range value retrieved.",
            ElementId: elementId,
            Value: pattern.Value.Value,
            Minimum: pattern.Minimum.Value,
            Maximum: pattern.Maximum.Value,
            SmallChange: pattern.SmallChange.Value,
            LargeChange: pattern.LargeChange.Value);
    }

    public static void SetRangeValue(AutomationElement element, double value)
    {
        EnsureInteractable(element);

        if (!element.Patterns.RangeValue.IsSupported)
            throw new InvalidOperationException("Element does not support the RangeValue pattern.");

        var pattern = element.Patterns.RangeValue.Pattern;
        var min = pattern.Minimum.Value;
        var max = pattern.Maximum.Value;

        if (value < min || value > max)
            throw new InvalidOperationException($"Value {value} is out of range [{min}, {max}].");

        pattern.SetValue(value);
        Thread.Sleep(100);
    }

    public static GridInfoResult GetGridInfo(AutomationElement element, string elementId)
    {
        if (!element.Patterns.Grid.IsSupported)
            throw new InvalidOperationException("Element does not support the Grid pattern.");

        var grid = element.Patterns.Grid.Pattern;
        var rowCount = grid.RowCount.Value;
        var colCount = grid.ColumnCount.Value;

        string[]? headers = null;
        if (element.Patterns.Table.IsSupported)
        {
            var table = element.Patterns.Table.Pattern;
            var headerElements = table.ColumnHeaders.Value;
            headers = headerElements.Select(h => h.Name).ToArray();
        }

        return new GridInfoResult(
            Success: true,
            Message: "Grid info retrieved.",
            ElementId: elementId,
            RowCount: rowCount,
            ColumnCount: colCount,
            ColumnHeaders: headers);
    }

    public static GetCellResult GetGridCell(AutomationElement element, string elementId, int row, int column)
    {
        if (!element.Patterns.Grid.IsSupported)
            throw new InvalidOperationException("Element does not support the Grid pattern.");

        var grid = element.Patterns.Grid.Pattern;
        var cell = grid.GetItem(row, column);

        if (cell is null)
            throw new InvalidOperationException($"Cell at row {row}, column {column} not found.");

        var cellValue = GetValue(cell);

        return new GetCellResult(
            Success: true,
            Message: "Cell value retrieved.",
            ElementId: elementId,
            Row: row,
            Column: column,
            Value: cellValue);
    }

    public static GetTextResult GetText(AutomationElement element, string elementId)
    {
        if (!element.Patterns.Text.IsSupported)
            throw new InvalidOperationException("Element does not support the Text pattern.");

        var pattern = element.Patterns.Text.Pattern;
        var text = pattern.DocumentRange.GetText(-1);

        return new GetTextResult(
            Success: true,
            Message: "Text retrieved.",
            ElementId: elementId,
            Text: text);
    }

    public static ScrollInfoResult GetScrollInfo(AutomationElement element, string elementId)
    {
        if (!element.Patterns.Scroll.IsSupported)
            throw new InvalidOperationException("Element does not support the Scroll pattern.");

        var pattern = element.Patterns.Scroll.Pattern;
        return new ScrollInfoResult(
            Success: true,
            Message: "Scroll info retrieved.",
            ElementId: elementId,
            HorizontalPercent: pattern.HorizontalScrollPercent.Value,
            VerticalPercent: pattern.VerticalScrollPercent.Value,
            HorizontalViewSize: pattern.HorizontalViewSize.Value,
            VerticalViewSize: pattern.VerticalViewSize.Value,
            HorizontallyScrollable: pattern.HorizontallyScrollable.Value,
            VerticallyScrollable: pattern.VerticallyScrollable.Value);
    }

    public static void SetScroll(AutomationElement element, double? horizontal, double? vertical)
    {
        if (!element.Patterns.Scroll.IsSupported)
            throw new InvalidOperationException("Element does not support the Scroll pattern.");

        var pattern = element.Patterns.Scroll.Pattern;
        var h = horizontal ?? -1.0; // -1 = NoScroll (no change)
        var v = vertical ?? -1.0;
        pattern.SetScrollPercent(h, v);
        Thread.Sleep(100);
    }

    public static DockPositionResult GetDockPosition(AutomationElement element, string elementId)
    {
        if (!element.Patterns.Dock.IsSupported)
            throw new InvalidOperationException("Element does not support the Dock pattern.");

        var pattern = element.Patterns.Dock.Pattern;
        return new DockPositionResult(
            Success: true,
            Message: "Dock position retrieved.",
            ElementId: elementId,
            DockPosition: pattern.DockPosition.Value.ToString());
    }

    public static void SetDockPosition(AutomationElement element, DockPosition position)
    {
        if (!element.Patterns.Dock.IsSupported)
            throw new InvalidOperationException("Element does not support the Dock pattern.");

        element.Patterns.Dock.Pattern.SetDockPosition(position);
        Thread.Sleep(100);
    }

    public static GridItemInfoResult GetGridItemInfo(AutomationElement element, string elementId)
    {
        if (!element.Patterns.GridItem.IsSupported)
            throw new InvalidOperationException("Element does not support the GridItem pattern.");

        var pattern = element.Patterns.GridItem.Pattern;
        return new GridItemInfoResult(
            Success: true,
            Message: "Grid item info retrieved.",
            ElementId: elementId,
            Row: pattern.Row.Value,
            Column: pattern.Column.Value,
            RowSpan: pattern.RowSpan.Value,
            ColumnSpan: pattern.ColumnSpan.Value);
    }

    public static TableItemInfoResult GetTableItemInfo(AutomationElement element, string elementId)
    {
        if (!element.Patterns.TableItem.IsSupported)
            throw new InvalidOperationException("Element does not support the TableItem pattern.");

        var pattern = element.Patterns.TableItem.Pattern;
        var rowHeaders = pattern.RowHeaderItems.Value?.Select(h => h.Name).ToArray();
        var colHeaders = pattern.ColumnHeaderItems.Value?.Select(h => h.Name).ToArray();

        return new TableItemInfoResult(
            Success: true,
            Message: "Table item info retrieved.",
            ElementId: elementId,
            RowHeaders: rowHeaders,
            ColumnHeaders: colHeaders);
    }

    public static MultipleViewInfoResult GetMultipleViewInfo(AutomationElement element, string elementId)
    {
        if (!element.Patterns.MultipleView.IsSupported)
            throw new InvalidOperationException("Element does not support the MultipleView pattern.");

        var pattern = element.Patterns.MultipleView.Pattern;
        var currentViewId = pattern.CurrentView.Value;
        var supportedViewIds = pattern.SupportedViews.Value;
        var supportedViewNames = supportedViewIds?.Select(id => pattern.GetViewName(id)).ToArray();

        return new MultipleViewInfoResult(
            Success: true,
            Message: "View info retrieved.",
            ElementId: elementId,
            CurrentViewId: currentViewId,
            CurrentViewName: pattern.GetViewName(currentViewId),
            SupportedViewIds: supportedViewIds,
            SupportedViewNames: supportedViewNames);
    }

    public static void SetMultipleView(AutomationElement element, int viewId)
    {
        if (!element.Patterns.MultipleView.IsSupported)
            throw new InvalidOperationException("Element does not support the MultipleView pattern.");

        element.Patterns.MultipleView.Pattern.SetCurrentView(viewId);
        Thread.Sleep(100);
    }

    public static TransformResult TransformElement(AutomationElement element, string elementId,
        double? x, double? y, double? width, double? height, double? degrees)
    {
        if (!element.Patterns.Transform.IsSupported)
            throw new InvalidOperationException("Element does not support the Transform pattern.");

        var pattern = element.Patterns.Transform.Pattern;

        if (x.HasValue || y.HasValue)
        {
            if (!pattern.CanMove.Value)
                throw new InvalidOperationException("Element does not support moving.");
            pattern.Move(x ?? 0, y ?? 0);
        }

        if (width.HasValue || height.HasValue)
        {
            if (!pattern.CanResize.Value)
                throw new InvalidOperationException("Element does not support resizing.");
            pattern.Resize(width ?? 0, height ?? 0);
        }

        if (degrees.HasValue)
        {
            if (!pattern.CanRotate.Value)
                throw new InvalidOperationException("Element does not support rotation.");
            pattern.Rotate(degrees.Value);
        }

        Thread.Sleep(100);

        return new TransformResult(
            Success: true,
            Message: "Transform applied.",
            ElementId: elementId,
            CanMove: pattern.CanMove.Value,
            CanResize: pattern.CanResize.Value,
            CanRotate: pattern.CanRotate.Value);
    }

    public static WindowStateResult GetWindowState(AutomationElement window)
    {
        if (!window.Patterns.Window.IsSupported)
            throw new InvalidOperationException("Element does not support the Window pattern.");

        var pattern = window.Patterns.Window.Pattern;
        var handle = window.Properties.NativeWindowHandle.ValueOrDefault;

        return new WindowStateResult(
            Success: true,
            Message: "Window state retrieved.",
            Handle: $"{handle.ToInt64():X}",
            Title: window.Properties.Name.ValueOrDefault,
            VisualState: pattern.WindowVisualState.Value.ToString(),
            CanMaximize: pattern.CanMaximize.Value,
            CanMinimize: pattern.CanMinimize.Value,
            IsModal: pattern.IsModal.Value,
            IsTopmost: pattern.IsTopmost.Value);
    }

    public static void SetWindowVisualState(AutomationElement window, WindowVisualState state)
    {
        if (!window.Patterns.Window.IsSupported)
            throw new InvalidOperationException("Element does not support the Window pattern.");

        window.Patterns.Window.Pattern.SetWindowVisualState(state);
        Thread.Sleep(200);
    }

    public static TreeNode BuildTree(AutomationElement element, int maxDepth, SessionFile session)
    {
        return BuildTreeRecursive(element, 0, maxDepth, session);
    }

    public Window[] GetAllTopLevelWindows()
    {
        if (_application is null)
            throw new InvalidOperationException("No application attached.");

        var handles = NativeInterop.GetProcessWindowHandles(_application.ProcessId);
        var seen = new HashSet<IntPtr>();
        var windows = new List<Window>();

        foreach (var handle in handles)
        {
            if (!seen.Add(handle)) continue;

            try
            {
                var element = _automation.FromHandle(handle).AsWindow();
                if (element is not null)
                    windows.Add(element);
            }
            catch
            {
                // Handle may no longer be valid
            }
        }

        return windows.ToArray();
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

    public static void SendKeys(VirtualKeyShort[] keys, AutomationElement? target = null,
        AutomationElement? window = null)
    {
        if (target is not null)
        {
            if (!target.Properties.HasKeyboardFocus.ValueOrDefault)
            {
                EnsureInteractable(target);
                target.Focus();
                Thread.Sleep(50);
            }
            else
            {
                // Already focused — just ensure the window is foreground without disturbing selection state
                var parentWindow = GetParentWindow(target);
                if (parentWindow is not null)
                    EnsureWindowForeground(parentWindow);
            }
        }
        else if (window is not null)
        {
            EnsureWindowForeground(window);
        }

        Keyboard.TypeSimultaneously(keys);
        Thread.Sleep(100);
    }

    public static void Expand(AutomationElement element)
    {
        if (!element.Patterns.ExpandCollapse.IsSupported)
            throw new InvalidOperationException("Element does not support the ExpandCollapse pattern.");

        element.Patterns.ExpandCollapse.Pattern.Expand();
        Thread.Sleep(100);
    }

    public static void Collapse(AutomationElement element)
    {
        if (!element.Patterns.ExpandCollapse.IsSupported)
            throw new InvalidOperationException("Element does not support the ExpandCollapse pattern.");

        element.Patterns.ExpandCollapse.Pattern.Collapse();
        Thread.Sleep(100);
    }

    public static bool ScrollIntoView(AutomationElement element)
    {
        if (!element.Patterns.ScrollItem.IsSupported)
            return false;

        element.Patterns.ScrollItem.Pattern.ScrollIntoView();
        Thread.Sleep(100);
        return true;
    }

    public static void EnsureWindowForeground(AutomationElement window)
    {
        var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
        if (handle == IntPtr.Zero) return;

        // Prefer UIA Window pattern for restoring minimized windows
        if (NativeInterop.IsIconic(handle) && window.Patterns.Window.IsSupported)
            window.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Normal);

        NativeInterop.BringToFront(handle);
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
                foreach (var topWindow in GetAllTopLevelWindows())
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
                    FocusAndClick(menuItem);

                lastItem = menuItem;
            }
            else
            {
                if (menuItem.Patterns.ExpandCollapse.IsSupported)
                    menuItem.Patterns.ExpandCollapse.Pattern.Expand();
                else
                    FocusAndClick(menuItem);

                Thread.Sleep(200);
                currentScope = menuItem;
            }
        }

        return lastItem;
    }

    public static ScreenshotResult CaptureScreenshot(AutomationElement element, string outputPath)
    {
        using var image = Capture.Element(element);
        var fullPath = Path.GetFullPath(outputPath);
        image.ToFile(fullPath);
        return new ScreenshotResult(true, "Screenshot saved.", fullPath,
            image.Bitmap.Width, image.Bitmap.Height);
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

    private static void FocusAndClick(AutomationElement element, bool doubleClick = false, bool rightClick = false)
    {
        var window = GetParentWindow(element);
        if (window is not null)
        {
            var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
            if (handle != IntPtr.Zero)
                NativeInterop.BringToFront(handle);
        }

        if (doubleClick)
            element.DoubleClick();
        else if (rightClick)
            element.RightClick();
        else
            element.Click();
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
