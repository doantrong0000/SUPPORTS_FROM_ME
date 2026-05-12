using System;
using System.Globalization;
using System.Windows.Data;

namespace Calculator_Foundation_Etabs_API.Models
{
    public class FractionConverter : IValueConverter
    {
        // Từ double (Model) -> string (UI)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString(culture);
            }
            return value?.ToString() ?? "";
        }

        // Từ string (UI) -> double (Model) - Cho phép nhập phân số
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value as string;
            if (string.IsNullOrWhiteSpace(input)) return 0.0;

            input = input.Trim().Replace(",", "."); // Chuẩn hóa dấu thập phân

            if (input.Contains("/"))
            {
                string[] parts = input.Split('/');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numerator) &&
                        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double denominator) &&
                        denominator != 0)
                    {
                        return numerator / denominator;
                    }
                }
            }

            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return 0.0;
        }
    }
}
