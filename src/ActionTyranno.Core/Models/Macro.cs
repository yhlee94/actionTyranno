namespace ActionTyranno.Core.Models;

public class Macro
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MacroAction> Actions { get; set; } = new();

    /// <summary>Number of times to run the full action sequence. Minimum 1 (no repeat).</summary>
    public int RepeatCount { get; set; } = 1;

    /// <summary>
    /// Automatic wait applied after every MouseMove/MouseClick/MouseScroll action, so consecutive
    /// mouse actions don't each need an explicit Delay action inserted between them.
    /// </summary>
    public int MouseActionDelayMs { get; set; } = 500;
}
