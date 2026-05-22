using SQLite;

namespace kal_sync.Models;

public class BodyMeasurement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double WeightKg { get; set; }
    public double BodyFatPercent { get; set; }
}
