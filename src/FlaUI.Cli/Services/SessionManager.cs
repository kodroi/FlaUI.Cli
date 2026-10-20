using System.Diagnostics;
using System.Text.Json;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Services;

public class SessionManager
{
    private readonly JsonSerializerOptions _jsonOptions = JsonOutput.GetOptions();

    public static string ResolveSessionPath(string? sessionFlag)
    {
        // 1. --session flag
        if (!string.IsNullOrEmpty(sessionFlag))
            return Path.GetFullPath(sessionFlag);

        // 2. FLAUI_SESSION env var
        var envSession = Environment.GetEnvironmentVariable("FLAUI_SESSION");
        if (!string.IsNullOrEmpty(envSession))
            return Path.GetFullPath(envSession);

        // 3. Find single *.session.json in cwd
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.session.json");
        return files.Length switch
        {
            1 => files[0],
            0 => throw new InvalidOperationException("No session file found. Run 'flaui session new' first."),
            _ => throw new InvalidOperationException(
                $"Multiple session files found ({files.Length}). Use --session to specify one.")
        };
    }

    public static string CreateSessionPath(string appPath)
    {
        var name = Path.GetFileNameWithoutExtension(appPath).ToLowerInvariant();
        return Path.Combine(Directory.GetCurrentDirectory(), $"{name}.session.json");
    }

    public SessionFile Load(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var session = JsonSerializer.Deserialize<SessionFile>(stream, _jsonOptions)
                   ?? throw new InvalidOperationException("Failed to deserialize session file.");
            session.SourcePath = Path.GetFullPath(path);
            return session;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Could not load session '{path}': {ex.Message}", ex);
        }
    }

    public void Save(string path, SessionFile session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, session, _jsonOptions);
    }

    public static bool IsProcessAlive(SessionFile session)
    {
        try
        {
            var process = Process.GetProcessById(session.Application.Pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsWindowValid(SessionFile session)
    {
        if (session.Application.MainWindowHandle == 0)
            return false;

        return NativeInterop.BelongsToProcess(new IntPtr(session.Application.MainWindowHandle), session.Application.Pid);
    }

    public static void AddElement(SessionFile session, string elementId, ElementEntry entry)
    {
        session.Elements[elementId] = entry;
    }

    public static ElementEntry? GetElement(SessionFile session, string elementId)
    {
        return session.Elements.GetValueOrDefault(elementId);
    }

    public static void SetVariable(SessionFile session, string name, string value)
    {
        session.Variables[name] = value;
    }

    public static string? GetVariable(SessionFile session, string name)
    {
        return session.Variables.GetValueOrDefault(name);
    }
}
