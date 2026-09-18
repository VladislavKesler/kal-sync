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
    private readonly CalibrationService _calibrationService;

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
    private string _backendUrl = string.Empty;

    // ── Training ─────────────────────────────────────────────────────────────

    /// <summary>Which sport the watch's "Cardio" profile stands for (drives the backend formula).</summary>
    [ObservableProperty]
    private string _selectedCardioSport = "Tennis (Einzel)";

    // ── Developer options ────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _usbDebuggingEnabled;

    // ── Notification settings ────────────────────────────────────────────────

    [ObservableProperty]
    private bool _measurementReminderEnabled;

    [ObservableProperty]
    private string _selectedReminderInterval = "Wöchentlich";

    [ObservableProperty]
    private bool _eveningCheckEnabled;

    [ObservableProperty]
    private string _selectedEveningCheckTime = "20:00";

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

    /// <summary>Index-aligned with <see cref="CardioSport"/> enum values.</summary>
    public List<string> CardioSportOptions { get; } =
        ["Tennis (Einzel)", "Tennis (Doppel)", "Allgemein (nur Puls)"];

    public List<string> ReminderIntervalOptions { get; } =
        ["Täglich", "Alle 3 Tage", "Wöchentlich", "Alle 2 Wochen", "Monatlich"];

    public List<string> EveningCheckTimeOptions { get; } =
        ["18:00", "19:00", "20:00", "21:00", "22:00"];

    // ── Calibration display ─────────────────────────────────────────────────

    /// <summary>Current self-calibration correction factor applied to Keytel-estimated activity calories.</summary>
    public double CorrectionFactor => _calibrationService.CorrectionFactor;

    /// <summary>Display text, e.g. "1.00× (noch keine Kalibrierung)" or "0.92× (zuletzt: 12.05.2026)".</summary>
    public string CorrectionFactorLabel => _calibrationService.LastCalibrationDate is { } lastDate
        ? $"{CorrectionFactor:F2}× (zuletzt: {lastDate:dd.MM.yyyy})"
        : $"{CorrectionFactor:F2}× (noch keine Kalibrierung — mind. {CalibrationService.MinBalanceDaysForCalibration} Tage Trend-Daten nötig)";

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsViewModel(UserProfileService profileService, NotificationService notificationService,
                             WidgetService widgetService, CalibrationService calibrationService)
    {
        _profileService      = profileService;
        _notificationService = notificationService;
        _widgetService       = widgetService;
        _calibrationService  = calibrationService;
        LoadProfile();
        LoadNotificationSettings();
    }

    private void LoadProfile()
    {
        var p               = _profileService.Load();
        WeightKg       = p.WeightKg;
        BodyFatPercent = p.BodyFatPercent;
        AgeDouble      = p.Age;
        SelectedSex    = p.Sex == Sex.Female ? "Female" : "Male";
        SelectedCardioSport = CardioSportOptions[(int)p.CardioSport];
        BackendUrl     = _profileService.GetBackendUrl();
        UsbDebuggingEnabled = Preferences.Get("dev.usb_debugging", false);
        WidgetEnabled       = Preferences.Get("widget.enabled", false);
        _profileLoaded      = true;  // Must be set after all properties are loaded
    }

    private void LoadNotificationSettings()
    {
        MeasurementReminderEnabled  = _notificationService.ReminderEnabled;
        int days = _notificationService.ReminderIntervalDays;
        int idx  = Array.IndexOf(IntervalDays, days);
        SelectedReminderInterval    = idx >= 0 ? ReminderIntervalOptions[idx] : "Wöchentlich";
        EveningCheckEnabled         = _notificationService.EveningCheckEnabled;
        SelectedEveningCheckTime    = $"{_notificationService.EveningCheckHour:00}:00";
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task Save()
    {
        var existing = _profileService.Load();
        _profileService.Save(new UserProfile
        {
            WeightKg          = WeightKg,
            BodyFatPercent    = BodyFatPercent,
            Age               = (int)Math.Round(AgeDouble),
            Sex               = SelectedSex == "Female" ? Sex.Female : Sex.Male,
            CalorieAdjustment = existing.CalorieAdjustment,
            CardioSport       = (CardioSport)Math.Max(CardioSportOptions.IndexOf(SelectedCardioSport), 0),
        });

        _notificationService.ReminderEnabled      = MeasurementReminderEnabled;
        int idx = ReminderIntervalOptions.IndexOf(SelectedReminderInterval);
        _notificationService.ReminderIntervalDays = idx >= 0 ? IntervalDays[idx] : 7;
        _notificationService.EveningCheckEnabled  = EveningCheckEnabled;
        _notificationService.EveningCheckHour     = ParseHour(SelectedEveningCheckTime);

        Preferences.Set("dev.usb_debugging", UsbDebuggingEnabled);

        if (UsbDebuggingEnabled && !string.IsNullOrWhiteSpace(BackendUrl))
            _profileService.SaveBackendUrl(BackendUrl.TrimEnd('/'));

        await Shell.Current.DisplayAlert(
            "Gespeichert",
            "Deine Einstellungen wurden übernommen.",
            "OK");
    }

    // Parses "20:00" → 20
    private static int ParseHour(string timeString)
        => int.TryParse(timeString.Split(':')[0], out int h) ? h : 20;
}
