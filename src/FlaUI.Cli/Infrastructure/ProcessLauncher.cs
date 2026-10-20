using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FlaUI.Cli.Infrastructure;

internal static class ProcessLauncher
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public IntPtr Reserved, Desktop, Title;
        public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public short ShowWindow, ReservedSize;
        public IntPtr ReservedBytes, StandardInput, StandardOutput, StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process, Thread;
        public int ProcessId, ThreadId;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcess(string application, [In, Out] char[] commandLine,
        IntPtr processAttributes, IntPtr threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint flags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInformation information);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    internal static int Launch(string path, string? args)
    {
        var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
        var commandLine = ($"\"{path}\"" + (string.IsNullOrEmpty(args) ? "" : " " + args) + '\0').ToCharArray();
        // No inherited handles: the application must not keep the caller's redirected pipes alive.
        // CREATE_NO_WINDOW avoids allocating a console for console-subsystem applications.
        if (!CreateProcess(path, commandLine, IntPtr.Zero, IntPtr.Zero, false, 0x08000000,
                IntPtr.Zero, Environment.CurrentDirectory, ref startup, out var information))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        _ = CloseHandle(information.Thread);
        _ = CloseHandle(information.Process);
        return information.ProcessId;
    }
}
