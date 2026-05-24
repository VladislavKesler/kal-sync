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
    [NotifyPropertyChangedFor(nameof(SurplusLabel))]
    private double _surplusPercent;

    public string SurplusLabel => SurplusPercent >= 0 ? "Überschuss" : "Defizit";

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

    public SettingsViewModel(UserProfileService profileService, NotificationService notificationService)
    {
        _profileService      = profileService;
        _notificationService = notificationService;
        LoadProfile();
        LoadNotificationSettings();
    }

    private void LoadProfile()
    {
        var p          = _profileService.Load();
        WeightKg       = p.WeightKg;
        BodyFatPercent = p.BodyFatPercent;
        AgeDouble      = p.Age;
        SelectedSex    = p.Sex == Sex.Female ? "Female" : "Male";
        SurplusPercent = p.SurplusPercent;
        BackendUrl        = _profileService.GetBackendUrl();
        UsbDebuggingEnabled = Preferences.Get("dev.usb_debugging", false);
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
    private void Save()
    {
        _profileService.Save(new UserProfile
        {
            WeightKg       = WeightKg,
            BodyFatPercent = BodyFatPercent,
            Age            = (int)Math.Round(AgeDouble),
            Sex            = SelectedSex == "Female" ? Sex.Female : Sex.Male,
            SurplusPercent = SurplusPercent,
        });

        _notificationService.ReminderEnabled = MeasurementReminderEnabled;
        int idx = ReminderIntervalOptions.IndexOf(SelectedReminderInterval);
        _notificationService.ReminderIntervalDays = idx >= 0 ? IntervalDays[idx] : 7;

        Preferences.Set("dev.usb_debugging", UsbDebuggingEnabled);

        if (UsbDebuggingEnabled && !string.IsNullOrWhiteSpace(BackendUrl))
            _profileService.SaveBackendUrl(BackendUrl.TrimEnd('/'));
    }
}
