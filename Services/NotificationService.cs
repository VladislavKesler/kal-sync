using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Storage;

namespace kal_sync.Services;

public class NotificationService
{
    private const string EnabledKey  = "notif.measurement_reminder_enabled";
    private const string IntervalKey = "notif.measurement_reminder_interval_days";

    [SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public bool ReminderEnabled
    {
        get => Preferences.Get(EnabledKey, false);
        set => Preferences.Set(EnabledKey, value);
    }

    [SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public int ReminderIntervalDays
    {
        get => Preferences.Get(IntervalKey, 7);
        set => Preferences.Set(IntervalKey, value);
    }

    /// <summary>Returns true if reminder is enabled and the interval since the last
    /// measurement date has been exceeded.</summary>
    public bool IsDue(DateTime? lastMeasurementDate)
    {
        if (!ReminderEnabled || lastMeasurementDate is null) return false;
        return (DateTime.Today - lastMeasurementDate.Value.Date).TotalDays >= ReminderIntervalDays;
    }
}
