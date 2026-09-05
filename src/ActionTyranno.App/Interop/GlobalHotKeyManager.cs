using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ActionTyranno.App.Interop;

/// <summary>
/// Registers OS-level global hotkeys (Win32 RegisterHotKey) that fire even when the app
/// is minimized or not focused. Must be created after the owning window's handle exists
/// (e.g. in OnSourceInitialized) and disposed when the window closes.
/// </summary>
public sealed class GlobalHotKeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;

    public GlobalHotKeyManager(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(handle)
            ?? throw new InvalidOperationException("Window handle is not yet available.");
        _source.AddHook(WndProc);
    }

    /// <summary>Returns false if the OS refused the registration (e.g. already claimed by another app).</summary>
    public bool Register(ModifierKeys modifiers, Key key, Action handler)
    {
        var id = _nextId++;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (!NativeMethods.RegisterHotKey(_source.Handle, id, (uint)modifiers, vk))
            return false;

        _handlers[id] = handler;
        return true;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }

        return nint.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_source.Handle, id);

        _handlers.Clear();
        _source.RemoveHook(WndProc);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(nint hWnd, int id);
    }
}
