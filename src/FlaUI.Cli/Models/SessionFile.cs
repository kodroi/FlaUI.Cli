namespace FlaUI.Cli.Models;

public class SessionFile
{
    public string Schema { get; set; } = "flaui-session-v1";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ApplicationInfo Application { get; set; } = new();
    public long? TargetWindowHandle { get; set; }
    public string? TargetWindowTitle { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? SourcePath { get; set; }
    public string SelectorPolicy { get; set; } = "stable";
    public Dictionary<string, ElementEntry> Elements { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
    public RecordingState? Recording { get; set; }
}
