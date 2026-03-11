using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

public static partial class NativeInterop
{
    private const int SwRestore = 9;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(IntPtr hWnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    public static void BringToFront(IntPtr hWnd)
    {
        ShowWindow(hWnd, SwRestore);
        Thread.Sleep(50);
        SetForegroundWindow(hWnd);
        Thread.Sleep(50);
    }

    public static List<IntPtr> GetProcessWindowHandles(int processId)
    {
        var handles = new List<IntPtr>();
        var targetPid = (uint)processId;

        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out var pid);
            if (pid == targetPid && IsWindowVisible(hWnd))
                handles.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        return handles;
    }
}
