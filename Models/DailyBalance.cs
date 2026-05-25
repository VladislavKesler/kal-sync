using SQLite;

namespace kal_sync.Models;

public class DailyBalance
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Planned deficit/surplus in kcal. Negative = deficit (goal), positive = surplus. TargetKcal − TDEE.</summary>
    public double BalanceKcal { get; set; }

    public double Tdee { get; set; }
    public double Bmr { get; set; }
    public double ActiveCalories { get; set; }
    public double TargetKcal { get; set; }
}
