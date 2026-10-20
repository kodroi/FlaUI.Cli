using System.Diagnostics;
using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class LaunchOutputTests
{
    [Theory]
    [InlineData("normal")]
    [InlineData("wrong-title")]
    [InlineData("slow")]
    public async Task Launch_ClosesCallerStreams_AndLeavesAppAlive(string mode)
    {
        var timeout = mode != "normal";
        var root = SolutionLocator.FindSolutionRoot();
        var session = Path.Combine(Path.GetTempPath(), $"flaui-capture-{Guid.NewGuid():N}.session.json");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(SolutionLocator.GetCliPath(root))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        foreach (var arg in new[] { "session", "new", "--app", SolutionLocator.GetTestAppPath(root), "--session", session })
            process.StartInfo.ArgumentList.Add(arg);
        foreach (var arg in new[] { "--args", mode == "slow" ? "flaui-fixture-slow" : "flaui-fixture-noisy" })
            process.StartInfo.ArgumentList.Add(arg);
        if (mode == "wrong-title")
            foreach (var arg in new[] { "--wait-title", "This title will never exist", "--wait-timeout", "300" })
                process.StartInfo.ArgumentList.Add(arg);
        if (mode == "slow")
            foreach (var arg in new[] { "--timeout", "300" }) process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        int? appPid = null;
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr).WaitAsync(TimeSpan.FromSeconds(15));
            using var result = JsonDocument.Parse(await stdout);
            appPid = result.RootElement.GetProperty("pid").GetInt32();
            Assert.Equal(!timeout, result.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(timeout ? 1 : 0, process.ExitCode);
            using var application = Process.GetProcessById(appPid.Value);
            Assert.False(application.HasExited);
            await Task.Delay(600);
            Assert.False(application.HasExited);
            if (timeout) Assert.Contains("attach", result.RootElement.GetProperty("recovery").GetString());
            if (mode == "slow")
            {
                await Task.Delay(1500);
                var cli = new CliRunner(SolutionLocator.GetCliPath(root));
                var attached = await cli.RunAsync($"session attach --pid {appPid} --session \"{session}\"");
                Assert.Equal(0, attached.ExitCode);
            }
        }
        finally
        {
            // A failed EOF assertion can still leave a session result on disk.
            if (appPid is null && File.Exists(session))
            {
                using var saved = JsonDocument.Parse(File.ReadAllText(session));
                appPid = saved.RootElement.GetProperty("application").GetProperty("pid").GetInt32();
            }
            if (appPid is { } pid)
            {
                try { using var app = Process.GetProcessById(pid); app.Kill(); app.WaitForExit(5000); }
                catch (ArgumentException) { }
            }
            if (!process.HasExited) process.Kill();
            File.Delete(session);
        }
    }
}
