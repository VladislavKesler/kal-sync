using System.Diagnostics.CodeAnalysis;
using kal_sync.Models;
using Microsoft.Maui.Storage;

namespace kal_sync.Services;

/// <summary>
/// Persists user body-composition data in device Preferences and
/// provides the Katch-McArdle BMR formula.
/// </summary>
public class UserProfileService
{
    // ── Preferences keys ────────────────────────────────────────────────────
    private const string WeightKey = "profile.weight_kg";
    private const string BodyFatKey = "profile.body_fat_percent";
    private const string AgeKey = "profile.age";
    private const string SexKey = "profile.sex";
    private const string SurplusKey = "profile.surplus_percent";

    // ── Persistence ─────────────────────────────────────────────────────────

    // CA1822 suppressed: intentionally non-static so the service can be injected
    // as a dependency and replaced by a mock or alternative implementation.
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Instance method for DI / testability")]
    public UserProfile Load() => new()
    {
        WeightKg = Preferences.Get(WeightKey, 80.0),
        BodyFatPercent = Preferences.Get(BodyFatKey, 20.0),
        Age = Preferences.Get(AgeKey, 30),
        Sex = (Sex)Preferences.Get(SexKey, (int)Sex.Male),
        SurplusPercent = Preferences.Get(SurplusKey, 5.0),
    };

    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Instance method for DI / testability")]
    public void Save(UserProfile profile)
    {
        Preferences.Set(WeightKey, profile.WeightKg);
        Preferences.Set(BodyFatKey, profile.BodyFatPercent);
        Preferences.Set(AgeKey, profile.Age);
        Preferences.Set(SexKey, (int)profile.Sex);
        Preferences.Set(SurplusKey, profile.SurplusPercent);
    }

    // ── Katch-McArdle formula ────────────────────────────────────────────────

    /// <summary>LBM = weight × (1 - body-fat% / 100)</summary>
    public static double CalculateLeanBodyMass(double weightKg, double bodyFatPercent)
        => weightKg * (1.0 - bodyFatPercent / 100.0);

    /// <summary>BMR = 370 + (21.6 × LBM)</summary>
    public static double CalculateBmr(double leanBodyMassKg)
        => 370.0 + (21.6 * leanBodyMassKg);

    /// <summary>Convenience: load profile → LBM → BMR.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Instance method for DI / testability")]
    public double GetBmr(UserProfile profile)
    {
        double lbm = CalculateLeanBodyMass(profile.WeightKg, profile.BodyFatPercent);
        return CalculateBmr(lbm);
    }
}
