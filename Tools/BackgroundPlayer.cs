using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CheckInPlease.Tools
{
    // Keep the real Player renderer running on a desktop that is never activated.
    // Hiding/minimizing Unity's window (or using batch mode) produces black captures.
    public sealed class BackgroundPlayer : IDisposable
    {
        IntPtr desktop, process;
        public uint Id { get; private set; }
        public string DesktopName { get; private set; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct StartupInfo
        {
            public uint cb;
            public string reserved, desktop, title;
            public uint x, y, width, height, xChars, yChars, fill, flags;
            public ushort showWindow, reservedSize;
            public IntPtr reservedData, stdin, stdout, stderr;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ProcessInfo
        {
            public IntPtr process, thread;
            public uint processId, threadId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr CreateDesktopW(string name, IntPtr device, IntPtr mode,
            uint flags, uint access, IntPtr attributes);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseDesktop(IntPtr handle);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateProcessW(string application, StringBuilder commandLine,
            IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles,
            uint flags, IntPtr environment, string directory, ref StartupInfo startup,
            out ProcessInfo info);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetExitCodeProcess(IntPtr handle, out uint code);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool TerminateProcess(IntPtr handle, uint code);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        public BackgroundPlayer(string executable, string arguments)
        {
            DesktopName = "CheckInPleaseValidation_" + Guid.NewGuid().ToString("N");
            desktop = CreateDesktopW(DesktopName, IntPtr.Zero, IntPtr.Zero, 0, 0x01ff, IntPtr.Zero);
            if (desktop == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var startup = new StartupInfo {
                    cb = (uint)Marshal.SizeOf(typeof(StartupInfo)),
                    desktop = DesktopName, flags = 1, showWindow = 4
                };
                ProcessInfo info;
                var command = new StringBuilder("\"" + executable + "\" " + arguments);
                if (!CreateProcessW(executable, command, IntPtr.Zero, IntPtr.Zero, false,
                    0, IntPtr.Zero, Path.GetDirectoryName(executable), ref startup, out info))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                process = info.process;
                Id = info.processId;
                CloseHandle(info.thread);
            }
            catch { Dispose(); throw; }
        }

        public bool WaitForExit(int milliseconds)
        {
            if (milliseconds < 0) throw new ArgumentOutOfRangeException("milliseconds");
            if (process == IntPtr.Zero) throw new ObjectDisposedException("BackgroundPlayer");
            uint status = WaitForSingleObject(process, (uint)milliseconds);
            if (status == 0xffffffff) throw new Win32Exception(Marshal.GetLastWin32Error());
            return status == 0;
        }

        public uint ExitCode
        {
            get
            {
                if (!WaitForExit(0)) throw new InvalidOperationException("Player is still running.");
                uint code;
                if (!GetExitCodeProcess(process, out code))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return code;
            }
        }

        public void Dispose()
        {
            if (process != IntPtr.Zero)
            {
                // Only terminate the process this instance created; never a user's game.
                if (WaitForSingleObject(process, 0) == 258)
                {
                    TerminateProcess(process, 1);
                    WaitForSingleObject(process, 5000);
                }
                CloseHandle(process);
                process = IntPtr.Zero;
            }
            if (desktop != IntPtr.Zero) { CloseDesktop(desktop); desktop = IntPtr.Zero; }
        }
    }
}
