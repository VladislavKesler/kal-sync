using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kal_sync.Converters;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync.ViewModels;

public partial class BodyMeasurementViewModel : ObservableObject
{
    private readonly DatabaseService     _db;
    private readonly NotificationService _notificationService;

    [ObservableProperty] private ObservableCollection<BodyMeasurement> _measurements = [];
    [ObservableProperty] private double _newWeightKg = 80.0;
    [ObservableProperty] private double _newBodyFatPercent = 20.0;
    [ObservableProperty] private bool   _isFormVisible;
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private bool   _hasData;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTab0Active))]
    [NotifyPropertyChangedFor(nameof(IsTab1Active))]
    [NotifyPropertyChangedFor(nameof(IsTab2Active))]
    private int _selectedTabIndex;

    public bool IsTab0Active => SelectedTabIndex == 0;
    public bool IsTab1Active => SelectedTabIndex == 1;
    public bool IsTab2Active => SelectedTabIndex == 2;

    /// <summary>Set by LoadAsync when the measurement reminder interval has elapsed.
    /// Code-behind watches this and shows a DisplayAlert, then resets it to null.</summary>
    [ObservableProperty] private string? _reminderAlert;

    /// <summary>Drawable for the weight/KFA chart; updated after each load.</summary>
    public MeasurementChartDrawable ChartDrawable { get; } = new();

    /// <summary>Drawable for the daily deficit/surplus bar chart; updated after each LoadBalanceChartAsync.</summary>
    public DailyBalanceChartDrawable BalanceChartDrawable { get; } = new();

    public BodyMeasurementViewModel(
        DatabaseService     db,
        UserProfileService  profileService,
        NotificationService notificationService)
    {
        _db                  = db;
        _notificationService = notificationService;
        var profile          = profileService.Load();
        NewWeightKg          = profile.WeightKg;
        NewBodyFatPercent    = profile.BodyFatPercent;
    }

    [RelayCommand]
    private async Task PageAppearingAsync() => await LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _db.GetAllAsync();
        Measurements = new ObservableCollection<BodyMeasurement>(items);
        HasData      = Measurements.Count > 0;

        ChartDrawable.Measurements = [.. items.OrderBy(m => m.Date)];

        var latest = items.OrderByDescending(m => m.Date).FirstOrDefault();
        if (_notificationService.IsDue(latest?.Date))
            ReminderAlert = "Zeit für deine nächste KFA-Messung!\nStelle dich auf die Waage und trage Gewicht & Körperfett ein.";

        await LoadBalanceChartAsync();

        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        await _db.AddAsync(new BodyMeasurement
        {
            Date           = DateTime.Today,
            WeightKg       = NewWeightKg,
            BodyFatPercent = NewBodyFatPercent,
        });
        IsFormVisible = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(BodyMeasurement measurement)
    {
        await _db.DeleteAsync(measurement);
        Measurements.Remove(measurement);
        HasData = Measurements.Count > 0;
        ChartDrawable.Measurements = [.. Measurements.OrderBy(m => m.Date)];
    }

    [RelayCommand]
    private void ToggleForm() => IsFormVisible = !IsFormVisible;

    [RelayCommand]
    private async Task SelectTab(int index)
    {
        SelectedTabIndex = index;
        await LoadBalanceChartAsync();
    }

    private async Task LoadBalanceChartAsync()
    {
        var entries = SelectedTabIndex switch
        {
            0 => await _db.GetLastNDaysAsync(7),
            1 => await _db.GetLastNDaysAsync(28),
            2 => await _db.GetLastNDaysAsync(120),
            _ => [],
        };

        BalanceChartDrawable.Entries = entries;
        BalanceChartDrawable.Mode    = (ChartMode)SelectedTabIndex;
        OnPropertyChanged(nameof(BalanceChartDrawable));
    }
}
