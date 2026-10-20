using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Formatting;

public static class MacroActionFormatter
{
    /// <summary>Full description including the type name (e.g. for a flat list without a separate type header).</summary>
    public static string Describe(MacroAction action) => action.Type switch
    {
        ActionType.MouseMove => $"마우스 이동 ({action.X}, {action.Y})",
        ActionType.MouseClick => $"마우스 {(action.DoubleClick ? "더블클릭" : "클릭")} [{action.Button}] ({action.X}, {action.Y})",
        ActionType.MouseScroll => $"마우스 스크롤 {ScrollDirectionLabel(action)} {Math.Abs(action.ScrollAmount ?? 0)}칸 ({action.X}, {action.Y})",
        ActionType.KeyPress => $"키 입력 [{action.Key}]",
        ActionType.KeyCombo => $"키 조합 [{string.Join(" + ", action.Keys ?? new List<string>())}]",
        ActionType.Delay => "대기",
        ActionType.ReadFile => $"파일 읽기 {DescribeParamsOnly(action)}",
        ActionType.AiRequest => $"AI 요청 {DescribeParamsOnly(action)}",
        ActionType.LaunchUrl => $"URL 열기 {DescribeParamsOnly(action)}",
        ActionType.PasteText => $"텍스트 붙여넣기 {DescribeParamsOnly(action)}",
        _ => action.Type.ToString()
    };

    /// <summary>Just the parameters, for use under a card header that already shows the type name.</summary>
    public static string DescribeParamsOnly(MacroAction action) => action.Type switch
    {
        ActionType.MouseMove => $"좌표 ({action.X}, {action.Y})",
        ActionType.MouseClick => $"[{action.Button}] 좌표 ({action.X}, {action.Y})",
        ActionType.MouseScroll => $"{ScrollDirectionLabel(action)} {Math.Abs(action.ScrollAmount ?? 0)}칸 · 좌표 ({action.X}, {action.Y})",
        ActionType.KeyPress => $"[{action.Key}]",
        ActionType.KeyCombo => $"[{string.Join(" + ", action.Keys ?? new List<string>())}]",
        ActionType.Delay => string.Empty,
        ActionType.ReadFile => $"{action.FolderPath}" +
            (string.IsNullOrWhiteSpace(action.FilePattern) ? string.Empty : $" [{action.FilePattern}]") +
            $" → {{{action.TargetVariable}}}",
        ActionType.AiRequest => $"프롬프트 #{action.PromptTemplateId} → {{{(string.IsNullOrWhiteSpace(action.TargetVariable) ? "AiResult" : action.TargetVariable)}}}",
        ActionType.LaunchUrl => action.Url ?? string.Empty,
        ActionType.PasteText => Truncate(action.Text, 60),
        _ => string.Empty
    };

    private static string Truncate(string? text, int max)
    {
        var oneLine = (text ?? string.Empty).ReplaceLineEndings(" ");
        return oneLine.Length > max ? oneLine[..max] + "…" : oneLine;
    }

    private static string ScrollDirectionLabel(MacroAction action) => (action.ScrollAmount ?? 0) < 0 ? "아래로" : "위로";
}
