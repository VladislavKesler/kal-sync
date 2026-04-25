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

    // ── Form fields ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedBmr))]
    private double weightKg;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalculatedBmr))]
    private double bodyFatPercent;

    [ObservableProperty]
    private int age;

    [ObservableProperty]
    private string selectedSex = "Male";

    [ObservableProperty]
    private double surplusPercent;

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
        var p = _profileService.Load();
        WeightKg = p.WeightKg;
        BodyFatPercent = p.BodyFatPercent;
        Age = p.Age;
        SelectedSex = p.Sex == Sex.Female ? "Female" : "Male";
        SurplusPercent = p.SurplusPercent;
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        _profileService.Save(new UserProfile
        {
            WeightKg = WeightKg,
            BodyFatPercent = BodyFatPercent,
            Age = Age,
            Sex = SelectedSex == "Female" ? Sex.Female : Sex.Male,
            SurplusPercent = SurplusPercent,
        });
    }
}
