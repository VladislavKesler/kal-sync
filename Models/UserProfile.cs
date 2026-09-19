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

    /// <summary>Calorie adjustment added to TDEE (positive = surplus, negative = deficit). Floored at BMR.</summary>
    public double CalorieAdjustment { get; set; } = 0.0;

    /// <summary>Sport recorded under the watch's "Cardio" profile (Garmin typeKey indoor_cardio).</summary>
    public CardioSport CardioSport { get; set; } = CardioSport.TennisSingles;
}
