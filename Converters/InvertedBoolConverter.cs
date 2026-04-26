using System.Globalization;

namespace kal_sync.Converters;

/// <summary>
/// For bool input : inverts the value (true → false, false → true).
/// For string input: returns true when the string is NOT null/empty (used to show error labels).
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s) return !string.IsNullOrEmpty(s);
        if (value is bool b)   return !b;
        return value is null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}
