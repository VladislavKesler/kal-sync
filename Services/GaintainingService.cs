using kal_sync.Models;

namespace kal_sync.Services;

/// <summary>
/// Business logic for the "Gaintaining" feature:
/// TDEE, target calories, monthly gain rate and traffic-light assessment.
/// </summary>
public class GaintainingService
{
    /// <summary>Strictest deficit the app will recommend automatically.</summary>
    public const double MaxDeficitKcal = -500.0;

    /// <summary>
    /// Above this daily activity-calorie estimate, a small buffer is added on top
    /// of <see cref="MaxDeficitKcal"/> so a heavy training day isn't compounded by
    /// the same fixed deficit as a rest day.
    /// </summary>
    public const double HighActivityThresholdKcal = 600.0;

    /// <summary>Buffer added to the deficit recommendation on high-activity days.</summary>
    public const double MaxBufferKcal = 100.0;

    /// <summary>TDEE = BMR + active calories burned today.</summary>
    public static double CalculateTdee(double bmr, double activeCalories)
        => bmr + activeCalories;

    /// <summary>
    /// Target = TDEE + calorieAdjustment, never falls below BMR.
    /// calorieAdjustment: positive = surplus, negative = deficit.
    /// </summary>
    public static double CalculateTargetKcal(double tdee, double calorieAdjustment, double bmr)
        => Math.Max(tdee + calorieAdjustment, bmr);

    /// <summary>
    /// Suggests a CalorieAdjustment: a strict -500 kcal deficit, or -500 + 100 kcal
    /// buffer when today's activity-calorie estimate exceeds
    /// <see cref="HighActivityThresholdKcal"/>. Purely a suggestion shown next to
    /// the manual slider — it is never applied automatically.
    /// </summary>
    public static double CalculateRecommendedAdjustment(double activeCalories)
        => activeCalories > HighActivityThresholdKcal
            ? MaxDeficitKcal + MaxBufferKcal
            : MaxDeficitKcal;

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
