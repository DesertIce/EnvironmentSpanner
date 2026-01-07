using System.Globalization;
using System.Windows.Data;

namespace EnvironmentSpanner;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            bool boolValue => !boolValue,
            _ => true
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            bool boolValue => !boolValue,
            _ => false
        };
}
