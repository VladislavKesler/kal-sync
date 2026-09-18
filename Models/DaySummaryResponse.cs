using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace kal_sync.Models;

/// <summary>One recorded activity with its sport-specific net-kcal estimate.</summary>
public class ActivityEstimate
{
    [JsonPropertyName("activity_id")]
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>Local start time as "yyyy-MM-dd HH:mm:ss".</summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("activity_type")]
    public string ActivityType { get; set; } = string.Empty;

    [JsonPropertyName("sport_label")]
    public string SportLabel { get; set; } = string.Empty;

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("avg_hr")]
    public int AvgHr { get; set; }

    [JsonPropertyName("max_hr")]
    public int MaxHr { get; set; }

    [JsonPropertyName("garmin_calories")]
    public double GarminCalories { get; set; }

    [JsonPropertyName("gross_kcal")]
    public double GrossKcal { get; set; }

    /// <summary>Gross minus the resting share — what gets added on top of the BMR.</summary>
    [JsonPropertyName("net_kcal")]
    public double NetKcal { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "medium";

    [JsonPropertyName("confidence_note")]
    public string? ConfidenceNote { get; set; }

    public bool IsLowConfidence => Confidence == "low";

    /// <summary>"HH:mm" for the list row; empty when the backend sent no time.</summary>
    public string StartClock => StartTime.Length >= 16 ? StartTime.Substring(11, 5) : string.Empty;
}

/// <summary>All activities of one day plus NEAT from steps taken outside them.</summary>
public class DaySummaryResponse
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("activities")]
    public List<ActivityEstimate> Activities { get; set; } = [];

    /// <summary>Sum of all activities' NetKcal.</summary>
    [JsonPropertyName("activity_kcal")]
    public double ActivityKcal { get; set; }

    [JsonPropertyName("steps")]
    public int Steps { get; set; }

    [JsonPropertyName("neat_kcal")]
    public double NeatKcal { get; set; }

    [JsonPropertyName("garmin_active_kcal")]
    public double? GarminActiveKcal { get; set; }
}
