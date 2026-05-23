using SQLite;

namespace kal_sync.Models;

public class TennisSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int DurationMinutes { get; set; }
    public int CaloriesBurned { get; set; }
    public int AvgHeartRate { get; set; }
}
