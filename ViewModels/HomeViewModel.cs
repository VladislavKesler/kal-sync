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
    [NotifyPropertyChangedFor(nameof(DeficitPercent))]
    private double _bmr;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(DeficitPercent))]
    private double _activeCalories;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeficitPercent))]
    [NotifyPropertyChangedFor(nameof(SurplusLabel))]
    private double _tdee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(SurplusLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentEyebrowLabel))]
    [NotifyPropertyChangedFor(nameof(AdjustmentSubLabel))]
    [NotifyPropertyChangedFor(nameof(DeficitPercent))]
    private double _deficitCap;

    /// <summary>Computed: BMR + max(0, activeCalories − deficitCap), floored at BMR.</summary>
    public double TargetKcal => GaintainingService.CalculateTargetKcal(Bmr, ActiveCalories, DeficitCap);

    /// <summary>Deficit as a % of TDEE — drives the ring's red arc.</summary>
    public double DeficitPercent => Tdee > 0 ? (Tdee - TargetKcal) / Tdee * 100.0 : 0.0;

    /// <summary>Badge text inside the target ring, e.g. "− 300 kcal Defizit".</summary>
    public string SurplusLabel
    {
        get
        {
            double deficit = Tdee - TargetKcal;
            return deficit > 0.5 ? $"− {deficit:F0} kcal Defizit" : "Erhalt";
        }
    }

    /// <summary>Eyebrow label in the adjustment card.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "XAML compiled bindings require instance properties.")]
    public string AdjustmentEyebrowLabel => "Defizit-Cap";

    /// <summary>Subtitle in the adjustment card.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "XAML compiled bindings require instance properties.")]
    public string AdjustmentSubLabel => "Lean Bulk";

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
                Activity       = activity;
                Bmr            = Math.Round(_userProfileService.GetBmr(profile), 0);
                ActiveCalories = activity.CalculatedCalories;
                Tdee           = GaintainingService.CalculateTdee(Bmr, ActiveCalories);
                DeficitCap     = profile.DeficitCap;
                HasData        = true;

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
