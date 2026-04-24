using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using kal_sync.Models;

namespace kal_sync.Services;

/// <summary>
/// Service for communicating with Garmin Calorie Calculator Backend
/// Handles API calls and response parsing
/// </summary>
public class GarminApiService
{
    private readonly HttpClient _httpClient;

    // API Base URL - Change for production
#if DEBUG
    // Android Emulator: 10.0.2.2 = localhost
    // iOS Simulator: localhost
    // Physical Device: Your Computer IP (e.g., 192.168.x.x)
    private readonly string _apiBaseUrl = "http://10.0.2.2:8000";
#else
    private readonly string _apiBaseUrl = "https://api.example.com"; // TODO: Update for production
#endif

    public GarminApiService()
    {
        var handler = new HttpClientHandler();

#if DEBUG
        // Allow self-signed certificates in Debug mode
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Fetch latest activity from backend
    /// </summary>
    /// <returns>ActivityResponse with calculated calories</returns>
    public async Task<ActivityResponse?> GetLatestActivityAsync()
    {
        try
        {
            Debug.WriteLine($"[GarminApiService] Calling {_apiBaseUrl}/api/activities/latest");

            var response = await _httpClient.GetAsync(
                $"{_apiBaseUrl}/api/activities/latest"
            );

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[GarminApiService] Response: {content}");

                var activity = JsonSerializer.Deserialize<ActivityResponse>(content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return activity;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"API Error: {response.StatusCode} - {errorContent}"
                );
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[GarminApiService] HTTP Error: {ex.Message}");
            throw new Exception($"Network Error: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[GarminApiService] JSON Parse Error: {ex.Message}");
            throw new Exception($"Invalid response format: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GarminApiService] Unexpected Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_apiBaseUrl}/api/health"
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
