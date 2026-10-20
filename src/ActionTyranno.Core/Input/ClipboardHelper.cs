using System.Runtime.InteropServices;

namespace ActionTyranno.Core.Input;

/// <summary>
/// Sets clipboard text through the Win32 API. Unlike WPF/WinForms Clipboard this needs
/// no STA thread, so it is safe to call from the Task.Run thread MacroPlayer runs on.
/// </summary>
internal static class ClipboardHelper
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    public static void SetText(string text)
    {
        var bytes = (text.Length + 1) * 2;
        var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
        if (hGlobal == IntPtr.Zero)
            throw new InvalidOperationException("클립보드 메모리 할당 실패");

        try
        {
            var ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("클립보드 메모리 잠금 실패");
            try
            {
                Marshal.Copy((text + "\0").ToCharArray(), 0, ptr, text.Length + 1);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (!OpenClipboardWithRetry())
                throw new InvalidOperationException("클립보드를 열 수 없습니다 (다른 프로그램이 사용 중).");
            try
            {
                EmptyClipboard();
                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    throw new InvalidOperationException("클립보드 설정 실패");
                hGlobal = IntPtr.Zero; // ownership moved to the system
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            if (hGlobal != IntPtr.Zero)
                GlobalFree(hGlobal);
        }
    }

    private static bool OpenClipboardWithRetry()
    {
        for (var i = 0; i < 10; i++)
        {
            if (OpenClipboard(IntPtr.Zero))
                return true;
            Thread.Sleep(50);
        }
        return false;
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)] private static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalFree(IntPtr hMem);
}
