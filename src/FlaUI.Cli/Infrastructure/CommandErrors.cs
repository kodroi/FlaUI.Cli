using System.CommandLine;
using System.Text.Json.Nodes;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Infrastructure;

internal static class CommandErrors
{
    internal static string Command { get; set; } = "flaui";
    internal static Dictionary<string, string?> Input { get; set; } = [];
    internal static bool Failed { get; set; }
    internal static string? LastErrorCode { get; private set; }

    internal static ErrorResult FromException(Exception exception) => new(false, exception.Message,
        exception switch
        {
            DpiUnavailableException => "dpi_unavailable",
            TimeoutException => "timeout",
            FormatException or ArgumentException or OverflowException => Code(exception.Message) == "invalid_handle" ? "invalid_handle" : "invalid_arguments",
            FileNotFoundException or DirectoryNotFoundException when exception.Message.Contains("session", StringComparison.OrdinalIgnoreCase) => "session_error",
            _ => Code(exception.Message)
        }, Recovery: exception is DpiUnavailableException ? DpiUnavailableException.Recovery : null);

    internal static void Initialize(ParseResult parsed, string[] args)
    {
        Failed = false;
        LastErrorCode = null;
        TargetContext.Reset();
        Command = parsed.CommandResult.Command.Name;
        var parent = parsed.CommandResult.Parent;
        while (parent is System.CommandLine.Parsing.CommandResult command && command.Command is not RootCommand)
        {
            Command = command.Command.Name + " " + Command;
            parent = command.Parent;
        }
        Input = [];
        for (var i = 0; i < args.Length; i++)
            if (args[i].StartsWith("--", StringComparison.Ordinal))
                Input[args[i]] = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[i + 1] : null;
    }

    internal static string Code(string message) => message switch
    {
        _ when message.StartsWith("Could not load session", StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith("Could not save session", StringComparison.OrdinalIgnoreCase) => "session_error",
        _ when message.Contains("handle", StringComparison.OrdinalIgnoreCase) &&
            (message.Contains("invalid", StringComparison.OrdinalIgnoreCase) || message.Contains("format", StringComparison.OrdinalIgnoreCase)) => "invalid_handle",
        _ when message.StartsWith("Timeout", StringComparison.OrdinalIgnoreCase) => "timeout",
        _ when (message.StartsWith("Element", StringComparison.OrdinalIgnoreCase) || message.StartsWith("Window", StringComparison.OrdinalIgnoreCase)) && message.Contains("not found", StringComparison.OrdinalIgnoreCase) => "not_found",
        _ when message.Contains("session", StringComparison.OrdinalIgnoreCase) || message.Contains(".session.json", StringComparison.OrdinalIgnoreCase) => "session_error",
        _ when message.Contains("not found", StringComparison.OrdinalIgnoreCase) => "not_found",
        _ when message.Contains("required", StringComparison.OrdinalIgnoreCase) || message.Contains("must", StringComparison.OrdinalIgnoreCase) || message.Contains("range", StringComparison.OrdinalIgnoreCase) => "invalid_arguments",
        _ => "automation_error"
    };

    internal static void Enrich(JsonObject result)
    {
        if (result["success"]?.GetValue<bool>() != false) return;
        Failed = true;
        var message = result["message"]?.GetValue<string>() ?? "Command failed.";
        result["errorCode"] ??= result.ContainsKey("failed") ? "batch_failed" : Code(message);
        LastErrorCode = result["errorCode"]?.GetValue<string>();
        result["command"] ??= Command;
        if (Input.Count > 0) result["input"] ??= System.Text.Json.JsonSerializer.SerializeToNode(Input);
    }
}
