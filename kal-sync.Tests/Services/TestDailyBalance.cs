namespace kal_sync.Tests.Services;

/// <summary>Minimal mirror of DailyBalance for tests (no SQLite dependency).</summary>
internal sealed class TestDailyBalance
{
    public DateTime Date { get; set; }

    public double BalanceKcal { get; set; }

    public double Tdee { get; set; }

    public double Bmr { get; set; }

    public double ActiveCalories { get; set; }

    public double TargetKcal { get; set; }
}
