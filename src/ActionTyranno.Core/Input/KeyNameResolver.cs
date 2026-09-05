namespace ActionTyranno.Core.Input;

/// <summary>
/// Maps human-typed key names (as stored in MacroAction.Key / Keys) to VirtualKey codes.
/// </summary>
public static class KeyNameResolver
{
    private static readonly Dictionary<string, VirtualKey> Map = BuildMap();

    public static bool TryResolve(string name, out VirtualKey key)
    {
        return Map.TryGetValue(name.Trim(), out key);
    }

    public static VirtualKey Resolve(string name)
    {
        if (TryResolve(name, out var key))
            return key;

        throw new ArgumentException($"Unknown key name: '{name}'", nameof(name));
    }

    private static Dictionary<string, VirtualKey> BuildMap()
    {
        var map = new Dictionary<string, VirtualKey>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backspace"] = VirtualKey.Backspace,
            ["Tab"] = VirtualKey.Tab,
            ["Enter"] = VirtualKey.Enter,
            ["Return"] = VirtualKey.Enter,
            ["Shift"] = VirtualKey.Shift,
            ["Ctrl"] = VirtualKey.Control,
            ["Control"] = VirtualKey.Control,
            ["Alt"] = VirtualKey.Alt,
            ["Menu"] = VirtualKey.Alt,
            ["Pause"] = VirtualKey.Pause,
            ["CapsLock"] = VirtualKey.CapsLock,
            ["Esc"] = VirtualKey.Escape,
            ["Escape"] = VirtualKey.Escape,
            ["Space"] = VirtualKey.Space,
            ["Spacebar"] = VirtualKey.Space,
            ["PageUp"] = VirtualKey.PageUp,
            ["PgUp"] = VirtualKey.PageUp,
            ["PageDown"] = VirtualKey.PageDown,
            ["PgDn"] = VirtualKey.PageDown,
            ["End"] = VirtualKey.End,
            ["Home"] = VirtualKey.Home,
            ["Left"] = VirtualKey.Left,
            ["Up"] = VirtualKey.Up,
            ["Right"] = VirtualKey.Right,
            ["Down"] = VirtualKey.Down,
            ["PrintScreen"] = VirtualKey.PrintScreen,
            ["Insert"] = VirtualKey.Insert,
            ["Ins"] = VirtualKey.Insert,
            ["Delete"] = VirtualKey.Delete,
            ["Del"] = VirtualKey.Delete,
            ["LWin"] = VirtualKey.LWin,
            ["RWin"] = VirtualKey.RWin,
            ["Win"] = VirtualKey.LWin,
            ["Windows"] = VirtualKey.LWin,
            ["Multiply"] = VirtualKey.Multiply,
            ["Add"] = VirtualKey.Add,
            ["Subtract"] = VirtualKey.Subtract,
            ["Decimal"] = VirtualKey.Decimal,
            ["Divide"] = VirtualKey.Divide,
            ["NumLock"] = VirtualKey.NumLock,
            ["ScrollLock"] = VirtualKey.ScrollLock,
            ["LShift"] = VirtualKey.LShift,
            ["RShift"] = VirtualKey.RShift,
            ["LCtrl"] = VirtualKey.LControl,
            ["LControl"] = VirtualKey.LControl,
            ["RCtrl"] = VirtualKey.RControl,
            ["RControl"] = VirtualKey.RControl,
            ["LAlt"] = VirtualKey.LAlt,
            ["RAlt"] = VirtualKey.RAlt,

            [";"] = VirtualKey.OemSemicolon,
            ["="] = VirtualKey.OemPlus,
            [","] = VirtualKey.OemComma,
            ["-"] = VirtualKey.OemMinus,
            ["."] = VirtualKey.OemPeriod,
            ["/"] = VirtualKey.OemQuestion,
            ["`"] = VirtualKey.OemTilde,
            ["["] = VirtualKey.OemOpenBrackets,
            ["\\"] = VirtualKey.OemPipe,
            ["]"] = VirtualKey.OemCloseBrackets,
            ["'"] = VirtualKey.OemQuotes,
        };

        for (var c = 'A'; c <= 'Z'; c++)
            map[c.ToString()] = Enum.Parse<VirtualKey>(c.ToString());

        for (var d = 0; d <= 9; d++)
            map[d.ToString()] = Enum.Parse<VirtualKey>("D" + d);

        for (var f = 1; f <= 12; f++)
            map["F" + f] = Enum.Parse<VirtualKey>("F" + f);

        for (var n = 0; n <= 9; n++)
            map["Numpad" + n] = Enum.Parse<VirtualKey>("Numpad" + n);

        return map;
    }
}
