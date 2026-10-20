using System.Windows;
using System.Runtime.InteropServices;

namespace FlaUI.Cli.TestApp;

public partial class App : Application
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(int extendedStyle, string className, string title,
        uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Contains("flaui-fixture-dpi")) StartupUri = new Uri("DpiTestWindow.xaml", UriKind.Relative);
        if (e.Args.Contains("flaui-fixture-slow")) Thread.Sleep(1500);
        if (e.Args.Contains("flaui-fixture-noisy"))
        {
            _ = Task.Run(async () =>
            {
                var output = new string('x', 4096);
                for (var i = 0; i < 1000; i++)
                {
                    Console.WriteLine(output);
                    Console.Error.WriteLine(output);
                    await Task.Delay(2);
                }
            });
        }
        base.OnStartup(e);
        if (e.Args.Contains("flaui-fixture-decorative"))
        {
            var handle = CreateWindowEx(0x08000080, "STATIC", "FlaUI decorative", 0x90000000,
                0, 0, 20, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (handle == IntPtr.Zero) throw new InvalidOperationException("Could not create decorative fixture.");
        }
    }
}
