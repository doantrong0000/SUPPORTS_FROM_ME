using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DATN_AUTO_CREATE_PART.Utils
{
    public static class WindowFocusHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void BringToFront(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    // Many times multiple processed are launched (like subprocesses), we want the main one
                    foreach (var process in processes)
                    {
                        IntPtr handle = process.MainWindowHandle;
                        if (handle != IntPtr.Zero)
                        {
                            ShowWindow(handle, 9); // SW_RESTORE
                            SetForegroundWindow(handle);
                            break;
                        }
                    }
                }
            }
            catch { }
        }
    }
}
