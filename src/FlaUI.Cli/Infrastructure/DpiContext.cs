using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

internal sealed class DpiUnavailableException(string message) : Exception(message)
{
    internal const string Recovery = "Remove Windows compatibility DPI overrides for the CLI host and retry in an interactive Windows desktop. Use a PerMonitorV2-capable Windows version. UI Automation pattern-only actions remain available.";
}

/// <summary>A synchronous, thread-affine override; never keep it across an await.</summary>
internal sealed class DpiContext : IDisposable
{
    internal static IntPtr PerMonitorV2 { get; } = new(-4);
    [ThreadStatic] private static DpiContext? _current;
    private readonly DpiContext? _previous;
    private readonly Func<IntPtr, IntPtr> _set;
    private readonly Func<bool> _verify;
    private readonly IntPtr _oldContext;
    private bool _disposed;
    internal string? Failure { get; }
    internal static string? ProcessFailure { get; private set; }

    internal DpiContext(Func<IntPtr, IntPtr> set, Func<bool> verify)
    {
        _previous = _current;
        _set = set;
        _verify = verify;
        try
        {
            _oldContext = set(PerMonitorV2);
            if (_oldContext == IntPtr.Zero || !verify())
                Failure = "Windows could not establish a PerMonitorV2 thread context.";
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException or Win32Exception)
        {
            Failure = ex.Message;
        }
        _current = this;
    }

    internal static void InitializeProcess()
    {
        try
        {
            if (AreDpiAwarenessContextsEqual(GetThreadDpiAwarenessContext(), PerMonitorV2)) return;
            if (!SetProcessDpiAwarenessContext(PerMonitorV2))
                ProcessFailure = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            ProcessFailure = ex.Message;
        }
    }

    internal static DpiContext Enter() => new(SetThreadDpiAwarenessContext,
        () => AreDpiAwarenessContextsEqual(GetThreadDpiAwarenessContext(), PerMonitorV2));

    internal static bool IsVerified => _current is { Failure: null, _disposed: false } scope && scope._verify();

    internal static void RequireCoordinates()
    {
        if (!IsVerified)
        {
            var reason = _current?.Failure ?? "The executing thread does not have a verified PerMonitorV2 coordinate context.";
            if (ProcessFailure is not null) reason += $" Process initialization: {ProcessFailure}";
            throw new DpiUnavailableException(reason);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            if (_oldContext != IntPtr.Zero) _set(_oldContext);
        }
        finally
        {
            _current = _previous;
            _disposed = true;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
}
