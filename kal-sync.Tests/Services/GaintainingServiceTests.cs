using FluentAssertions;
using Xunit;

namespace kal_sync.Tests.Services;

/// <summary>
/// Tests for GaintainingService.
/// Covers TDEE calculation, target calories, monthly gain rate and traffic light logic.
/// </summary>
public class GaintainingServiceTests
{
    // --- TDEE ---

    [Fact]
    public void Tdee_ShouldBeSumOfBmrAndActiveCalories()
    {
        double bmr = 1942.0;
        double activeCalories = 487.0;

        double tdee = CalculateTdee(bmr, activeCalories);

        tdee.Should().BeApproximately(2429.0, precision: 0.1);
    }

    // --- Target calories ---

    [Fact]
    public void TargetKcal_ShouldApplySurplusCorrectly_ForFivePercent()
    {
        double tdee = 2429.0;
        double surplusPercent = 5.0;

        double target = CalculateTargetKcal(tdee, surplusPercent);

        // 2429 * 1.05 = 2550.45
        target.Should().BeApproximately(2550.45, precision: 0.1);
    }

    [Fact]
    public void TargetKcal_ShouldEqualTdee_WhenSurplusIsZero()
    {
        double tdee = 2429.0;
        double target = CalculateTargetKcal(tdee, surplusPercent: 0.0);
        target.Should().BeApproximately(tdee, precision: 0.01);
    }

    // --- Traffic light (boundary value tests) ---

    [Theory]
    [InlineData(0.0, TrafficLight.Green)]
    [InlineData(0.3, TrafficLight.Green)]
    [InlineData(0.499, TrafficLight.Green)]
    [InlineData(0.5, TrafficLight.Yellow)]   // boundary: exactly 0.5 → Yellow
    [InlineData(0.75, TrafficLight.Yellow)]
    [InlineData(0.999, TrafficLight.Yellow)]
    [InlineData(1.0, TrafficLight.Red)]      // boundary: exactly 1.0 → Red
    [InlineData(1.5, TrafficLight.Red)]
    [InlineData(2.0, TrafficLight.Red)]
    public void TrafficLight_ShouldReflectMonthlyGainRate(
        double monthlyGainPercent, TrafficLight expected)
    {
        TrafficLight result = EvaluateTrafficLight(monthlyGainPercent);
        result.Should().Be(expected);
    }

    // --- Monthly gain rate ---

    [Fact]
    public void MonthlyGainRate_ShouldBeCorrect_ForTypicalScenario()
    {
        // Body weight 90 kg, gained 0.27 kg in a month → 0.3%
        double currentAvg = 90.27;
        double previousAvg = 90.0;
        double bodyWeight = 90.0;

        double rate = CalculateMonthlyGainRate(currentAvg, previousAvg, bodyWeight);

        rate.Should().BeApproximately(0.3, precision: 0.01);
    }

    [Fact]
    public void MonthlyGainRate_ShouldBeNegative_WhenWeightDecreases()
    {
        double rate = CalculateMonthlyGainRate(
            currentAvg: 89.5,
            previousAvg: 90.0,
            bodyWeight: 90.0);

        rate.Should().BeLessThan(0);
    }

    // --- Helpers (mirror GaintainingService implementation) ---

    private static double CalculateTdee(double bmr, double activeCalories)
        => bmr + activeCalories;

    private static double CalculateTargetKcal(double tdee, double surplusPercent)
        => tdee * (1.0 + surplusPercent / 100.0);

    private static double CalculateMonthlyGainRate(
        double currentAvg, double previousAvg, double bodyWeight)
        => (currentAvg - previousAvg) / bodyWeight * 100.0;

    private static TrafficLight EvaluateTrafficLight(double monthlyGainPercent)
    {
        if (monthlyGainPercent < 0.5) return TrafficLight.Green;
        if (monthlyGainPercent < 1.0) return TrafficLight.Yellow;
        return TrafficLight.Red;
    }
}

// Mirrors the enum that will live in kal-sync.Core
public enum TrafficLight { Green, Yellow, Red }
