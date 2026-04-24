using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kal_sync.Models;
using kal_sync.Services;

namespace kal_sync.ViewModels;

/// <summary>
/// ViewModel for HomePage
/// Handles activity data loading and state management
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly GarminApiService _apiService;

    // Observable Properties (auto-notify binding)
    [ObservableProperty]
    private ActivityResponse? currentActivity;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasActivity;

    public HomeViewModel()
    {
        _apiService = new GarminApiService();
        HasActivity = false;
        IsLoading = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Load latest activity from API
    /// </summary>
    [RelayCommand]
    public async Task LoadLatestActivity()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Debug.WriteLine("[HomeViewModel] Loading latest activity...");

            CurrentActivity = await _apiService.GetLatestActivityAsync();
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

    /// <summary>
    /// Refresh activity data (same as LoadLatestActivity)
    /// </summary>
    [RelayCommand]
    public async Task RefreshData()
    {
        await LoadLatestActivity();
    }

    /// <summary>
    /// Called when page appears
    /// Auto-load if no activity yet
    /// </summary>
    [RelayCommand]
    public async Task PageAppearing()
    {
        if (!HasActivity)
        {
            await LoadLatestActivity();
        }
    }
}