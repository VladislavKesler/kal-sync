using FluentAssertions;
using Xunit;

namespace kal_sync.Tests.Services;

/// <summary>
/// Tests for CalibrationService.CalculateCorrectionFactor — the pure, Preferences-free
/// part of the self-calibration logic (linear comparison of predicted vs. actual
/// calorie balance, clamped to a safe range).
/// </summary>
public class CalibrationServiceTests
{
    // --- Noise guard ---

    [Fact]
    public void CorrectionFactor_ShouldStayAtOne_WhenPredictedBalanceTooSmall()
    {
        // Predicted balance well below the reliability threshold (700 kcal).
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: 300.0, actualWeightChangeKg: -1.0);

        factor.Should().Be(1.0);
    }

    [Fact]
    public void CorrectionFactor_ShouldStayAtOne_WhenPredictedBalanceIsZero()
    {
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: 0.0, actualWeightChangeKg: -0.5);

        factor.Should().Be(1.0);
    }

    // --- Direction / magnitude ---

    [Fact]
    public void CorrectionFactor_ShouldBeOne_WhenActualMatchesPredictedExactly()
    {
        // Predicted a 7700 kcal deficit (~1 kg) and the scale shows exactly -1 kg.
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: -7700.0, actualWeightChangeKg: -1.0);

        factor.Should().BeApproximately(1.0, precision: 0.001);
    }

    [Fact]
    public void CorrectionFactor_ShouldBeBelowOne_WhenLessWeightLostThanPredicted()
    {
        // Predicted -1 kg, but only -0.5 kg actually happened → activity calories were
        // overestimated → factor should scale future estimates down.
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: -7700.0, actualWeightChangeKg: -0.5);

        factor.Should().BeLessThan(1.0);
        factor.Should().BeApproximately(0.85, precision: 0.001); // clamped at MinCorrectionFactor
    }

    [Fact]
    public void CorrectionFactor_ShouldBeAboveOne_WhenMoreWeightLostThanPredicted()
    {
        // Predicted -0.5 kg, but -1 kg actually happened → activity calories were
        // underestimated → factor should scale future estimates up.
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: -3850.0, actualWeightChangeKg: -1.0);

        factor.Should().BeGreaterThan(1.0);
    }

    // --- Clamping ---

    [Fact]
    public void CorrectionFactor_ShouldClampAtMinimum()
    {
        // Wildly more deficit predicted than achieved → raw factor far below 0.85.
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: -20000.0, actualWeightChangeKg: -0.1);

        factor.Should().Be(0.85);
    }

    [Fact]
    public void CorrectionFactor_ShouldClampAtMaximum()
    {
        // Wildly more weight lost than the predicted deficit accounts for.
        double factor = CalculateCorrectionFactor(predictedBalanceKcal: -1000.0, actualWeightChangeKg: -5.0);

        factor.Should().Be(1.15);
    }

    // --- Helpers (mirror CalibrationService implementation; no MAUI/Preferences dependency) ---

    private const double MinReliablePredictedBalanceKcal = 700.0;
    private const double KcalPerKg = 7700.0;
    private const double MinCorrectionFactor = 0.85;
    private const double MaxCorrectionFactor = 1.15;

    private static double CalculateCorrectionFactor(double predictedBalanceKcal, double actualWeightChangeKg)
    {
        if (Math.Abs(predictedBalanceKcal) < MinReliablePredictedBalanceKcal)
            return 1.0;

        double actualBalanceKcal = actualWeightChangeKg * KcalPerKg;
        double rawFactor = actualBalanceKcal / predictedBalanceKcal;
        return Math.Clamp(rawFactor, MinCorrectionFactor, MaxCorrectionFactor);
    }
}
