using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sttplay.MediaPlayer 
{
    public class SCWin32
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        public const uint MB_ICONHAND = 0x00000010;
        public const uint MB_ICONQUESTION = 0x00000020;
        public const uint MB_ICONEXCLAMATION = 0x00000030;
        public const uint MB_ICONASTERISK = 0x00000040;
        public const uint MB_ICONWARNING = MB_ICONEXCLAMATION;
        public const uint MB_ICONERROR = MB_ICONHAND;

        [DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr handle, string message, string title, uint type);

        public delegate bool WNDENUMPROC(IntPtr hwnd, uint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, uint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern void SetLastError(uint dwErrCode);

        public static IntPtr GetProcessWnd()
        {
            IntPtr ptrWnd = IntPtr.Zero;
            uint pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

            bool bResult = EnumWindows(new WNDENUMPROC(delegate (IntPtr hwnd, uint lParam)
            {
                uint id = 0;

                if (GetParent(hwnd) == IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hwnd, ref id);
                    if (id == lParam)
                    {
                        ptrWnd = hwnd;
                        SetLastError(0);
                        return false;
                    }
                }
                return true;
            }), pid);

            return (!bResult && Marshal.GetLastWin32Error() == 0) ? ptrWnd : IntPtr.Zero;
        }
#endif
    }
}
