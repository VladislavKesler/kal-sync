using SQLite;

namespace kal_sync.Models;

public class RunSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int DurationMinutes { get; set; }
    public double DistanceKm { get; set; }
    public int CaloriesBurned { get; set; }
    public int AvgHeartRate { get; set; }

    /// <summary>Pace in min/km, computed from stored duration and distance.</summary>
    public double PaceMinPerKm => DistanceKm > 0 ? (double)DurationMinutes / DistanceKm : 0;
}
