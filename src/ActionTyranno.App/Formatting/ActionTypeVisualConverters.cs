using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Formatting;

/// <summary>Short Korean label for an action's type, for the bold card header.</summary>
public class ActionTypeLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not MacroAction action ? string.Empty : action.Type switch
        {
            ActionType.MouseMove => "마우스 이동",
            ActionType.MouseClick => action.DoubleClick ? "마우스 더블클릭" : "마우스 클릭",
            ActionType.KeyPress => "키 입력",
            ActionType.KeyCombo => "키 조합",
            ActionType.Delay => "대기",
            _ => action.Type.ToString()
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Pictographic glyph representing an action's type, for the card icon badge.</summary>
public class ActionTypeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not MacroAction action ? "•" : action.Type switch
        {
            ActionType.MouseMove => "✥",   // move cursor glyph
            ActionType.MouseClick => "◉",  // click target glyph
            ActionType.KeyPress => "⌨",    // keyboard glyph
            ActionType.KeyCombo => "⌨",
            ActionType.Delay => "⏱",       // stopwatch glyph
            _ => "•"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Per action-type accent color, looked up from the app's resource dictionary.</summary>
public class ActionTypeAccentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is MacroAction action ? action.Type switch
        {
            ActionType.MouseMove => "AccentMouseMove",
            ActionType.MouseClick => "AccentMouseClick",
            ActionType.KeyPress => "AccentKeyPress",
            ActionType.KeyCombo => "AccentKeyCombo",
            ActionType.Delay => "AccentDelay",
            _ => "AccentMouseClick"
        } : "AccentMouseClick";

        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
