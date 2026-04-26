using System.Globalization;
using kal_sync.ViewModels;

namespace kal_sync.Converters;

/// <summary>
/// Receives the entire SettingsViewModel and returns LBM = weight × (1 − bodyFat / 100).
/// Used in SettingsPage to show live LBM alongside the BMR preview.
/// </summary>
public class LeanBodyMassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SettingsViewModel vm)
            return vm.WeightKg * (1.0 - vm.BodyFatPercent / 100.0);
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
