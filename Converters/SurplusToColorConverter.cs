using System.Globalization;

namespace kal_sync.Converters;

/// <summary>
/// Returns a color based on the sign of a surplus/deficit percentage.
/// ConverterParameter="fg" → text color (AccentInk / Danger)
/// ConverterParameter="bg" → soft background (AccentBg / DangerBg)
/// </summary>
public class SurplusToColorConverter : IValueConverter
{
    static readonly Color AccentInk = Color.FromArgb("#3F6E1F");
    static readonly Color AccentBg  = Color.FromArgb("#E8F2D9");
    static readonly Color Danger    = Color.FromArgb("#C0533A");
    static readonly Color DangerBg  = Color.FromArgb("#F4DAD2");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v     = System.Convert.ToDouble(value, culture);
        var which = (parameter as string) ?? "fg";
        if (v >= 0) return which == "bg" ? AccentBg : AccentInk;
        return which == "bg" ? DangerBg : Danger;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
