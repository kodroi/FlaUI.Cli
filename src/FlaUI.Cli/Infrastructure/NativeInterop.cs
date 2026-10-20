using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

public static partial class NativeInterop
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, [Out] char[] text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, [Out] char[] text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    internal static bool BelongsToProcess(IntPtr handle, int pid)
    {
        if (!IsWindow(handle)) return false;
        GetWindowThreadProcessId(handle, out var actualPid);
        return actualPid == (uint)pid;
    }

    internal static (string Title, string ClassName, Models.BoundsInfo Bounds) ReadWindow(IntPtr handle)
    {
        var title = new char[32768];
        var className = new char[256];
        var titleLength = GetWindowText(handle, title, title.Length);
        var classLength = GetClassName(handle, className, className.Length);
        if (!GetWindowRect(handle, out var rect))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return (new string(title, 0, titleLength), new string(className, 0, classLength), new Models.BoundsInfo(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
    }
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

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

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

        keybd_event(0, 0, 0, UIntPtr.Zero);
        SetForegroundWindow(hWnd);
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
