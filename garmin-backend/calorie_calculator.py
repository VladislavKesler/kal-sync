from dataclasses import dataclass
from enum import StrEnum

from models import CardioSport


class ActivityType(StrEnum):
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
    """%HRR-based heuristic (%HRR × BMR/1440 × activity_factor).

    NOT used in the production path — kept only as a documented
    reference/alternative implementation: the per-activity-type factors in
    _ACTIVITY_FACTORS (1.2-1.8) are hand-picked estimates without a
    validated source, so this formula has not been trusted for the actual
    calorie target.
    """
    hrr = calculate_hrr(max_hr, resting_hr)
    intensity = calculate_intensity(avg_hr, resting_hr, hrr)
    kcal_per_min = intensity * (bmr_kcal / 1440) * activity_factor
    return round(kcal_per_min * duration_minutes, 1)


def get_activity_factor(activity_type: ActivityType | None) -> float:
    if activity_type is None:
        return _DEFAULT_FACTOR
    return _ACTIVITY_FACTORS.get(activity_type, _DEFAULT_FACTOR)


# ── Keytel (2005) formula ────────────────────────────────────────────────────
# Source: Keytel et al., "Prediction of energy expenditure from heart rate
# monitoring during submaximal exercise", J Sports Sci, 2005.
#
# Without VO2max:
#   Male:   kJ/min = 0.6309·HR + 0.1988·W + 0.2017·age − 55.0969
#   Female: kJ/min = 0.4472·HR − 0.1263·W + 0.074·age  − 20.4022
# With VO2max (better fit in the original study):
#   Male:   kJ/min = 0.634·HR + 0.404·VO2max + 0.394·W + 0.271·age − 95.7735
#   Female: kJ/min = 0.450·HR + 0.380·VO2max + 0.103·W + 0.274·age − 59.3954
#
# Both variants estimate GROSS energy expenditure (resting share included).

KJ_PER_KCAL = 4.184


def keytel_kcal_per_min(
    avg_hr: int,
    weight_kg: float,
    age: int,
    sex_male: bool,
    vo2max: float | None = None,
) -> float:
    if vo2max is not None:
        if sex_male:
            kj = 0.634 * avg_hr + 0.404 * vo2max + 0.394 * weight_kg + 0.271 * age
            kj -= 95.7735
        else:
            kj = 0.450 * avg_hr + 0.380 * vo2max + 0.103 * weight_kg + 0.274 * age
            kj -= 59.3954
    elif sex_male:
        kj = 0.6309 * avg_hr + 0.1988 * weight_kg + 0.2017 * age - 55.0969
    else:
        kj = 0.4472 * avg_hr - 0.1263 * weight_kg + 0.074 * age - 20.4022
    return max(kj / KJ_PER_KCAL, 0.0)


def calculate_calories_keytel(
    avg_hr: int,
    weight_kg: float,
    age: int,
    sex_male: bool,
    duration_minutes: int,
) -> float:
    """Return total (gross) kcal using the Keytel (2005) heart-rate formula."""
    kcal_per_min = keytel_kcal_per_min(avg_hr, weight_kg, age, sex_male)
    return round(kcal_per_min * duration_minutes, 1)


# ── Sport-specific estimation ────────────────────────────────────────────────
# Every estimator returns GROSS and NET kcal. Net = gross minus the resting
# share (1 MET ≈ 1 kcal/kg/h) so that activity calories can be added on top
# of the BMR without counting the resting metabolism twice.

# MET values: Ainsworth et al., Compendium of Physical Activities (2011).
MET_STRENGTH_ACTIVE_SET = 6.0
MET_STRENGTH_REST = 1.8
MET_STRENGTH_FALLBACK = 3.5

# ACSM metabolic equation for running (VO2 in ml/kg/min, speed in m/min).
ACSM_RUNNING_HORIZONTAL = 0.2
ACSM_RUNNING_VERTICAL = 0.9
ACSM_RESTING_VO2 = 3.5
KCAL_PER_LITRE_O2 = 5.0
MIN_RUNNING_DISTANCE_M = 500.0
MAX_GRADE = 0.3

# Walking: ≈0.5 kcal/kg/km net; stride ≈ 0.414 × body height.
WALKING_NET_KCAL_PER_KG_KM = 0.5
STRIDE_HEIGHT_FACTOR = 0.414
DEFAULT_HEIGHT_CM = 175.0

CONFIDENCE_HIGH = "high"
CONFIDENCE_MEDIUM = "medium"
CONFIDENCE_LOW = "low"

TYPE_RUNNING = {"running", "trail_running", "treadmill_running", "track_running"}
TYPE_CYCLING = {"cycling", "indoor_cycling", "virtual_ride", "road_biking"}
TYPE_STRENGTH = {"strength_training"}
TYPE_TENNIS = {"tennis"}
TYPE_INDOOR_CARDIO = {"indoor_cardio", "cardio"}


@dataclass(frozen=True)
class BodyProfile:
    weight_kg: float
    age: int
    sex_male: bool
    vo2max: float | None = None


@dataclass(frozen=True)
class ActivityInput:
    activity_type: str
    duration_minutes: float
    avg_hr: int
    moving_duration_minutes: float = 0.0
    distance_m: float = 0.0
    elevation_gain_m: float = 0.0


@dataclass(frozen=True)
class CalorieEstimate:
    gross_kcal: float
    net_kcal: float
    method: str
    confidence: str
    note: str | None = None


def resting_kcal(weight_kg: float, minutes: float) -> float:
    return weight_kg * minutes / 60.0


def met_kcal(met: float, weight_kg: float, minutes: float) -> float:
    return met * weight_kg * minutes / 60.0


def _keytel_gross(activity: ActivityInput, profile: BodyProfile) -> float:
    per_min = keytel_kcal_per_min(
        activity.avg_hr,
        profile.weight_kg,
        profile.age,
        profile.sex_male,
        profile.vo2max,
    )
    return per_min * activity.duration_minutes


def _keytel_method(profile: BodyProfile) -> str:
    return "keytel_vo2max" if profile.vo2max is not None else "keytel"


def _finish(
    gross: float, rest: float, method: str, confidence: str, note: str | None
) -> CalorieEstimate:
    return CalorieEstimate(
        gross_kcal=round(gross, 1),
        net_kcal=round(max(gross - rest, 0.0), 1),
        method=method,
        confidence=confidence,
        note=note,
    )


def estimate_keytel(
    activity: ActivityInput, profile: BodyProfile, note: str | None = None
) -> CalorieEstimate:
    return _finish(
        _keytel_gross(activity, profile),
        resting_kcal(profile.weight_kg, activity.duration_minutes),
        _keytel_method(profile),
        CONFIDENCE_MEDIUM,
        note,
    )


def estimate_running(activity: ActivityInput, profile: BodyProfile) -> CalorieEstimate:
    if activity.distance_m < MIN_RUNNING_DISTANCE_M or activity.duration_minutes <= 0:
        return estimate_keytel(
            activity,
            profile,
            note="Keine Distanz aufgezeichnet — HR-basierte Schätzung statt "
            "Distanzformel.",
        )

    speed_m_min = activity.distance_m / activity.duration_minutes
    grade = min(max(activity.elevation_gain_m / activity.distance_m, 0.0), MAX_GRADE)
    net_vo2 = ACSM_RUNNING_HORIZONTAL * speed_m_min + (
        ACSM_RUNNING_VERTICAL * speed_m_min * grade
    )
    litres_per_min = net_vo2 * profile.weight_kg / 1000.0
    net = litres_per_min * KCAL_PER_LITRE_O2 * activity.duration_minutes
    rest = resting_kcal(profile.weight_kg, activity.duration_minutes)
    return _finish(net + rest, rest, "acsm_running", CONFIDENCE_HIGH, None)


def estimate_cycling(activity: ActivityInput, profile: BodyProfile) -> CalorieEstimate:
    return estimate_keytel(activity, profile)


def estimate_strength(activity: ActivityInput, profile: BodyProfile) -> CalorieEstimate:
    duration = activity.duration_minutes
    active = activity.moving_duration_minutes
    rest = resting_kcal(profile.weight_kg, duration)

    if active <= 0 or active > duration:
        gross = met_kcal(MET_STRENGTH_FALLBACK, profile.weight_kg, duration)
        return _finish(
            gross,
            rest,
            "strength_met_fallback",
            CONFIDENCE_LOW,
            "Keine Satz-/Pausenzeiten von Garmin — pauschal 3,5 MET über die "
            "gesamte Dauer.",
        )

    gross = met_kcal(MET_STRENGTH_ACTIVE_SET, profile.weight_kg, active) + met_kcal(
        MET_STRENGTH_REST, profile.weight_kg, duration - active
    )
    return _finish(
        gross,
        rest,
        "strength_set_model",
        CONFIDENCE_MEDIUM,
        "Satz/Pausen-Modell (6,0 / 1,8 MET). Herzfrequenz wird bei Krafttraining "
        "bewusst nicht verwendet — sie überschätzt den Verbrauch deutlich.",
    )


def estimate_tennis(activity: ActivityInput, profile: BodyProfile) -> CalorieEstimate:
    """HR-based (Keytel+VO2max) estimate for tennis — no flat MET blend.

    A blend with a fixed MET value (Ainsworth "tennis, singles" = 8.0)
    was tried first, but real recorded sessions from the same player showed
    it swings wildly with match intensity: it matched Garmin almost exactly
    at avg HR 159 (a hard match) but ran 35 % over Garmin at avg HR 118 (an
    easy match), because the fixed MET assumes constant effort regardless of
    how hard the match actually was. Pure HR-based Keytel tracked Garmin
    consistently (~15-21 % above it) across all three recorded intensities,
    so it replaces the blend entirely — a stable, explainable gap beats an
    unpredictable one.
    """
    gross = _keytel_gross(activity, profile)
    rest = resting_kcal(profile.weight_kg, activity.duration_minutes)
    method = "tennis_keytel_vo2max" if profile.vo2max is not None else "tennis_keytel"
    return _finish(
        gross,
        rest,
        method,
        CONFIDENCE_MEDIUM,
        "HR-basierte Schätzung (Keytel). Bei Stop-and-Go-Sport wie Tennis kann "
        "die Herzfrequenz den tatsächlichen Verbrauch etwas überschätzen — "
        "erfahrungsgemäß ca. 15–20 % über Garmins eigenem Wert.",
    )


def estimate_activity(
    activity: ActivityInput,
    profile: BodyProfile,
    cardio_sport: CardioSport = CardioSport.TENNIS_SINGLES,
) -> CalorieEstimate:
    """Dispatch on Garmin's typeKey to the most suitable estimator."""
    kind = activity.activity_type
    if kind in TYPE_RUNNING:
        return estimate_running(activity, profile)
    if kind in TYPE_CYCLING:
        return estimate_cycling(activity, profile)
    if kind in TYPE_STRENGTH:
        return estimate_strength(activity, profile)
    if kind in TYPE_TENNIS:
        return estimate_tennis(activity, profile)
    if kind in TYPE_INDOOR_CARDIO:
        if cardio_sport in (CardioSport.TENNIS_SINGLES, CardioSport.TENNIS_DOUBLES):
            return estimate_tennis(activity, profile)
        return estimate_keytel(
            activity,
            profile,
            note="Cardio-Profil ohne zugeordnete Sportart — rein HR-basiert.",
        )
    return estimate_keytel(activity, profile)


def sport_label(activity_type: str, cardio_sport: CardioSport) -> str:
    if activity_type in TYPE_RUNNING:
        return "Laufen"
    if activity_type in TYPE_CYCLING:
        return "Radfahren"
    if activity_type in TYPE_STRENGTH:
        return "Krafttraining"
    if activity_type in TYPE_TENNIS:
        return "Tennis"
    if activity_type in TYPE_INDOOR_CARDIO:
        return {
            CardioSport.TENNIS_SINGLES: "Tennis (Einzel)",
            CardioSport.TENNIS_DOUBLES: "Tennis (Doppel)",
            CardioSport.GENERIC: "Cardio",
        }[cardio_sport]
    return activity_type.replace("_", " ").title()


# ── NEAT from steps ──────────────────────────────────────────────────────────


def neat_kcal_from_steps(
    steps: int, weight_kg: float, height_cm: float | None = None
) -> float:
    """Net walking cost of steps taken outside recorded activities."""
    if steps <= 0:
        return 0.0
    stride_m = STRIDE_HEIGHT_FACTOR * (height_cm or DEFAULT_HEIGHT_CM) / 100.0
    km = steps * stride_m / 1000.0
    return round(km * WALKING_NET_KCAL_PER_KG_KM * weight_kg, 1)
