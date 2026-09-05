namespace ActionTyranno.Core.Models;

public class MacroAction
{
    public ActionType Type { get; set; }

    // MouseMove / MouseClick: virtual-screen coordinates
    public int? X { get; set; }
    public int? Y { get; set; }

    // MouseClick only
    public MouseButton? Button { get; set; }

    // KeyPress only, e.g. "A", "Enter", "F6"
    public string? Key { get; set; }

    // KeyCombo only, e.g. ["Ctrl", "Alt", "Delete"]
    public List<string>? Keys { get; set; }

    // Delay action's own duration, or the pause after any action executes
    public int DelayAfterMs { get; set; }
}
