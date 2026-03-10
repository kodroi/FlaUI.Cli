namespace FlaUI.Cli.Models;

public class RecordingState
{
    public bool Active { get; set; }
    public DateTime? StartedAt { get; set; }
    public string? Description { get; set; }
    public List<RecordingStep> Steps { get; set; } = [];
}
