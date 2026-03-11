using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Services;

public record SelectorResult(AutomationElement Element, SelectorQuality Quality, string Strategy);

public class SelectorResolver
{
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
        // Priority 1: AutomationId
        if (!string.IsNullOrEmpty(automationId))
        {
            var element = FindWithTimeout(parent, _cf.ByAutomationId(automationId), timeoutMs);
            if (element is not null)
                return new SelectorResult(element, SelectorQuality.Stable, "AutomationId");
        }

        // Priority 2: Name + ControlType
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(controlType)
            && Enum.TryParse<ControlType>(controlType, true, out var ct))
        {
            var condition = _cf.ByName(name).And(_cf.ByControlType(ct));
            var element = FindWithTimeout(parent, condition, timeoutMs);
            if (element is not null)
                return new SelectorResult(element, SelectorQuality.Acceptable, "Name+ControlType");
        }

        // Priority 3: Name alone
        if (!string.IsNullOrEmpty(name))
        {
            var element = FindWithTimeout(parent, _cf.ByName(name), timeoutMs);
            if (element is not null)
                return new SelectorResult(element, SelectorQuality.Acceptable, "Name");
        }

        // Priority 4: ControlType + ClassName
        if (!string.IsNullOrEmpty(controlType) && !string.IsNullOrEmpty(className)
            && Enum.TryParse<ControlType>(controlType, true, out var ct2))
        {
            var condition = _cf.ByControlType(ct2).And(_cf.ByClassName(className));
            var element = FindWithTimeout(parent, condition, timeoutMs);
            if (element is not null)
                return new SelectorResult(element, SelectorQuality.Fragile, "ControlType+ClassName");
        }

        // Priority 5: ClassName alone
        if (!string.IsNullOrEmpty(className))
        {
            var element = FindWithTimeout(parent, _cf.ByClassName(className), timeoutMs);
            if (element is not null)
                return new SelectorResult(element, SelectorQuality.Fragile, "ClassName");
        }

        // Priority 6: Unresolvable
        return null;
    }

    public static int CheckPolicy(SelectorQuality quality, string policy)
    {
        return policy.ToLowerInvariant() switch
        {
            "stable" when quality != SelectorQuality.Stable => ExitCodes.SelectorPolicyViolation,
            "acceptable" when quality is not (SelectorQuality.Stable or SelectorQuality.Acceptable) =>
                ExitCodes.SelectorPolicyViolation,
            "fragile" when quality == SelectorQuality.Unresolvable => ExitCodes.Unresolvable,
            _ => ExitCodes.Success
        };
    }

    private static AutomationElement? FindWithTimeout(AutomationElement parent, ConditionBase condition, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var element = parent.FindFirstDescendant(condition);
            if (element is not null)
                return element;

            Thread.Sleep(100);
        }

        return null;
    }
}
