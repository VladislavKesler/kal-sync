using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly UserProfileService _profileService;
    private readonly NotificationService _notificationService;
    private readonly WidgetService _widgetService;

    // Prevents RequestPinWidget() from firing during initial profile load
    private bool _profileLoaded;

    private static readonly int[] IntervalDays = [1, 3, 7, 14, 30];

    // ── Form fields ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedBmr))]
    private double _weightKg;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedBmr))]
    private double _bodyFatPercent;

    /// <summary>Age as double so it binds to Stepper.Value without coercion; cast to int in Save().</summary>
    [ObservableProperty]
    private double _ageDouble;

    [ObservableProperty]
    private string _selectedSex = "Male";

    [ObservableProperty]
    private double _calorieAdjustment;

    [ObservableProperty]
    private string _backendUrl = string.Empty;

    // ── Developer options ────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _usbDebuggingEnabled;

    // ── Notification settings ────────────────────────────────────────────────

    [ObservableProperty]
    private bool _measurementReminderEnabled;

    [ObservableProperty]
    private string _selectedReminderInterval = "Wöchentlich";

    // ── Widget ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _widgetEnabled;

    partial void OnWidgetEnabledChanged(bool value)
    {
        Preferences.Set("widget.enabled", value);
        if (value && _profileLoaded)
            _widgetService.RequestPinWidget();
    }

    // ── Derived display ──────────────────────────────────────────────────────

    /// <summary>Katch-McArdle BMR, recomputed whenever weight or body-fat changes.</summary>
    public double CalculatedBmr
    {
        get
        {
            double lbm = UserProfileService.CalculateLeanBodyMass(WeightKg, BodyFatPercent);
            return Math.Round(UserProfileService.CalculateBmr(lbm), 0);
        }
    }

    // ── Picker options ───────────────────────────────────────────────────────

    public List<string> SexOptions { get; } = ["Male", "Female"];

    public List<string> ReminderIntervalOptions { get; } =
        ["Täglich", "Alle 3 Tage", "Wöchentlich", "Alle 2 Wochen", "Monatlich"];

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsViewModel(UserProfileService profileService, NotificationService notificationService,
                             WidgetService widgetService)
    {
        _profileService      = profileService;
        _notificationService = notificationService;
        _widgetService       = widgetService;
        LoadProfile();
        LoadNotificationSettings();
    }

    private void LoadProfile()
    {
        var p               = _profileService.Load();
        WeightKg            = p.WeightKg;
        BodyFatPercent      = p.BodyFatPercent;
        AgeDouble           = p.Age;
        SelectedSex         = p.Sex == Sex.Female ? "Female" : "Male";
        CalorieAdjustment   = p.CalorieAdjustment;
        BackendUrl          = _profileService.GetBackendUrl();
        UsbDebuggingEnabled = Preferences.Get("dev.usb_debugging", false);
        WidgetEnabled       = Preferences.Get("widget.enabled", false);
        _profileLoaded      = true;  // Must be set after all properties are loaded
    }

    private void LoadNotificationSettings()
    {
        MeasurementReminderEnabled = _notificationService.ReminderEnabled;
        int days = _notificationService.ReminderIntervalDays;
        int idx  = Array.IndexOf(IntervalDays, days);
        SelectedReminderInterval = idx >= 0 ? ReminderIntervalOptions[idx] : "Wöchentlich";
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task Save()
    {
        _profileService.Save(new UserProfile
        {
            WeightKg       = WeightKg,
            BodyFatPercent = BodyFatPercent,
            Age            = (int)Math.Round(AgeDouble),
            Sex            = SelectedSex == "Female" ? Sex.Female : Sex.Male,
            CalorieAdjustment = CalorieAdjustment,
        });

        _notificationService.ReminderEnabled = MeasurementReminderEnabled;
        int idx = ReminderIntervalOptions.IndexOf(SelectedReminderInterval);
        _notificationService.ReminderIntervalDays = idx >= 0 ? IntervalDays[idx] : 7;

        Preferences.Set("dev.usb_debugging", UsbDebuggingEnabled);

        if (UsbDebuggingEnabled && !string.IsNullOrWhiteSpace(BackendUrl))
            _profileService.SaveBackendUrl(BackendUrl.TrimEnd('/'));

        await Shell.Current.DisplayAlert(
            "Gespeichert",
            "Deine Einstellungen wurden übernommen.",
            "OK");
    }
}
