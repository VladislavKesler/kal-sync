using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync.ViewModels;

/// <summary>
/// ViewModel for HomePage.
/// Handles activity data loading and state management.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly GarminApiService _apiService;
    private readonly UserProfileService _userProfileService;

    [ObservableProperty]
    private ActivityResponse? currentActivity;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasActivity;

    public HomeViewModel(GarminApiService apiService, UserProfileService userProfileService)
    {
        _apiService = apiService;
        _userProfileService = userProfileService;
        HasActivity = false;
        IsLoading = false;
        ErrorMessage = null;
    }

    /// <summary>Load latest activity from backend, using the saved user profile for the formula.</summary>
    [RelayCommand]
    public async Task LoadLatestActivity()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Debug.WriteLine("[HomeViewModel] Loading latest activity...");

            var profile = _userProfileService.Load();
            CurrentActivity = await _apiService.GetLatestActivityAsync(profile);
            HasActivity = CurrentActivity != null;
            ErrorMessage = null;

            Debug.WriteLine($"[HomeViewModel] Activity loaded: {CurrentActivity?.ActivityDate}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomeViewModel] Error: {ex.Message}");
            ErrorMessage = $"Fehler beim Laden: {ex.Message}";
            HasActivity = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshData() => await LoadLatestActivity();

    /// <summary>Auto-load on first appearance.</summary>
    [RelayCommand]
    public async Task PageAppearing()
    {
        if (!HasActivity)
            await LoadLatestActivity();
    }
}
