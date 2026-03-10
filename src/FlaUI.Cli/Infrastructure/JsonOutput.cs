using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlaUI.Cli.Infrastructure;

public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void Write<T>(T value)
    {
        Console.WriteLine(JsonSerializer.Serialize(value, Options));
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public static JsonSerializerOptions GetOptions() => Options;
}
