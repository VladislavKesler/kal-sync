using FluentAssertions;
using Xunit;

namespace kal_sync.Tests.Services;

/// <summary>
/// Tests for the Katch-McArdle BMR formula.
/// Formula: BMR = 370 + (21.6 × LeanBodyMass)
/// LeanBodyMass = BodyWeightKg × (1 - BodyFatPercent / 100)
/// </summary>
public class UserProfileServiceTests
{
    // --- LeanBodyMass calculation ---

    [Fact]
    public void LeanBodyMass_ShouldBeCorrect_ForTypicalMale()
    {
        // Arrange
        double bodyWeightKg = 91.0;
        double bodyFatPercent = 20.0;

        // Act
        double lbm = CalculateLeanBodyMass(bodyWeightKg, bodyFatPercent);

        // Assert
        lbm.Should().BeApproximately(72.8, precision: 0.01);
    }

    [Fact]
    public void LeanBodyMass_ShouldBeCorrect_ForZeroBodyFat()
    {
        // Arrange — edge case: 0% body fat means LBM equals total weight
        double bodyWeightKg = 80.0;
        double bodyFatPercent = 0.0;

        // Act
        double lbm = CalculateLeanBodyMass(bodyWeightKg, bodyFatPercent);

        // Assert
        lbm.Should().BeApproximately(80.0, precision: 0.01);
    }

    [Theory]
    [InlineData(70.0, 15.0, 59.5)]
    [InlineData(91.0, 20.0, 72.8)]
    [InlineData(60.0, 25.0, 45.0)]
    public void LeanBodyMass_ShouldBeCorrect_ForVariousInputs(
        double weightKg, double bodyFatPercent, double expectedLbm)
    {
        double lbm = CalculateLeanBodyMass(weightKg, bodyFatPercent);
        lbm.Should().BeApproximately(expectedLbm, precision: 0.01);
    }

    // --- BMR (Katch-McArdle) calculation ---

    [Fact]
    public void Bmr_ShouldMatchExpectedValue_ForPlanExample()
    {
        // From Plan: BMR = 370 + (21.6 × (91 × 0.80)) → ~1942
        double bodyWeightKg = 91.0;
        double bodyFatPercent = 20.0;

        double lbm = CalculateLeanBodyMass(bodyWeightKg, bodyFatPercent);
        double bmr = CalculateBmr(lbm);

        bmr.Should().BeApproximately(1942.0, precision: 1.0);
    }

    [Fact]
    public void Bmr_ShouldNeverBeBelowBaseline()
    {
        // BMR formula always produces at least 370 (the constant)
        double lbm = 0.0;
        double bmr = CalculateBmr(lbm);
        bmr.Should().BeGreaterThanOrEqualTo(370.0);
    }

    [Theory]
    [InlineData(50.0, 1450.0)]   // 370 + 21.6 * 50 = 1450
    [InlineData(72.8, 1942.48)]  // 370 + 21.6 * 72.8
    [InlineData(60.0, 1666.0)]   // 370 + 21.6 * 60
    public void Bmr_ShouldMatchFormula_ForKnownValues(double lbm, double expectedBmr)
    {
        double bmr = CalculateBmr(lbm);
        bmr.Should().BeApproximately(expectedBmr, precision: 1.0);
    }

    // --- Helper methods (mirrors what UserProfileService will implement) ---
    // These will be replaced by actual service calls once kal-sync.Core exists.

    private static double CalculateLeanBodyMass(double bodyWeightKg, double bodyFatPercent)
        => bodyWeightKg * (1.0 - bodyFatPercent / 100.0);

    private static double CalculateBmr(double leanBodyMassKg)
        => 370.0 + (21.6 * leanBodyMassKg);
}
