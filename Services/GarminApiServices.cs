using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using kal_sync.Models;

namespace kal_sync.Services;

/// <summary>
/// Service for communicating with the Garmin Calorie Calculator backend.
/// Implements <see cref="IDisposable"/> to properly release the underlying
/// <see cref="HttpClient"/>.
/// </summary>
public class GarminApiService : IDisposable
{
    // CA1869 — cached options instance reused across all deserialisation calls.
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private bool _disposed;

#if DEBUG
    #if ANDROID
    // Android Emulator routes 10.0.2.2 → host machine's localhost.
    private readonly string _apiBaseUrl = "http://10.0.2.2:8000";
    #else
    // Windows / iOS Simulator / macOS → real localhost.
    private readonly string _apiBaseUrl = "http://localhost:8000";
    #endif
#else
    private readonly string _apiBaseUrl = "https://api.example.com";
#endif

    /// <summary>Initialises the service and its <see cref="HttpClient"/>.</summary>
    public GarminApiService()
    {
        var handler = new HttpClientHandler();

#if DEBUG
        // Allow self-signed certificates in Debug mode.
        handler.ServerCertificateCustomValidationCallback =
            (message, cert, chain, errors) => true;
#endif

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    /// <summary>
    /// Fetch today's calorie data from the backend.
    /// Returns BMR-independent active calories from Garmin's daily tracking.
    /// </summary>
    public async Task<ActivityResponse?> GetLatestActivityAsync()
    {
        var url = $"{_apiBaseUrl}/api/activities/latest";

        Debug.WriteLine($"[GarminApiService] Calling {url}");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[GarminApiService] HTTP Error: {ex.Message}");
            throw new InvalidOperationException($"Network Error: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"API Error: {response.StatusCode} - {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();
        Debug.WriteLine($"[GarminApiService] Response: {content}");

        try
        {
            return JsonSerializer.Deserialize<ActivityResponse>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[GarminApiService] JSON Parse Error: {ex.Message}");
            throw new InvalidOperationException(
                $"Invalid response format: {ex.Message}", ex);
        }
    }

    /// <summary>Returns <c>true</c> when the backend health endpoint responds 200.</summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
