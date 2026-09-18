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
    private readonly GarminApiService    _apiService;
    private readonly UserProfileService  _userProfileService;
    private readonly WidgetService       _widgetService;
    private readonly NotificationService _notificationService;
    private readonly WorkerDataService   _workerDataService;
    private readonly DatabaseService     _databaseService;
    private readonly CalibrationService  _calibrationService;

    // ── Calorie dashboard ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(AdjustmentPercent))]
    private double _bmr;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetKcal))]
    [NotifyPropertyChangedFor(nameof(AdjustmentPercent))]
    [NotifyPropertyChangedFor(nameof(RecommendedAdjustment))]
    [NotifyPropertyChangedFor(nameof(RecommendedAdjustmentLabel))]
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

    /// <summary>Automatically suggested CalorieAdjustment (-500 kcal, +100 kcal buffer on high-activity days). Shown next to the slider, never applied automatically.</summary>
    public double RecommendedAdjustment => GaintainingService.CalculateRecommendedAdjustment(ActiveCalories);

    /// <summary>Display text for the recommendation, e.g. "Empfohlen: −420 kcal".</summary>
    public string RecommendedAdjustmentLabel => $"Empfohlen: {RecommendedAdjustment:+0;−0;0} kcal";

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
    [NotifyPropertyChangedFor(nameof(IsLowConfidenceActivity))]
    [NotifyPropertyChangedFor(nameof(ConfidenceNote))]
    private ActivityResponse? _activity;

    /// <summary>True when the last activity's calorie estimate has low confidence (e.g. strength training).</summary>
    public bool IsLowConfidenceActivity => Activity?.Confidence == "low";

    /// <summary>Explanation shown in the low-confidence tooltip, if any.</summary>
    public string? ConfidenceNote => Activity?.ConfidenceNote;

    // ── State ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasData;

    private DateTime _lastLoaded = DateTime.MinValue;

    public HomeViewModel(
        GarminApiService    apiService,
        UserProfileService  userProfileService,
        WidgetService       widgetService,
        NotificationService notificationService,
        WorkerDataService   workerDataService,
        DatabaseService     databaseService,
        CalibrationService  calibrationService)
    {
        _apiService          = apiService;
        _userProfileService  = userProfileService;
        _widgetService       = widgetService;
        _notificationService = notificationService;
        _workerDataService   = workerDataService;
        _databaseService     = databaseService;
        _calibrationService  = calibrationService;
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
                await RunCalibrationIfDueAsync();

                Activity          = activity;
                Bmr               = Math.Round(_userProfileService.GetBmr(profile), 0);
                ActiveCalories    = activity.CalculatedCalories * _calibrationService.CorrectionFactor;
                Tdee              = GaintainingService.CalculateTdee(Bmr, ActiveCalories);
                CalorieAdjustment = profile.CalorieAdjustment;
                HasData           = true;

                double surplusFrac = Tdee > 0 ? (TargetKcal - Tdee) / Tdee * 100.0 : 0.0;
                _widgetService.UpdateData(TargetKcal, surplusFrac);

                if (_notificationService.EveningCheckEnabled)
                    await _notificationService.ScheduleEveningCheckAsync(TargetKcal, Bmr, ActiveCalories);

                _workerDataService.WriteBalanceData(
                    balanceKcal:    TargetKcal - Tdee,
                    tdee:           Tdee,
                    bmr:            Bmr,
                    activeCalories: ActiveCalories,
                    targetKcal:     TargetKcal);
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

    /// <summary>
    /// Every ~3 weeks, compares the calorie balance the app predicted (DailyBalance
    /// history) against the weight trend actually measured (BodyMeasurement history)
    /// and updates the correction factor applied to future activity-calorie estimates.
    /// </summary>
    private async Task RunCalibrationIfDueAsync()
    {
        if (!_calibrationService.IsDue) return;

        var balances = await _databaseService.GetLastNDaysAsync(CalibrationService.CalibrationIntervalDays);
        var windowStart = DateTime.Today.AddDays(-CalibrationService.CalibrationIntervalDays);
        var measurements = (await _databaseService.GetAllAsync())
            .Where(m => m.Date >= windowStart)
            .ToList();

        _calibrationService.RunCalibration(balances, measurements);
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

    /// <summary>Persists the current CalorieAdjustment slider value to the user profile.</summary>
    [RelayCommand]
    private void SaveDeficitCap()
    {
        var profile = _userProfileService.Load();
        profile.CalorieAdjustment = CalorieAdjustment;
        _userProfileService.Save(profile);
    }
}
