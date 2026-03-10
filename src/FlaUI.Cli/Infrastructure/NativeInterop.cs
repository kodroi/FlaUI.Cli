using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

public static partial class NativeInterop
{
    private const int SwRestore = 9;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(IntPtr hWnd);

    public static void BringToFront(IntPtr hWnd)
    {
        ShowWindow(hWnd, SwRestore);
        Thread.Sleep(50);
        SetForegroundWindow(hWnd);
        Thread.Sleep(50);
    }
}
