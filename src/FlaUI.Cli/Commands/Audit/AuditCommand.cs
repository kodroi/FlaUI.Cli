using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Audit;

public static class AuditCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var windowOption = new Option<string?>("--window") { Description = "Window element ID to audit" };
        var recordingOption = new Option<bool>("--recording") { Description = "Audit recording steps instead of live elements" };
        var outOption = new Option<string?>("--out") { Description = "Output file path" };

        var command = new Command("audit", "Audit selector quality and missing AutomationIds");
        command.Add(windowOption);
        command.Add(recordingOption);
        command.Add(outOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var windowId = parseResult.GetValue(windowOption);
            var auditRecording = parseResult.GetValue(recordingOption);
            var outPath = parseResult.GetValue(outOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);

                AuditResult result;

                if (auditRecording)
                {
                    result = AuditRecordingSteps(session);
                }
                else
                {
                    var (_, mainWindow) = engine.ReattachFromSession(session);
                    AutomationElement root = mainWindow;

                    if (!string.IsNullOrEmpty(windowId))
                    {
                        var resolved = Elem.CommandHelper.ResolveElement(
                            engine, sessionManager, session, mainWindow, windowId);
                        if (resolved is not null) root = resolved;
                    }

                    result = AuditLiveElements(root);
                }

                if (!string.IsNullOrEmpty(outPath))
                    File.WriteAllText(Path.GetFullPath(outPath), JsonOutput.Serialize(result));

                JsonOutput.Write(result);
                Environment.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static AuditResult AuditLiveElements(AutomationElement root)
    {
        var allElements = new List<AutomationElement>();
        CollectElements(root, allElements, 0, 10);

        var withAid = 0;
        var withoutAid = 0;
        var distribution = new Dictionary<string, int>
        {
            ["stable"] = 0, ["acceptable"] = 0, ["fragile"] = 0, ["unresolvable"] = 0
        };
        var issues = new List<AuditIssue>();

        foreach (var element in allElements)
        {
            var aid = element.Properties.AutomationId.ValueOrDefault;
            var name = element.Properties.Name.ValueOrDefault;
            var ct = element.Properties.ControlType.ValueOrDefault;
            var className = element.Properties.ClassName.ValueOrDefault;

            if (!string.IsNullOrEmpty(aid))
            {
                withAid++;
                distribution["stable"]++;
            }
            else if (!string.IsNullOrEmpty(name))
            {
                withoutAid++;
                distribution["acceptable"]++;
                if (IsInteractiveControl(ct))
                {
                    issues.Add(new AuditIssue(
                        name, ct.ToString(), className, SelectorQuality.Acceptable,
                        "Interactive control missing AutomationId."));
                }
            }
            else if (!string.IsNullOrEmpty(className))
            {
                withoutAid++;
                distribution["fragile"]++;
                if (IsInteractiveControl(ct))
                {
                    issues.Add(new AuditIssue(
                        name, ct.ToString(), className, SelectorQuality.Fragile,
                        "Interactive control missing AutomationId and Name."));
                }
            }
            else
            {
                withoutAid++;
                distribution["unresolvable"]++;
            }
        }

        return new AuditResult(
            Success: true,
            Message: "Audit complete.",
            TotalElements: allElements.Count,
            WithAutomationId: withAid,
            WithoutAutomationId: withoutAid,
            SelectorDistribution: distribution,
            Issues: issues);
    }

    private static AuditResult AuditRecordingSteps(SessionFile session)
    {
        if (session.Recording is null)
            return new AuditResult(true, "No recording.", 0, 0, 0, null, null);

        var distribution = new Dictionary<string, int>
        {
            ["stable"] = 0, ["acceptable"] = 0, ["fragile"] = 0, ["unresolvable"] = 0
        };
        var issues = new List<AuditIssue>();

        foreach (var step in session.Recording.Steps.Where(s => s.Included && s.Target is not null))
        {
            var quality = step.Target!.SelectorQuality.ToString().ToLowerInvariant();
            distribution[quality] = distribution.GetValueOrDefault(quality) + 1;

            if (step.Target.SelectorQuality != SelectorQuality.Stable)
            {
                issues.Add(new AuditIssue(
                    step.Target.Name, step.Target.ControlType, null,
                    step.Target.SelectorQuality,
                    $"Step {step.Seq} ({step.Command}) uses {step.Target.SelectorQuality} selector."));
            }
        }

        var total = session.Recording.Steps.Count(s => s.Included);

        return new AuditResult(
            Success: true,
            Message: "Recording audit complete.",
            TotalElements: total,
            WithAutomationId: distribution["stable"],
            WithoutAutomationId: total - distribution["stable"],
            SelectorDistribution: distribution,
            Issues: issues);
    }

    private static void CollectElements(AutomationElement element, List<AutomationElement> list, int depth, int maxDepth)
    {
        list.Add(element);
        if (depth >= maxDepth) return;

        try
        {
            foreach (var child in element.FindAllChildren())
                CollectElements(child, list, depth + 1, maxDepth);
        }
        catch
        {
            // Some elements don't support children enumeration
        }
    }

    private static bool IsInteractiveControl(ControlType ct)
    {
        return ct is ControlType.Button or ControlType.CheckBox or ControlType.ComboBox
            or ControlType.Edit or ControlType.List or ControlType.ListItem
            or ControlType.RadioButton or ControlType.Tab or ControlType.TabItem
            or ControlType.Tree or ControlType.TreeItem;
    }
}
