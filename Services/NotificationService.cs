using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Storage;
#if ANDROID
using Android.App;
using Android.Content;
#endif

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

    /// <summary>Schedules (or replaces) the daily evening check notification via AlarmManager.</summary>
#if ANDROID
    public Task ScheduleEveningCheckAsync(double targetKcal, double bmr, double activeCalories)
    {
        if (!EveningCheckEnabled) return Task.CompletedTask;

        var context = Android.App.Application.Context;
        var intent  = new Intent(context, typeof(EveningCheckReceiver));
        intent.PutExtra("title", "Tagescheck 📊");
        intent.PutExtra("body",  BuildEveningCheckBody(targetKcal, bmr, activeCalories));

        var flags = OperatingSystem.IsAndroidVersionAtLeast(23)
            ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;

        var pending      = PendingIntent.GetBroadcast(context, 1001, intent, flags)!;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);

        var now    = Java.Util.Calendar.Instance!;
        var fireAt = Java.Util.Calendar.Instance!;
        fireAt.Set(Java.Util.CalendarField.HourOfDay,    EveningCheckHour);
        fireAt.Set(Java.Util.CalendarField.Minute,       0);
        fireAt.Set(Java.Util.CalendarField.Second,       0);
        fireAt.Set(Java.Util.CalendarField.Millisecond,  0);
        if (!fireAt.After(now))
            fireAt.Add(Java.Util.CalendarField.DayOfMonth, 1);

        alarmManager?.SetInexactRepeating(
            AlarmType.RtcWakeup,
            fireAt.TimeInMillis,
            AlarmManager.IntervalDay,
            pending);

        return Task.CompletedTask;
    }
#else
    [SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public Task ScheduleEveningCheckAsync(double targetKcal, double bmr, double activeCalories)
        => Task.CompletedTask;
#endif

    /// <summary>Builds the notification body string. Public static for unit-testability.</summary>
    public static string BuildEveningCheckBody(double targetKcal, double bmr, double activeCalories)
        => $"Tagesziel: {targetKcal:F0} kcal | Aktiv: {activeCalories:F0} kcal | BMR: {bmr:F0} kcal — On track?";
}
