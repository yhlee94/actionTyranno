using System.Globalization;
using System.Windows.Data;
using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Formatting;

public class MacroActionDetailConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is MacroAction action ? MacroActionFormatter.DescribeParamsOnly(action) : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
