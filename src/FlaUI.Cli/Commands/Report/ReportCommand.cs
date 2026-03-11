using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;

namespace FlaUI.Cli.Commands.Report;

/// <summary>
/// Creates a GitHub issue on the FlaUI.Cli repository via the gh CLI.
/// </summary>
public static class ReportCommand
{
    private const string Repo = "kodroi/FlaUI.Cli";

    public static Command Create(Option<string?> sessionOption)
    {
        var titleOption = new Option<string>("--title")
        {
            Description = "Issue title",
            Required = true
        };

        var descriptionOption = new Option<string>("--description")
        {
            Description = "Issue body / description",
            Required = true
        };

        var command = new Command("report", "Report an issue to the FlaUI.Cli GitHub repository");
        command.Add(titleOption);
        command.Add(descriptionOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var title = parseResult.GetValue(titleOption)!;
            var description = parseResult.GetValue(descriptionOption)!;

            try
            {
                if (!IsGhInstalled())
                {
                    JsonOutput.Write(new ErrorResult(false, "GitHub CLI (gh) is not installed. Install it from https://cli.github.com"));
                    Environment.ExitCode = ExitCodes.GhNotInstalled;
                    return;
                }

                if (!IsGhAuthenticated())
                {
                    JsonOutput.Write(new ErrorResult(false, "GitHub CLI is not authenticated. Run 'gh auth login' first."));
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                var (exitCode, output) = RunGh($"issue create --repo {Repo} --title \"{EscapeArg(title)}\" --body \"{EscapeArg(description)}\"");
                if (exitCode == 0)
                {
                    var issueUrl = output.Trim();
                    JsonOutput.Write(new ReportResult(true, "Issue created successfully.", issueUrl));
                    Environment.ExitCode = ExitCodes.Success;
                }
                else
                {
                    JsonOutput.Write(new ErrorResult(false, $"Failed to create issue: {output.Trim()}"));
                    Environment.ExitCode = ExitCodes.Error;
                }
            }
            catch (Exception ex)
            {
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static bool IsGhInstalled()
    {
        try
        {
            var (exitCode, _) = RunGh("--version");
            return exitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static bool IsGhAuthenticated()
    {
        var (exitCode, _) = RunGh("auth status");
        return exitCode == 0;
    }

    private static (int ExitCode, string Output) RunGh(string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var output = string.IsNullOrEmpty(stdout) ? stderr : stdout;
        return (process.ExitCode, output);
    }

    private static string EscapeArg(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
