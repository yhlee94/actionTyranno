using ActionTyranno.Core.Models;
using static ActionTyranno.Core.Input.NativeMethods;
using MouseButton = ActionTyranno.Core.Models.MouseButton;

namespace ActionTyranno.Core.Input;

/// <summary>
/// Simulates mouse and keyboard input via Win32 APIs (SendInput / GetCursorPos / SetCursorPos).
/// </summary>
public class InputSimulator
{
    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out var point);
        return (point.X, point.Y);
    }

    public static VirtualScreenBounds GetVirtualScreenBounds()
    {
        var x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        return new VirtualScreenBounds(x, y, width, height);
    }

    public void MoveMouseTo(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public void Click(int x, int y, MouseButton button = MouseButton.Left)
    {
        MoveMouseTo(x, y);
        SendClick(button);
    }

    /// <summary>
    /// Two clicks close enough together in time and position for Windows' own double-click
    /// detection (GetDoubleClickTime/GetSystemMetrics(SM_CXDOUBLECLK)) to register them as one.
    /// </summary>
    public void DoubleClick(int x, int y, MouseButton button = MouseButton.Left)
    {
        MoveMouseTo(x, y);
        SendClick(button);
        SendClick(button);
    }

    private static void SendClick(MouseButton button)
    {
        var (downFlag, upFlag) = button switch
        {
            MouseButton.Left => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP),
            MouseButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
            MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        SendMouseInput(downFlag);
        SendMouseInput(upFlag);
    }

    public void KeyPress(string key)
    {
        var vk = KeyNameResolver.Resolve(key);
        SendKeyInput(vk, keyUp: false);
        SendKeyInput(vk, keyUp: true);
    }

    public void KeyCombo(IEnumerable<string> keys)
    {
        var vks = keys.Select(KeyNameResolver.Resolve).ToList();
        if (vks.Count == 0)
            return;

        foreach (var vk in vks)
            SendKeyInput(vk, keyUp: false);

        for (var i = vks.Count - 1; i >= 0; i--)
            SendKeyInput(vks[i], keyUp: true);
    }

    private static void SendMouseInput(uint flags)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } }
        };
        SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyInput(VirtualKey vk, bool keyUp)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                }
            }
        };
        SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
    }
}
