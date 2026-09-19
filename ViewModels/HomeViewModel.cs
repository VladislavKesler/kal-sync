using System;
using System.Collections.Generic;
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
/// Displays the calorie dashboard: BMR, active calories (all of today's
/// activities + NEAT), TDEE and calorie target, plus today's activity list.
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

    /// <summary>Net activity kcal + NEAT — everything burned on top of the BMR.</summary>
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

    // ── Today's activities (day list card) ───────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActivities))]
    [NotifyPropertyChangedFor(nameof(DaySummaryLabel))]
    private List<ActivityEstimate> _activities = [];

    /// <summary>Sum of all activities' net kcal, after the calibration factor.</summary>
    [ObservableProperty]
    private double _activityKcal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeatLabel))]
    private double _neatKcal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DaySummaryLabel))]
    [NotifyPropertyChangedFor(nameof(NeatLabel))]
    private int _steps;

    public bool HasActivities => Activities.Count > 0;

    /// <summary>Subtitle of the day card, e.g. "2 Aktivitäten · 6.528 Schritte".</summary>
    public string DaySummaryLabel
    {
        get
        {
            string activities = Activities.Count switch
            {
                0 => "Keine Aktivität",
                1 => "1 Aktivität",
                var n => $"{n} Aktivitäten",
            };
            return $"{activities} · {Steps:N0} Schritte";
        }
    }

    /// <summary>NEAT row text, e.g. "5.812 Schritte außerhalb von Aktivitäten".</summary>
    public string NeatLabel => $"{NeatKcal:F0} kcal aus Alltagsbewegung";

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

    /// <summary>Load calorie dashboard from user profile + all of today's Garmin activities.</summary>
    [RelayCommand]
    public async Task LoadLatestActivity()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Debug.WriteLine("[HomeViewModel] Loading calorie dashboard...");

            var profile = _userProfileService.Load();
            var day     = await _apiService.GetDaySummaryAsync(DateTime.Today);

            if (day != null)
            {
                await RunCalibrationIfDueAsync();

                Activities   = day.Activities;
                Steps        = day.Steps;
                NeatKcal     = Math.Round(day.NeatKcal, 0);
                ActivityKcal = Math.Round(day.ActivityKcal * _calibrationService.CorrectionFactor, 0);

                Bmr               = Math.Round(_userProfileService.GetBmr(profile), 0);
                ActiveCalories    = ActivityKcal + NeatKcal;
                Tdee              = Math.Round(GaintainingService.CalculateTdee(Bmr, ActiveCalories), 0);
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
                $"[HomeViewModel] BMR={Bmr}, Activity={ActivityKcal}, NEAT={NeatKcal}, " +
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
