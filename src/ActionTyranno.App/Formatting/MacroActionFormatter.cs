using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Formatting;

public static class MacroActionFormatter
{
    public static string Describe(MacroAction action) => action.Type switch
    {
        ActionType.MouseMove => $"마우스 이동 ({action.X}, {action.Y})",
        ActionType.MouseClick => $"마우스 {(action.DoubleClick ? "더블클릭" : "클릭")} [{action.Button}] ({action.X}, {action.Y})",
        ActionType.KeyPress => $"키 입력 [{action.Key}]",
        ActionType.KeyCombo => $"키 조합 [{string.Join(" + ", action.Keys ?? new List<string>())}]",
        ActionType.Delay => "대기",
        _ => action.Type.ToString()
    };
}
