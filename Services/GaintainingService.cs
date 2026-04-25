using kal_sync.Models;

namespace kal_sync.Services;

/// <summary>
/// Business logic for the "Gaintaining" feature:
/// TDEE, target calories, monthly gain rate and traffic-light assessment.
/// </summary>
public class GaintainingService
{
    /// <summary>TDEE = BMR + active calories burned today.</summary>
    public static double CalculateTdee(double bmr, double activeCalories)
        => bmr + activeCalories;

    /// <summary>Target = TDEE × (1 + surplusPercent / 100).</summary>
    public static double CalculateTargetKcal(double tdee, double surplusPercent)
        => tdee * (1.0 + surplusPercent / 100.0);

    /// <summary>
    /// Monthly gain rate = (currentAvg - previousAvg) / bodyWeight × 100.
    /// Returns a percentage; negative means weight loss.
    /// </summary>
    public static double CalculateMonthlyGainRate(
        double currentAvg, double previousAvg, double bodyWeight)
        => (currentAvg - previousAvg) / bodyWeight * 100.0;

    /// <summary>
    /// Green  = &lt;0.5% / month (lean bulk on track)
    /// Yellow = 0.5–1.0% / month
    /// Red    = ≥1.0% / month  (too fast)
    /// </summary>
    public static TrafficLight EvaluateTrafficLight(double monthlyGainPercent)
    {
        if (monthlyGainPercent < 0.5) return TrafficLight.Green;
        if (monthlyGainPercent < 1.0) return TrafficLight.Yellow;
        return TrafficLight.Red;
    }
}
