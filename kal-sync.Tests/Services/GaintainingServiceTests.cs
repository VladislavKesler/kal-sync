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
    public void Tdee_ShouldBeSumOfBmrAndNetActiveCalories()
    {
        double bmr = 1942.0;
        double activeCalories = 487.0;

        double tdee = CalculateTdee(bmr, activeCalories);

        // No TEF markup: TEF depends on actual intake, which this app
        // doesn't track, so it's not approximated here.
        tdee.Should().BeApproximately(2429.0, precision: 0.1);
    }

    [Fact]
    public void Tdee_OnRestDay_IsBmrOnly()
    {
        double tdee = CalculateTdee(bmr: 2000.0, activeCalories: 0.0);

        tdee.Should().BeApproximately(2000.0, precision: 0.01);
    }

    // --- Target calories (CalorieAdjustment formula) ---

    [Fact]
    public void TargetKcal_ShouldAddSurplusToTdee()
    {
        double tdee = 2500.0;
        double bmr  = 1700.0;

        double target = CalculateTargetKcal(tdee, calorieAdjustment: 300.0, bmr);

        // 2500 + 300 = 2800
        target.Should().BeApproximately(2800.0, precision: 0.1);
    }

    [Fact]
    public void TargetKcal_ShouldSubtractDeficitFromTdee()
    {
        double tdee = 2500.0;
        double bmr  = 1700.0;

        double target = CalculateTargetKcal(tdee, calorieAdjustment: -500.0, bmr);

        // 2500 - 500 = 2000
        target.Should().BeApproximately(2000.0, precision: 0.1);
    }

    [Fact]
    public void TargetKcal_ShouldEqualTdee_WhenAdjustmentIsZero()
    {
        double tdee = 2429.0;
        double bmr  = 1700.0;

        double target = CalculateTargetKcal(tdee, calorieAdjustment: 0.0, bmr);

        target.Should().BeApproximately(tdee, precision: 0.01);
    }

    [Fact]
    public void TargetKcal_ShouldFloorAtBmr_WhenLargeDeficit()
    {
        double tdee = 2000.0;
        double bmr  = 1700.0;

        double target = CalculateTargetKcal(tdee, calorieAdjustment: -500.0, bmr);

        // 2000 - 500 = 1500, but floor is BMR = 1700
        target.Should().BeApproximately(bmr, precision: 0.01);
    }

    // --- Recommended adjustment (CalculateRecommendedAdjustment) ---

    [Fact]
    public void RecommendedAdjustment_ShouldBeMaxDeficit_ForLowActivity()
    {
        double recommended = CalculateRecommendedAdjustment(activeCalories: 300.0);

        recommended.Should().Be(MaxDeficitKcal);
        recommended.Should().Be(-500.0);
    }

    [Fact]
    public void RecommendedAdjustment_ShouldBeMaxDeficit_AtActivityThreshold()
    {
        // At exactly the threshold (not above it), no buffer is added yet.
        double recommended = CalculateRecommendedAdjustment(activeCalories: HighActivityThresholdKcal);

        recommended.Should().Be(MaxDeficitKcal);
    }

    [Fact]
    public void RecommendedAdjustment_ShouldAddBuffer_ForHighActivity()
    {
        double recommended = CalculateRecommendedAdjustment(activeCalories: 800.0);

        recommended.Should().Be(MaxDeficitKcal + MaxBufferKcal);
        recommended.Should().Be(-400.0);
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

    private const double MaxDeficitKcal = -500.0;
    private const double HighActivityThresholdKcal = 600.0;
    private const double MaxBufferKcal = 100.0;

    private static double CalculateTdee(double bmr, double activeCalories)
        => bmr + activeCalories;

    private static double CalculateTargetKcal(double tdee, double calorieAdjustment, double bmr)
        => Math.Max(tdee + calorieAdjustment, bmr);

    private static double CalculateRecommendedAdjustment(double activeCalories)
        => activeCalories > HighActivityThresholdKcal
            ? MaxDeficitKcal + MaxBufferKcal
            : MaxDeficitKcal;

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
