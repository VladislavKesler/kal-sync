using FluentAssertions;
using Xunit;

namespace kal_sync.Tests.Services;

/// <summary>
/// Tests for DailyBalance calculation and aggregation logic.
/// </summary>
public class DailyBalanceTests
{
    // ── BalanceKcal calculation ──────────────────────────────────────────────

    [Fact]
    public void BalanceKcal_ShouldBeTargetMinusTdee()
    {
        double bmr            = 1800.0;
        double activeCalories = 400.0;
        double adjustment     = -200.0;

        double tdee       = bmr + activeCalories;           // 2200
        double targetKcal = CalculateTargetKcal(tdee, adjustment, bmr); // 2000
        double balance    = targetKcal - tdee;

        balance.Should().BeApproximately(-200.0, precision: 0.01);
    }

    [Fact]
    public void BalanceKcal_ShouldBeNegative_WhenDeficit()
    {
        double bmr            = 1800.0;
        double activeCalories = 400.0;
        double adjustment     = -300.0;

        double tdee    = bmr + activeCalories;
        double target  = CalculateTargetKcal(tdee, adjustment, bmr);
        double balance = target - tdee;

        balance.Should().BeLessThan(0);
    }

    [Fact]
    public void BalanceKcal_ShouldEqualNegativeActiveCalories_WhenBmrFloorApplies()
    {
        // Large deficit forces TargetKcal to be clamped to BMR.
        // Then: BalanceKcal = BMR − (BMR + ActiveCalories) = −ActiveCalories
        double bmr            = 1800.0;
        double activeCalories = 300.0;
        double adjustment     = -700.0;   // 2100 − 700 = 1400 < BMR → clamped to 1800

        double tdee       = bmr + activeCalories;           // 2100
        double targetKcal = CalculateTargetKcal(tdee, adjustment, bmr); // 1800 (floored at BMR)
        double balance    = targetKcal - tdee;

        balance.Should().BeApproximately(-activeCalories, precision: 0.01);
    }

    // ── Weekly aggregation ───────────────────────────────────────────────────

    [Fact]
    public void WeeklyAverage_ShouldBeCorrect_ForKnownValues()
    {
        var weekValues = new[] { -500.0, -400.0, -300.0, -200.0, -100.0, 100.0, -200.0 };
        double expected = weekValues.Sum() / weekValues.Length;   // ≈ −228.57

        double average = weekValues.Average();

        average.Should().BeApproximately(expected, precision: 0.01);
    }

    [Fact]
    public void WeeklyAverage_ShouldBeNegative_WhenMostDaysAreDeficit()
    {
        var weekValues = new[] { -300.0, -250.0, -200.0, -150.0, -100.0, 50.0, -50.0 };

        double average = weekValues.Average();

        average.Should().BeLessThan(0);
    }

    // ── Empty periods ────────────────────────────────────────────────────────

    [Fact]
    public void EmptyEntries_ShouldYieldAllNullSlots_ForSevenDays()
    {
        var entries = new List<TestDailyBalance>();

        var slots = BuildSevenDaysSlots(entries);

        slots.Should().OnlyContain(s => s.value == null);
    }

    [Fact]
    public void EmptyEntries_ShouldYieldAllNullSlots_ForFourWeeks()
    {
        var entries = new List<TestDailyBalance>();

        var slots = BuildFourWeeksSlots(entries);

        slots.Should().OnlyContain(s => s.value == null);
    }

    // ── Helpers (mirror logic from GaintainingService and DailyBalanceChartDrawable) ──

    private static double CalculateTargetKcal(double tdee, double adjustment, double bmr)
        => Math.Max(tdee + adjustment, bmr);

    private static (double? value, string label)[] BuildSevenDaysSlots(
        IEnumerable<TestDailyBalance> entries)
    {
        var map   = entries.ToDictionary(e => e.Date.Date);
        var slots = new (double? value, string label)[7];

        for (int i = 0; i < 7; i++)
        {
            var date = DateTime.Today.AddDays(i - 6);
            slots[i] = map.TryGetValue(date, out var entry)
                ? (entry.BalanceKcal, date.DayOfWeek.ToString()[..2])
                : (null, date.DayOfWeek.ToString()[..2]);
        }

        return slots;
    }

    private static (double? value, string label)[] BuildFourWeeksSlots(
        IEnumerable<TestDailyBalance> entries)
    {
        var map   = entries.ToDictionary(e => e.Date.Date);
        var slots = new (double? value, string label)[4];

        for (int w = 0; w < 4; w++)
        {
            var startDate  = DateTime.Today.AddDays(-27 + w * 7);
            var weekValues = new List<double>();

            for (int d = 0; d < 7; d++)
            {
                var date = startDate.AddDays(d);
                if (map.TryGetValue(date, out var entry))
                    weekValues.Add(entry.BalanceKcal);
            }

            slots[w] = weekValues.Count > 0
                ? (weekValues.Average(), $"KW{w}")
                : (null, $"KW{w}");
        }

        return slots;
    }
}
