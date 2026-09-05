using System.Globalization;
using System.Windows.Data;

namespace ActionTyranno.App.Formatting;

public class DelaySecondsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int delayMs ? DelaySecondsFormat.ToDisplayString(delayMs) + "초" : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
