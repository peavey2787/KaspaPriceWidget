using System;
using System.Runtime.InteropServices;

namespace KaspaPriceWidget
{
    public class AlwaysOnTopToggler
    {
        // Constants from Win32 API
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // P/Invoke signatures
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // RECT structure for window dimensions
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private IntPtr _mainWindowHandle;

        // Constructor to accept window handle
        public AlwaysOnTopToggler(IntPtr mainWindowHandle)
        {
            _mainWindowHandle = mainWindowHandle;
        }

        // Method to set window always on top
        public void SetAlwaysOnTop(bool enable)
        {
            if (_mainWindowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid window handle.");
            }

            IntPtr hWndInsertAfter = enable ? (IntPtr)HWND_TOPMOST : (IntPtr)HWND_NOTOPMOST;
            SetWindowPos(_mainWindowHandle, hWndInsertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        // Method to toggle always on top
        public void ToggleAlwaysOnTop()
        {
            if (_mainWindowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid window handle.");
            }

            RECT rect;
            if (!GetWindowRect(_mainWindowHandle, out rect))
            {
                throw new InvalidOperationException("Could not get window rectangle.");
            }

            // Check if the window is currently topmost
            bool isTopmost = ((uint)GetWindowLong(_mainWindowHandle, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;
            SetAlwaysOnTop(!isTopmost);
        }

        // Constants and P/Invoke for GetWindowLong
        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TOPMOST = 0x00000008;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    }
}


