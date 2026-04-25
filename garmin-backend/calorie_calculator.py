from enum import Enum


class ActivityType(str, Enum):
    STRENGTH = "strength"
    ZONE2_CYCLING = "zone2_cycling"
    ZONE3 = "zone3"
    HIIT = "hiit"


_ACTIVITY_FACTORS: dict[ActivityType, float] = {
    ActivityType.STRENGTH: 1.5,
    ActivityType.ZONE2_CYCLING: 1.2,
    ActivityType.ZONE3: 1.3,
    ActivityType.HIIT: 1.8,
}

_DEFAULT_FACTOR = 1.3


def calculate_hrr(max_hr: int, resting_hr: int) -> int:
    return max_hr - resting_hr


def calculate_intensity(avg_hr: int, resting_hr: int, hrr: int) -> float:
    return (avg_hr - resting_hr) / hrr


def calculate_calories(
    avg_hr: int,
    max_hr: int,
    resting_hr: int,
    duration_minutes: int,
    bmr_kcal: float,
    activity_factor: float,
) -> float:
    hrr = calculate_hrr(max_hr, resting_hr)
    intensity = calculate_intensity(avg_hr, resting_hr, hrr)
    kcal_per_min = intensity * (bmr_kcal / 1440) * activity_factor
    return round(kcal_per_min * duration_minutes, 1)


def get_activity_factor(activity_type: ActivityType | None) -> float:
    if activity_type is None:
        return _DEFAULT_FACTOR
    return _ACTIVITY_FACTORS.get(activity_type, _DEFAULT_FACTOR)
