using System.Globalization;
using System.Windows.Data;

namespace ProjectApp.Utils.Converters;

public class EnumToBooleanConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? Enum.Parse(targetType, parameter.ToString()) : Binding.DoNothing;
    }
}