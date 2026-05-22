using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync.ViewModels;

public partial class BodyMeasurementViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    [ObservableProperty] private ObservableCollection<BodyMeasurement> _measurements = [];
    [ObservableProperty] private double _newWeightKg = 80.0;
    [ObservableProperty] private double _newBodyFatPercent = 20.0;
    [ObservableProperty] private bool _isFormVisible;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;

    public BodyMeasurementViewModel(DatabaseService db, UserProfileService profileService)
    {
        _db = db;
        var profile = profileService.Load();
        NewWeightKg = profile.WeightKg;
        NewBodyFatPercent = profile.BodyFatPercent;
    }

    [RelayCommand]
    private async Task PageAppearingAsync() => await LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _db.GetAllAsync();
        Measurements = new ObservableCollection<BodyMeasurement>(items);
        HasData = Measurements.Count > 0;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        await _db.AddAsync(new BodyMeasurement
        {
            Date = DateTime.Today,
            WeightKg = NewWeightKg,
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
    }

    [RelayCommand]
    private void ToggleForm() => IsFormVisible = !IsFormVisible;
}
