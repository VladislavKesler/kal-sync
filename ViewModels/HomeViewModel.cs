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
    private readonly UpdateService _updateService;
    private readonly WidgetService _widgetService;

    // ── Calorie dashboard ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(AdjustmentPercent))]
    private double _bmr;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(AdjustmentPercent))]
    private double _activeCalories;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(AdjustmentPercent))]
    [NotifyPropertyChangedFor(nameof(SurplusLabel))]
    private double _tdee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(SurplusLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentEyebrowLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentSubLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentPercent))]
    private double _calorieAdjustment;

    /// <summary>Computed: TDEE + calorieAdjustment, floored at BMR.</summary>
    public double TargetKcal => GaintainingService.CalculateTargetKcal(Tdee, CalorieAdjustment, Bmr);

    /// <summary>Signed % of TDEE — drives the ring arc (positive = green, negative = red).</summary>
    public double AdjustmentPercent => Tdee > 0 ? CalorieAdjustment / Tdee * 100.0 : 0.0;

    /// <summary>Badge text inside the target ring.</summary>
    public string SurplusLabel => CalorieAdjustment > 0.5
        ? $"+ {CalorieAdjustment:F0} kcal Überschuss"
        : CalorieAdjustment < -0.5
            ? $"− {Math.Abs(CalorieAdjustment):F0} kcal Defizit"
            : "Erhalt";

    /// <summary>Eyebrow label in the adjustment card.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "XAML compiled bindings require instance properties.")]
    public string AdjustmentEyebrowLabel => CalorieAdjustment > 0 ? "Überschuss" : CalorieAdjustment < 0 ? "Defizit" : "Erhalt";

    /// <summary>Subtitle in the adjustment card.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "XAML compiled bindings require instance properties.")]
    public string AdjustmentSubLabel => CalorieAdjustment > 0 ? "Lean Bulk" : CalorieAdjustment < 0 ? "Diät" : "Gleichgewicht";

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

    // ── Update ──────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _availableVersion = string.Empty;

    [ObservableProperty]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    private int _updateProgress;

    private DateTime _lastLoaded = DateTime.MinValue;

    public HomeViewModel(GarminApiService apiService, UserProfileService userProfileService,
                         UpdateService updateService, WidgetService widgetService)
    {
        _apiService         = apiService;
        _userProfileService = userProfileService;
        _updateService      = updateService;
        _widgetService      = widgetService;
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
                Activity          = activity;
                Bmr               = Math.Round(_userProfileService.GetBmr(profile), 0);
                ActiveCalories    = activity.CalculatedCalories;
                Tdee              = GaintainingService.CalculateTdee(Bmr, ActiveCalories);
                CalorieAdjustment = profile.CalorieAdjustment;
                HasData           = true;

                double surplusFrac = Tdee > 0 ? (TargetKcal - Tdee) / Tdee * 100.0 : 0.0;
                _widgetService.UpdateData(TargetKcal, surplusFrac);
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

        // Non-blocking: check for update in background after data is loaded
        _ = CheckForUpdateInBackgroundAsync();
    }

    private async Task CheckForUpdateInBackgroundAsync()
    {
        var version = await _updateService.CheckForUpdateAsync();
        if (version is not null)
        {
            AvailableVersion = version;
            UpdateAvailable  = true;
        }
    }

    [RelayCommand]
    private async Task DownloadAndRestart()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Update installieren",
            $"Version {AvailableVersion} wird heruntergeladen. Die App startet danach automatisch neu.",
            "Installieren",
            "Abbrechen");

        if (!confirmed) return;

        IsDownloadingUpdate = true;
        try
        {
            var progress = new Progress<int>(p => UpdateProgress = p);
            await _updateService.DownloadAndRestartAsync(progress);
        }
        catch
        {
            IsDownloadingUpdate = false;
        }
    }
}
