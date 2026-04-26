using System.Globalization;

namespace kal_sync.Converters;

public class DoubleToIntStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d ? Math.Round(d).ToString("N0", culture) : "0";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
