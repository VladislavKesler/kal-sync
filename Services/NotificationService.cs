using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;

namespace kal_sync.Services;

public class NotificationService
{
    // ── Measurement reminder ─────────────────────────────────────────────────
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

    // ── Evening check ────────────────────────────────────────────────────────
    private const string EveningCheckEnabledKey = "notif.evening_check_enabled";
    private const string EveningCheckHourKey    = "notif.evening_check_hour";
    private const int    EveningCheckNotifId    = 1001;

    [SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public bool EveningCheckEnabled
    {
        get => Preferences.Get(EveningCheckEnabledKey, false);
        set => Preferences.Set(EveningCheckEnabledKey, value);
    }

    [SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public int EveningCheckHour
    {
        get => Preferences.Get(EveningCheckHourKey, 20);
        set => Preferences.Set(EveningCheckHourKey, value);
    }

    /// <summary>Schedules (or replaces) the daily evening check notification.</summary>
    public async Task ScheduleEveningCheckAsync(double targetKcal, double bmr, double activeCalories)
    {
        if (!EveningCheckEnabled) return;

        _ = await LocalNotificationCenter.Current.RequestNotificationPermission();

        var now        = DateTime.Now;
        var notifyTime = new DateTime(now.Year, now.Month, now.Day, EveningCheckHour, 0, 0);
        if (notifyTime <= now)
            notifyTime = notifyTime.AddDays(1);

        await LocalNotificationCenter.Current.Schedule(new NotificationRequest
        {
            NotificationId = EveningCheckNotifId,
            Title          = "Tagescheck 📊",
            Description    = BuildEveningCheckBody(targetKcal, bmr, activeCalories),
            Schedule       = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime,
                RepeatType = NotificationRepeat.Daily,
            },
        });
    }

    /// <summary>Builds the notification body string. Public static for unit-testability.</summary>
    public static string BuildEveningCheckBody(double targetKcal, double bmr, double activeCalories)
        => $"Tagesziel: {targetKcal:F0} kcal | Aktiv: {activeCalories:F0} kcal | BMR: {bmr:F0} kcal — On track?";
}
