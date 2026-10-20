namespace ActionTyranno.Core.Models;

public class MacroAction
{
    public ActionType Type { get; set; }

    // Optional user-given label shown on the action card instead of the default type name
    public string? Name { get; set; }

    // MouseMove / MouseClick / MouseScroll: virtual-screen coordinates
    public int? X { get; set; }
    public int? Y { get; set; }

    // MouseClick only
    public MouseButton? Button { get; set; }
    public bool DoubleClick { get; set; }

    // MouseScroll only: signed wheel notches (positive = scroll up, negative = scroll down)
    public int? ScrollAmount { get; set; }

    // KeyPress only, e.g. "A", "Enter", "F6"
    public string? Key { get; set; }

    // KeyCombo only, e.g. ["Ctrl", "Alt", "Delete"]
    public List<string>? Keys { get; set; }

    // Delay action's own duration, or the pause after any action executes
    public int DelayAfterMs { get; set; }

    // ReadFile: FolderPath may be a folder (newest file matching FilePattern is used) or a file path
    public string? FolderPath { get; set; }
    public string? FilePattern { get; set; }

    // ReadFile / AiRequest: variable that receives the result
    public string? TargetVariable { get; set; }

    // AiRequest
    public int? PromptTemplateId { get; set; }
    public string? SaveResultPath { get; set; }

    // LaunchUrl
    public string? Url { get; set; }

    // PasteText: literal text, may contain {Variable} placeholders
    public string? Text { get; set; }
}
