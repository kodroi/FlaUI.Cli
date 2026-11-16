using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Services;

public record SelectorResult(AutomationElement Element, SelectorQuality Quality, string Strategy);

public class SelectorResolver
{
    private const int RetryIntervalMs = 100;
    private readonly ConditionFactory _cf;

    public SelectorResolver(ConditionFactory conditionFactory)
    {
        _cf = conditionFactory;
    }

    public SelectorResult? Resolve(
        AutomationElement parent,
        string? automationId = null,
        string? name = null,
        string? controlType = null,
        string? className = null,
        int timeoutMs = 10000)
    {
        foreach (var (condition, quality, strategy) in GetSelectors(automationId, name, controlType, className))
        {
            var element = FindWithTimeout(parent, condition, timeoutMs);
            if (element is not null)
                return new SelectorResult(element, quality, strategy);
        }

        return null;
    }

    internal (SelectorResult Result, AutomationElement Window)? ResolveAcrossWindows(
        Func<IEnumerable<AutomationElement>> getWindows,
        Stopwatch stopwatch,
        int timeoutMs,
        string? automationId,
        string? name,
        string? controlType,
        string? className)
    {
        var selectors = GetSelectors(automationId, name, controlType, className).ToArray();
        if (selectors.Length == 0) return null;

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            foreach (var window in getWindows())
            {
                foreach (var (condition, quality, strategy) in selectors)
                {
                    if (stopwatch.ElapsedMilliseconds >= timeoutMs) return null;

                    var element = window.FindFirstDescendant(condition);
                    if (stopwatch.ElapsedMilliseconds >= timeoutMs) return null;
                    if (element is not null)
                        return (new SelectorResult(element, quality, strategy), window);
                }
            }

            var remaining = timeoutMs - stopwatch.ElapsedMilliseconds;
            if (remaining > 0)
                Thread.Sleep((int)Math.Min(remaining, RetryIntervalMs));
        }

        return null;
    }

    private IEnumerable<(ConditionBase Condition, SelectorQuality Quality, string Strategy)> GetSelectors(
        string? automationId, string? name, string? controlType, string? className)
    {
        if (!string.IsNullOrEmpty(automationId))
            yield return (_cf.ByAutomationId(automationId), SelectorQuality.Stable, "AutomationId");

        var hasControlType = Enum.TryParse<ControlType>(controlType, true, out var parsedType);
        if (!string.IsNullOrEmpty(name) && hasControlType)
            yield return (_cf.ByName(name).And(_cf.ByControlType(parsedType)), SelectorQuality.Acceptable, "Name+ControlType");

        if (!string.IsNullOrEmpty(name))
            yield return (_cf.ByName(name), SelectorQuality.Acceptable, "Name");

        if (!string.IsNullOrEmpty(className) && hasControlType)
            yield return (_cf.ByControlType(parsedType).And(_cf.ByClassName(className)), SelectorQuality.Fragile, "ControlType+ClassName");

        if (!string.IsNullOrEmpty(className))
            yield return (_cf.ByClassName(className), SelectorQuality.Fragile, "ClassName");
    }

    private static AutomationElement? FindWithTimeout(AutomationElement parent, ConditionBase condition, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var element = parent.FindFirstDescendant(condition);
            if (element is not null)
                return element;

            Thread.Sleep(RetryIntervalMs);
        }

        return null;
    }
}
