using System.Diagnostics.CodeAnalysis;
using kal_sync.Models;
using Microsoft.Maui.Storage;

namespace kal_sync.Services;

/// <summary>
/// Simple linear self-calibration: every few weeks, compares the calorie balance
/// the app predicted (sum of DailyBalance.BalanceKcal) against the body-weight
/// trend actually measured (BodyMeasurement history) and derives a correction
/// factor applied to future Keytel-based activity-calorie estimates. No ML —
/// just "did reality match the prediction, and by how much".
/// </summary>
public class CalibrationService
{
    /// <summary>Minimum days between two calibration runs.</summary>
    public const int CalibrationIntervalDays = 21;

    /// <summary>Minimum number of DailyBalance entries required for a calibration run.</summary>
    public const int MinBalanceDaysForCalibration = 14;

    /// <summary>
    /// Below this absolute predicted balance (kcal), the signal is too small
    /// relative to day-to-day noise (water weight, measurement error) to derive
    /// a trustworthy correction — the factor is left unchanged.
    /// </summary>
    public const double MinReliablePredictedBalanceKcal = 700.0;

    /// <summary>Widely used approximation: 1 kg of body-weight change ≈ 7700 kcal.</summary>
    public const double KcalPerKg = 7700.0;

    public const double MinCorrectionFactor = 0.85;
    public const double MaxCorrectionFactor = 1.15;

    private const string CorrectionFactorKey = "calibration.correction_factor";
    private const string LastCalibrationDateKey = "calibration.last_date_ticks";

    /// <summary>Current correction factor applied to Keytel-estimated activity calories. Default 1.0 = no correction yet.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Instance property for DI / testability")]
    public double CorrectionFactor
    {
        get => Preferences.Get(CorrectionFactorKey, 1.0);
        private set => Preferences.Set(CorrectionFactorKey, value);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Instance property for DI / testability")]
    public DateTime? LastCalibrationDate
    {
        get
        {
            long ticks = Preferences.Get(LastCalibrationDateKey, 0L);
            return ticks == 0L ? null : new DateTime(ticks);
        }
        private set => Preferences.Set(LastCalibrationDateKey, value?.Ticks ?? 0L);
    }

    /// <summary>True when no calibration has run yet, or the last one is older than <see cref="CalibrationIntervalDays"/>.</summary>
    public bool IsDue
        => LastCalibrationDate is null
           || (DateTime.Today - LastCalibrationDate.Value).TotalDays >= CalibrationIntervalDays;

    /// <summary>
    /// Pure calculation: given the app's predicted calorie balance (kcal, summed
    /// over the calibration window) and the weight change actually measured over
    /// the same window (kg), returns a correction factor clamped to
    /// [<see cref="MinCorrectionFactor"/>, <see cref="MaxCorrectionFactor"/>].
    /// Returns 1.0 unchanged when the predicted balance is too small to trust.
    /// </summary>
    public static double CalculateCorrectionFactor(double predictedBalanceKcal, double actualWeightChangeKg)
    {
        if (Math.Abs(predictedBalanceKcal) < MinReliablePredictedBalanceKcal)
            return 1.0;

        double actualBalanceKcal = actualWeightChangeKg * KcalPerKg;
        double rawFactor = actualBalanceKcal / predictedBalanceKcal;
        return Math.Clamp(rawFactor, MinCorrectionFactor, MaxCorrectionFactor);
    }

    /// <summary>
    /// Runs a calibration pass over the caller-supplied window of daily balances
    /// and body measurements. Requires at least <see cref="MinBalanceDaysForCalibration"/>
    /// balance entries and 2 measurements; otherwise leaves the stored factor
    /// untouched and returns null. On success, persists and returns the new factor.
    /// </summary>
    public double? RunCalibration(
        IReadOnlyList<DailyBalance> balances, IReadOnlyList<BodyMeasurement> measurements)
    {
        if (balances.Count < MinBalanceDaysForCalibration || measurements.Count < 2)
            return null;

        var ordered = measurements.OrderBy(m => m.Date).ToList();
        double predictedBalanceKcal = balances.Sum(b => b.BalanceKcal);
        double actualWeightChangeKg = ordered[^1].WeightKg - ordered[0].WeightKg;

        double factor = CalculateCorrectionFactor(predictedBalanceKcal, actualWeightChangeKg);
        CorrectionFactor = factor;
        LastCalibrationDate = DateTime.Today;
        return factor;
    }
}
