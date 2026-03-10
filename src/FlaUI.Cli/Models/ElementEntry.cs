namespace FlaUI.Cli.Models;

public class ElementEntry
{
    public string? AutomationId { get; set; }
    public string? Name { get; set; }
    public string? ControlType { get; set; }
    public string? ClassName { get; set; }
    public int[]? RuntimeId { get; set; }
    public SelectorQuality SelectorQuality { get; set; }
    public DateTime LastVerified { get; set; }
}
