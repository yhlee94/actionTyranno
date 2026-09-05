using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Formatting;

public static class MacroActionFormatter
{
    /// <summary>Full description including the type name (e.g. for a flat list without a separate type header).</summary>
    public static string Describe(MacroAction action) => action.Type switch
    {
        ActionType.MouseMove => $"마우스 이동 ({action.X}, {action.Y})",
        ActionType.MouseClick => $"마우스 {(action.DoubleClick ? "더블클릭" : "클릭")} [{action.Button}] ({action.X}, {action.Y})",
        ActionType.KeyPress => $"키 입력 [{action.Key}]",
        ActionType.KeyCombo => $"키 조합 [{string.Join(" + ", action.Keys ?? new List<string>())}]",
        ActionType.Delay => "대기",
        _ => action.Type.ToString()
    };

    /// <summary>Just the parameters, for use under a card header that already shows the type name.</summary>
    public static string DescribeParamsOnly(MacroAction action) => action.Type switch
    {
        ActionType.MouseMove => $"좌표 ({action.X}, {action.Y})",
        ActionType.MouseClick => $"[{action.Button}] 좌표 ({action.X}, {action.Y})",
        ActionType.KeyPress => $"[{action.Key}]",
        ActionType.KeyCombo => $"[{string.Join(" + ", action.Keys ?? new List<string>())}]",
        ActionType.Delay => string.Empty,
        _ => string.Empty
    };
}
