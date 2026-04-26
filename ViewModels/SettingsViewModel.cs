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

    // ── Form fields ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedBmr))]
    private double _weightKg;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedBmr))]
    private double _bodyFatPercent;

    /// <summary>
    /// Age exposed as double so it binds without type coercion issues to Stepper.Value (double).
    /// Cast to int is applied in Save().
    /// </summary>
    [ObservableProperty]
    private double _ageDouble;

    [ObservableProperty]
    private string _selectedSex = "Male";

    [ObservableProperty]
    private double _surplusPercent;

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

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsViewModel(UserProfileService profileService)
    {
        _profileService = profileService;
        LoadProfile();
    }

    private void LoadProfile()
    {
        var p      = _profileService.Load();
        WeightKg   = p.WeightKg;
        BodyFatPercent = p.BodyFatPercent;
        AgeDouble  = p.Age;
        SelectedSex = p.Sex == Sex.Female ? "Female" : "Male";
        SurplusPercent = p.SurplusPercent;
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
    }
}
