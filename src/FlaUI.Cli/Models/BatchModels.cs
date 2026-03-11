using System.Text.Json.Serialization;

namespace FlaUI.Cli.Models;

public class BatchInput
{
    [JsonPropertyName("steps")]
    public List<BatchStep> Steps { get; set; } = [];
}

public class BatchStep
{
    [JsonPropertyName("cmd")]
    public string Cmd { get; set; } = "";

    [JsonPropertyName("args")]
    public Dictionary<string, string> Args { get; set; } = new();
}
