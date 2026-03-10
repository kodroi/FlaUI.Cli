using FlaUI.Cli.Models;

namespace FlaUI.Cli.Services;

public class RecordingService
{
    public void Start(SessionFile session, string? description)
    {
        session.Recording = new RecordingState
        {
            Active = true,
            StartedAt = DateTime.UtcNow,
            Description = description,
            Steps = []
        };
    }

    public void Stop(SessionFile session)
    {
        if (session.Recording is null)
            throw new InvalidOperationException("No recording is active.");

        session.Recording.Active = false;
    }

    public void AddStep(SessionFile session, RecordingStep step)
    {
        if (session.Recording is not { Active: true }) return;

        step.Seq = (session.Recording.Steps.Count > 0
            ? session.Recording.Steps.Max(s => s.Seq)
            : 0) + 1;
        step.Timestamp = DateTime.UtcNow;
        session.Recording.Steps.Add(step);
    }

    public void Drop(SessionFile session, int seq)
    {
        if (session.Recording is null)
            throw new InvalidOperationException("No recording exists.");

        var step = session.Recording.Steps.FirstOrDefault(s => s.Seq == seq)
                   ?? throw new InvalidOperationException($"Step {seq} not found.");
        step.Included = false;
    }

    public void DropLast(SessionFile session, int count = 1)
    {
        if (session.Recording is null)
            throw new InvalidOperationException("No recording exists.");

        var stepsToExclude = session.Recording.Steps
            .Where(s => s.Included)
            .OrderByDescending(s => s.Seq)
            .Take(count);

        foreach (var step in stepsToExclude)
            step.Included = false;
    }

    public void Keep(SessionFile session, int seq)
    {
        if (session.Recording is null)
            throw new InvalidOperationException("No recording exists.");

        var step = session.Recording.Steps.FirstOrDefault(s => s.Seq == seq)
                   ?? throw new InvalidOperationException($"Step {seq} not found.");
        step.Included = true;
    }

    public List<RecordingStepSummary> List(SessionFile session)
    {
        if (session.Recording is null)
            return [];

        return session.Recording.Steps.Select(s => new RecordingStepSummary(
            s.Seq,
            s.Command,
            s.Target?.AutomationId ?? s.Target?.Name ?? "unknown",
            s.Target?.SelectorQuality,
            s.Included)).ToList();
    }

    public RecordExportResult Export(SessionFile session)
    {
        if (session.Recording is null)
            throw new InvalidOperationException("No recording exists.");

        var includedSteps = session.Recording.Steps.Where(s => s.Included).ToList();

        var selectorSummary = includedSteps
            .Where(s => s.Target is not null)
            .GroupBy(s => s.Target!.SelectorQuality.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new RecordExportResult(
            Success: true,
            Message: "Recording exported.",
            Description: session.Recording.Description,
            StepCount: includedSteps.Count,
            SelectorSummary: selectorSummary,
            Steps: includedSteps);
    }
}
