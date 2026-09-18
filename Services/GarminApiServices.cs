using System;
using System.Diagnostics;
using System.Globalization;
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
    private readonly UserProfileService _userProfileService;
    private bool _disposed;

    /// <summary>Initialises the service and its <see cref="HttpClient"/>.</summary>
    public GarminApiService(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
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
    /// Fetch the latest activity from the backend, with its calories estimated via
    /// the Keytel formula. The backend is stateless, so the body profile it needs
    /// (weight/age/sex) is sent along as query parameters.
    /// </summary>
    public async Task<ActivityResponse?> GetLatestActivityAsync()
    {
        var profile = _userProfileService.Load();
        var sexMale = profile.Sex == Sex.Male;
        var url = $"{_userProfileService.GetBackendUrl()}/api/activities/latest" +
                  $"?weight_kg={profile.WeightKg.ToString(CultureInfo.InvariantCulture)}" +
                  $"&age={profile.Age.ToString(CultureInfo.InvariantCulture)}" +
                  $"&sex_male={(sexMale ? "true" : "false")}";

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
            var response = await _httpClient.GetAsync($"{_userProfileService.GetBackendUrl()}/api/health");
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
