namespace kal_sync.Models;

/// <summary>
/// Three-state indicator for monthly bodyweight gain rate.
/// Green  = &lt;0.5% / month  (lean bulk on track)
/// Yellow = 0.5–1.0% / month (acceptable, but watch it)
/// Red    = ≥1.0% / month   (gaining too fast, likely excess fat)
/// </summary>
public enum TrafficLight
{
    Green,
    Yellow,
    Red,
}
