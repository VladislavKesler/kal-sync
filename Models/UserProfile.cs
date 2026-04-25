namespace kal_sync.Models;

public enum Sex { Male, Female }

/// <summary>
/// Persisted user data needed for BMR (Katch-McArdle) and calorie formula (Keytel 2005).
/// </summary>
public class UserProfile
{
    /// <summary>Body weight in kilograms.</summary>
    public double WeightKg { get; set; } = 80.0;

    /// <summary>Body-fat percentage (0–100).</summary>
    public double BodyFatPercent { get; set; } = 20.0;

    /// <summary>Age in years.</summary>
    public int Age { get; set; } = 30;

    public Sex Sex { get; set; } = Sex.Male;

    /// <summary>Caloric surplus on top of TDEE, in percent (e.g. 5 = +5%).</summary>
    public double SurplusPercent { get; set; } = 5.0;
}
