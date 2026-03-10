namespace FlaUI.Cli.Models;

public class RecordingStep
{
    public int Seq { get; set; }
    public DateTime Timestamp { get; set; }
    public string Command { get; set; } = string.Empty;
    public RecordingTarget? Target { get; set; }
    public Dictionary<string, object?>? Params { get; set; }
    public RecordingStepResult? Result { get; set; }
    public bool Included { get; set; } = true;
}

public class RecordingTarget
{
    public string? AutomationId { get; set; }
    public string? Name { get; set; }
    public string? ControlType { get; set; }
    public SelectorQuality SelectorQuality { get; set; }
    public string? Strategy { get; set; }
}

public class RecordingStepResult
{
    public bool Success { get; set; }
    public string? Value { get; set; }
    public string? Error { get; set; }
}
