using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SingleInstanceDemo;

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FlashInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashInfo
    {
        public uint Size;
        public IntPtr Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    internal static void AllowForeground(int processId) => AllowSetForegroundWindow(processId);

    internal static void RestoreAndActivate(Window window)
    {
        window.Dispatcher.VerifyAccess();
        if (!window.IsVisible) window.Show();
        IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
        if (window.WindowState == WindowState.Minimized)
            ShowWindow(handle, 9); // SW_RESTORE，保留最小化前的恢复状态。
        if (!window.Activate())
        {
            var info = new FlashInfo
            {
                Size = (uint)Marshal.SizeOf<FlashInfo>(), Window = handle, Flags = 2, Count = 3
            };
            FlashWindowEx(ref info); // FLASHW_TRAY：提示用户，不反复抢焦点。
        }
    }
}
