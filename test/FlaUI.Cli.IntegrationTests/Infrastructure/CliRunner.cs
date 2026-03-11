using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlaUI.Cli.IntegrationTests.Infrastructure;

public record CliResult(int ExitCode, string Stdout, string Stderr);

public class CliRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _cliPath;

    public CliRunner(string cliPath)
    {
        _cliPath = cliPath;
    }

    public async Task<CliResult> RunAsync(string arguments, int timeoutMs = 30000)
    {
        var sw = Stopwatch.StartNew();
        Log($"[CLI] START: flaui {arguments}");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.EnableRaisingEvents = true;

        // Use TaskCompletionSource tied to the Exited event.
        // Unlike WaitForExitAsync/WaitForExit(), this fires when the process exits
        // without waiting for child processes that inherited stdout/stderr handles.
        var exitTcs = new TaskCompletionSource<int>();
        process.Exited += (_, _) =>
        {
            try { exitTcs.TrySetResult(process.ExitCode); }
            catch { exitTcs.TrySetResult(-1); }
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        Log($"[CLI] PID={process.Id} launched ({sw.ElapsedMilliseconds}ms)");

        // If the process already exited before we subscribed, check now
        if (process.HasExited)
            exitTcs.TrySetResult(process.ExitCode);

        using var cts = new CancellationTokenSource(timeoutMs);
        await using var _ = cts.Token.Register(() => exitTcs.TrySetCanceled());

        int exitCode;
        try
        {
            exitCode = await exitTcs.Task;
        }
        catch (TaskCanceledException)
        {
            Log($"[CLI] TIMEOUT after {sw.ElapsedMilliseconds}ms! Killing PID={process.Id}. Args: {arguments}");
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"CLI process did not exit within {timeoutMs}ms. Args: {arguments}");
        }

        // Give event-based output a moment to flush after process exit
        await Task.Delay(100);

        var result = new CliResult(exitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
        Log($"[CLI] DONE: exit={result.ExitCode} ({sw.ElapsedMilliseconds}ms) args: {arguments}");
        if (result.ExitCode != 0)
        {
            Log($"[CLI] STDOUT: {result.Stdout}");
            if (!string.IsNullOrEmpty(result.Stderr))
                Log($"[CLI] STDERR: {result.Stderr}");
        }

        return result;
    }

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);

    private static void Log(string message)
    {
        Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}
