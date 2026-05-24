using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync.ViewModels;

/// <summary>
/// ViewModel for HomePage.
/// Displays the calorie dashboard: BMR, active calories, TDEE and calorie target.
/// Also exposes the last Garmin activity for the activity detail row.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly GarminApiService _apiService;
    private readonly UserProfileService _userProfileService;

    // ── Calorie dashboard ────────────────────────────────────────────────────

    [ObservableProperty]
    private double _bmr;

    [ObservableProperty]
    private double _activeCalories;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    private double _tdee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(SurplusLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentEyebrowLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentSubLabel))]
    private double _surplusPercent;

    /// <summary>Computed live: TDEE × (1 + SurplusPercent / 100).</summary>
    public double TargetKcal => GaintainingService.CalculateTargetKcal(Tdee, SurplusPercent);

    /// <summary>Badge text inside the target ring, e.g. "inkl. 5.0 % Überschuss".</summary>
    public string SurplusLabel => SurplusPercent >= 0
        ? $"inkl. {SurplusPercent:F1} % Überschuss"
        : $"inkl. {Math.Abs(SurplusPercent):F1} % Defizit";

    /// <summary>Eyebrow label in the adjustment card ("Überschuss" / "Defizit" / "Erhalt").</summary>
    public string AdjustmentEyebrowLabel => SurplusPercent > 0 ? "Überschuss" : SurplusPercent < 0 ? "Defizit" : "Erhalt";

    /// <summary>Subtitle in the adjustment card ("Lean Bulk" / "Diät" / "Gleichgewicht").</summary>
    public string AdjustmentSubLabel => SurplusPercent > 0 ? "Lean Bulk" : SurplusPercent < 0 ? "Diät" : "Gleichgewicht";

    /// <summary>Formatted date shown in the top bar (e.g. "Montag, 27. April").</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1822:Mark members as static",
        Justification = "XAML compiled bindings require instance properties.")]
    public string TodayLabel => DateTime.Today.ToString("dddd, d. MMMM", CultureInfo.CurrentCulture);

    // ── Last Garmin activity (for the activity detail row) ───────────────────

    [ObservableProperty]
    private ActivityResponse? _activity;

    // ── State ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasData;

    private DateTime _lastLoaded = DateTime.MinValue;

    public HomeViewModel(GarminApiService apiService, UserProfileService userProfileService)
    {
        _apiService = apiService;
        _userProfileService = userProfileService;
    }

    /// <summary>Load calorie dashboard from user profile + latest Garmin activity.</summary>
    [RelayCommand]
    public async Task LoadLatestActivity()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Debug.WriteLine("[HomeViewModel] Loading calorie dashboard...");

            var profile  = _userProfileService.Load();
            var activity = await _apiService.GetLatestActivityAsync();

            if (activity != null)
            {
                Activity        = activity;
                Bmr             = Math.Round(_userProfileService.GetBmr(profile), 0);
                ActiveCalories  = activity.CalculatedCalories;
                Tdee            = GaintainingService.CalculateTdee(Bmr, ActiveCalories);
                SurplusPercent  = profile.SurplusPercent;
                HasData         = true;
            }
            else
            {
                HasData = false;
            }

            Debug.WriteLine(
                $"[HomeViewModel] BMR={Bmr}, ActiveCal={ActiveCalories}, " +
                $"TDEE={Tdee}, Target={TargetKcal}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomeViewModel] Error: {ex.Message}");
            ErrorMessage = $"Fehler beim Laden: {ex.Message}";
            HasData = false;
        }
        finally
        {
            IsLoading = false;
        }

        _lastLoaded = DateTime.Now;
    }

    [RelayCommand]
    public async Task RefreshData() => await LoadLatestActivity();

    /// <summary>Refresh on every appearance, but at most once per 30 minutes.</summary>
    [RelayCommand]
    public async Task PageAppearing()
    {
        if (!HasData || (DateTime.Now - _lastLoaded).TotalMinutes >= 30)
            await LoadLatestActivity();
    }
}
