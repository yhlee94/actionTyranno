namespace ActionTyranno.Core.Models;

public class Macro
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MacroAction> Actions { get; set; } = new();
}
