namespace FlaUI.Cli.Models;

public class ApplicationInfo
{
    public string? Path { get; set; }
    public string? Args { get; set; }
    public int Pid { get; set; }
    public string? ProcessName { get; set; }
    public long MainWindowHandle { get; set; }
    public string? MainWindowTitle { get; set; }
}
