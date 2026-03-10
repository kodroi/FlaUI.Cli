namespace FlaUI.Cli.Models;

public class SessionFile
{
    public string Schema { get; set; } = "flaui-session-v1";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ApplicationInfo Application { get; set; } = new();
    public string SelectorPolicy { get; set; } = "stable";
    public Dictionary<string, ElementEntry> Elements { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
    public RecordingState? Recording { get; set; }
}
