using System;
using System.Diagnostics;
using System.Globalization;
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
    /// Fetch latest activity from backend.
    /// When a <paramref name="profile"/> is provided the backend uses the
    /// Keytel (2005) formula instead of the default HRR formula.
    /// </summary>
    public async Task<ActivityResponse?> GetLatestActivityAsync(UserProfile? profile = null)
    {
        try
        {
            var url = $"{_apiBaseUrl}/api/activities/latest";

            if (profile != null)
            {
                var w = profile.WeightKg.ToString("F1", CultureInfo.InvariantCulture);
                var sex = (int)profile.Sex;
                url += $"?weight_kg={w}&age={profile.Age}&sex={sex}";
            }

            Debug.WriteLine($"[GarminApiService] Calling {url}");

            var response = await _httpClient.GetAsync(url);

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
