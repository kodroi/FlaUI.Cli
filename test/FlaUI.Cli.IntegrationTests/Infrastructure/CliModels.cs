using System.Text.Json.Serialization;

namespace FlaUI.Cli.IntegrationTests.Infrastructure;

public record SessionNewResult(
    bool Success,
    string? Message,
    string? SessionFile,
    int Pid,
    string? ProcessName,
    string? MainWindowTitle);

public record SessionAttachResult(
    bool Success,
    string? Message,
    string? SessionFile,
    int Pid,
    string? ProcessName,
    string? MainWindowTitle);

public record SessionStatusResult(
    bool Success,
    string? Message,
    int Pid,
    bool ProcessAlive,
    bool WindowValid,
    int ElementCount,
    bool Recording,
    string? MainWindowTitle = null,
    string? MainWindowHandle = null);

public record ElementFindResult(
    bool Success,
    string? Message,
    string? ElementId,
    string? AutomationId,
    string? Name,
    string? ControlType,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SelectorQuality? SelectorQuality,
    string? SelectorStrategy,
    BoundsInfo? Bounds,
    string? WindowHandle);

public record BoundsInfo(double X, double Y, double Width, double Height);

public record ElementTreeResult(
    bool Success,
    string? Message,
    TreeNode? Root);

public class TreeNode
{
    public string? ElementId { get; set; }
    public string? AutomationId { get; set; }
    public string? Name { get; set; }
    public string? ControlType { get; set; }
    public string? ClassName { get; set; }
    public List<TreeNode> Children { get; set; } = [];
}

public record ElementPropsResult(
    bool Success,
    string? Message,
    string? ElementId,
    string? AutomationId,
    string? Name,
    string? ControlType,
    string? ClassName,
    BoundsInfo? Bounds,
    bool IsEnabled,
    bool IsOffscreen,
    int[]? RuntimeId,
    string? HelpText,
    string? AcceleratorKey);

public record ActionResult(
    bool Success,
    string? Message,
    string? ElementId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SelectorQuality? SelectorQuality);

public record GetValueResult(
    bool Success,
    string? Message,
    string? ElementId,
    string? Value,
    string? SavedAs);

public record GetStateResult(
    bool Success,
    string? Message,
    string? ElementId,
    bool IsEnabled,
    bool IsOffscreen,
    bool IsVisible,
    bool HasFocus,
    string? ToggleState,
    string? ExpandState);

public record WaitResult(
    bool Success,
    string? Message,
    long Elapsed,
    string? WindowHandle = null,
    string? WindowTitle = null);

public record AuditResult(
    bool Success,
    string? Message,
    int TotalElements,
    int WithAutomationId,
    int WithoutAutomationId,
    Dictionary<string, int>? SelectorDistribution,
    List<AuditIssue>? Issues);

public record AuditIssue(
    string? Name,
    string? ControlType,
    string? ClassName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SelectorQuality SelectorQuality,
    string? Recommendation);

public record WindowInfoItem(
    string? Handle,
    string? Title,
    bool IsModal,
    string? ClassName,
    BoundsInfo? Bounds);

public record WindowListResult(
    bool Success,
    string? Message,
    List<WindowInfoItem>? Windows);

public record WindowFocusResult(
    bool Success,
    string? Message,
    string? Handle,
    string? Title);

public record WindowCloseResult(
    bool Success,
    string? Message,
    string? Handle,
    string? Title);

public record KeysResult(
    bool Success,
    string? Message,
    string? Keys,
    string? ElementId);

public record MenuResult(
    bool Success,
    string? Message,
    string? Path,
    string? ClickedItemName);

public record BatchResult(
    bool Success,
    string? Message,
    int TotalSteps,
    int Succeeded,
    int Failed,
    List<BatchStepResult>? Steps);

public record BatchStepResult(
    int Index,
    string? Command,
    bool Success,
    string? Message,
    object? Result);

public record ScreenshotResult(
    bool Success,
    string? Message,
    string? OutputPath,
    int Width,
    int Height);

public record ScrollIntoViewResult(
    bool Success,
    string? Message,
    string? ElementId,
    bool Scrolled);

public record GetRangeResult(
    bool Success,
    string? Message,
    string? ElementId,
    double Value,
    double Minimum,
    double Maximum,
    double SmallChange,
    double LargeChange);

public record GridInfoResult(
    bool Success,
    string? Message,
    string? ElementId,
    int RowCount,
    int ColumnCount,
    string[]? ColumnHeaders);

public record GetCellResult(
    bool Success,
    string? Message,
    string? ElementId,
    int Row,
    int Column,
    string? Value);

public record GetTextResult(
    bool Success,
    string? Message,
    string? ElementId,
    string? Text);

public record ScrollInfoResult(
    bool Success,
    string? Message,
    string? ElementId,
    double HorizontalPercent,
    double VerticalPercent,
    double HorizontalViewSize,
    double VerticalViewSize,
    bool HorizontallyScrollable,
    bool VerticallyScrollable);

public record DockPositionResult(
    bool Success,
    string? Message,
    string? ElementId,
    string? DockPosition);

public record GridItemInfoResult(
    bool Success,
    string? Message,
    string? ElementId,
    int Row,
    int Column,
    int RowSpan,
    int ColumnSpan);

public record TableItemInfoResult(
    bool Success,
    string? Message,
    string? ElementId,
    string[]? RowHeaders,
    string[]? ColumnHeaders);

public record MultipleViewInfoResult(
    bool Success,
    string? Message,
    string? ElementId,
    int CurrentViewId,
    string? CurrentViewName,
    int[]? SupportedViewIds,
    string[]? SupportedViewNames);

public record TransformResult(
    bool Success,
    string? Message,
    string? ElementId,
    bool CanMove,
    bool CanResize,
    bool CanRotate);

public record WindowStateResult(
    bool Success,
    string? Message,
    string? Handle,
    string? Title,
    string? VisualState,
    bool CanMaximize,
    bool CanMinimize,
    bool IsModal,
    bool IsTopmost);

public record ErrorResult(
    bool Success,
    string? Message);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SelectorQuality
{
    Stable,
    Acceptable,
    Fragile,
    Unresolvable
}
