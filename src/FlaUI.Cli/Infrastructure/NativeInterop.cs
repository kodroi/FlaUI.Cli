using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

public static partial class NativeInterop
{
    private const int SwRestore = 9;
    private const int SwShow = 5;

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

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachThreadInput(uint idAttach, uint idAttachTo,
        [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint WmClose = 0x0010;

    public static void CloseWindow(IntPtr hWnd)
    {
        SendMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);
    }

    public static void BringToFront(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SwRestore);
        else
            ShowWindow(hWnd, SwShow);

        var foregroundHwnd = GetForegroundWindow();
        var ourThread = GetCurrentThreadId();
        GetWindowThreadProcessId(foregroundHwnd, out _);
        var foregroundThread = GetWindowThreadProcessId(foregroundHwnd, out _);

        if (ourThread != foregroundThread)
        {
            AttachThreadInput(ourThread, foregroundThread, true);
            SetForegroundWindow(hWnd);
            BringWindowToTop(hWnd);
            AttachThreadInput(ourThread, foregroundThread, false);
        }
        else
        {
            SetForegroundWindow(hWnd);
            BringWindowToTop(hWnd);
        }

        Thread.Sleep(100);
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
