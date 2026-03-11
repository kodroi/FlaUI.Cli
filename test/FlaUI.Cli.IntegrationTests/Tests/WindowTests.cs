using System.Runtime.InteropServices;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class WindowTests
{
    private readonly TestAppFixture _fixture;

    public WindowTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    #region Win32 Helpers for focus tests

    private const int SwMinimize = 6;
    private const int SwRestore = 9;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private static void BringToFront_New(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SwRestore);

        keybd_event(0, 0, 0, UIntPtr.Zero);
        SetForegroundWindow(hWnd);
        Thread.Sleep(100);
    }

    private static IntPtr GetFirstVisibleWindowForProcess(int pid)
    {
        IntPtr found = IntPtr.Zero;
        var targetPid = (uint)pid;
        EnumWindows((hWnd, _) =>
        {
            uint threadId = GetWindowThreadProcessId(hWnd, out var windowPid);
            if (windowPid == targetPid && IsWindowVisible(hWnd))
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    #endregion

    [Fact]
    public async Task WindowList_ReturnsAtLeastOneWindow()
    {
        var result = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, result.ExitCode);

        var windowList = CliRunner.Deserialize<WindowListResult>(result.Stdout);
        Assert.NotNull(windowList);
        Assert.True(windowList.Success);
        Assert.NotNull(windowList.Windows);
        Assert.NotEmpty(windowList.Windows);

        var mainWindow = windowList.Windows.FirstOrDefault(w => w.Title == "Contact Form");
        Assert.NotNull(mainWindow);
        Assert.NotNull(mainWindow.Handle);
        Assert.False(mainWindow.IsModal);
    }

    [Fact]
    public async Task WindowFocus_BringsWindowToForeground()
    {
        // First get the window handle
        var listResult = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, listResult.ExitCode);
        var windowList = CliRunner.Deserialize<WindowListResult>(listResult.Stdout);
        Assert.NotNull(windowList?.Windows);
        var mainWindow = windowList.Windows.First(w => w.Title == "Contact Form");

        // Focus the window
        var focusResult = await _fixture.Cli.RunAsync(
            $"window focus --handle {mainWindow.Handle} {_fixture.SessionArg}");
        Assert.Equal(0, focusResult.ExitCode);

        var focus = CliRunner.Deserialize<WindowFocusResult>(focusResult.Stdout);
        Assert.NotNull(focus);
        Assert.True(focus.Success);
        Assert.Equal("Contact Form", focus.Title);
    }

    [Fact]
    public async Task WindowList_ContainsHandleAndBounds()
    {
        var result = await _fixture.Cli.RunAsync($"window list {_fixture.SessionArg}");
        Assert.Equal(0, result.ExitCode);

        var windowList = CliRunner.Deserialize<WindowListResult>(result.Stdout);
        Assert.NotNull(windowList?.Windows);

        var window = windowList.Windows.First();
        Assert.NotNull(window.Handle);
        Assert.NotNull(window.Bounds);
        Assert.True(window.Bounds.Width > 0);
        Assert.True(window.Bounds.Height > 0);
    }

    [Fact]
    public async Task BringToFront_NewApproach_RestoresMinimizedWindow()
    {
        var testAppHwnd = GetFirstVisibleWindowForProcess(_fixture.AppPid);
        Assert.NotEqual(IntPtr.Zero, testAppHwnd);

        // Minimize test app
        ShowWindow(testAppHwnd, SwMinimize);
        await Task.Delay(300);
        Assert.True(IsIconic(testAppHwnd), "Test app should be minimized");

        // Use new approach to bring test app to front
        BringToFront_New(testAppHwnd);
        await Task.Delay(300);

        // Verify test app is restored and foreground
        Assert.False(IsIconic(testAppHwnd), "Test app should no longer be minimized");
        var foreground = GetForegroundWindow();
        Assert.True(foreground == testAppHwnd,
            "Test app should be the foreground window after BringToFront_New");
    }

    [Fact]
    public async Task BringToFront_NewApproach_ReliablySetsForeground()
    {
        var testAppHwnd = GetFirstVisibleWindowForProcess(_fixture.AppPid);
        Assert.NotEqual(IntPtr.Zero, testAppHwnd);

        // Run multiple iterations to verify reliability
        for (int i = 0; i < 5; i++)
        {
            // Minimize test app
            ShowWindow(testAppHwnd, SwMinimize);
            await Task.Delay(200);

            // Restore via new approach
            BringToFront_New(testAppHwnd);
            await Task.Delay(200);

            var foreground = GetForegroundWindow();
            Assert.True(foreground == testAppHwnd,
                $"Iteration {i}: Test app should be foreground after BringToFront_New");
            Assert.False(IsIconic(testAppHwnd),
                $"Iteration {i}: Test app should not be minimized after BringToFront_New");
        }
    }
}
