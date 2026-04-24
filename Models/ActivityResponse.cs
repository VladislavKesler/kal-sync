using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace kal_sync.Models;

public class ActivityResponse
{
    [JsonPropertyName("activity_date")]
    public string? ActivityDate { get; set; } // Nullable!

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("avg_hr")]
    public int AvgHr { get; set; }

    [JsonPropertyName("max_hr")]
    public int MaxHr { get; set; }

    [JsonPropertyName("garmin_calories")]
    public double GarminCalories { get; set; }

    [JsonPropertyName("calculated_calories")]
    public double CalculatedCalories { get; set; }

    [JsonPropertyName("difference")]
    public double Difference { get; set; }

    [JsonPropertyName("zones")]
    public ZoneData? Zones { get; set; } // Nullable!
}

public class ZoneData
{
    [JsonPropertyName("zone1_minutes")]
    public int Zone1Minutes { get; set; }

    [JsonPropertyName("zone2_minutes")]
    public int Zone2Minutes { get; set; }

    [JsonPropertyName("zone3_minutes")]
    public int Zone3Minutes { get; set; }

    [JsonPropertyName("zone4_minutes")]
    public int Zone4Minutes { get; set; }

    [JsonPropertyName("zone5_minutes")]
    public int Zone5Minutes { get; set; }
}