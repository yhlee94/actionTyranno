using System.Globalization;
using System.Windows.Data;
using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Formatting;

/// <summary>
/// Only Delay actions carry a meaningful wait time - every other action type executes
/// instantly, so this shows "-" for them instead of a confusing "0초".
/// </summary>
public class DelaySecondsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MacroAction action || action.Type != ActionType.Delay)
            return "-";

        return DelaySecondsFormat.ToDisplayString(action.DelayAfterMs) + "초";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
