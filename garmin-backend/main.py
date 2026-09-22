import os
from datetime import date

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from calorie_calculator import (
    ActivityInput,
    BodyProfile,
    calculate_calories_keytel,
    estimate_activity,
    neat_kcal_from_steps,
    sport_label,
)
from garmin_service import GarminService
from models import (
    ActivityEstimate,
    ActivityResponse,
    CardioSport,
    DaySummaryResponse,
    ZoneData,
)

load_dotenv()

app = FastAPI(title="Garmin Calorie Calculator", version="0.4.0")

# Fallback profile values, used only when a caller omits the query params
# (e.g. manual curl testing). The MAUI app always sends the real profile.
DEFAULT_WEIGHT_KG = 80.0
DEFAULT_AGE = 30
DEFAULT_SEX_MALE = True

STRENGTH_TRAINING_TYPE = "strength_training"
STRENGTH_CONFIDENCE_NOTE = (
    "HR-basierte Formeln sind bei Krafttraining wenig valide, da die "
    "Herzfrequenz dort kaum mit dem Energieverbrauch korreliert."
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

garmin_service = GarminService(
    email=os.getenv("GARMIN_EMAIL", ""),
    password=os.getenv("GARMIN_PASSWORD", ""),
)


@app.get("/api/health")
async def health_check() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/api/activities/latest", response_model=ActivityResponse)
async def get_latest_activity(
    weight_kg: float = DEFAULT_WEIGHT_KG,
    age: int = DEFAULT_AGE,
    sex_male: bool = DEFAULT_SEX_MALE,
) -> ActivityResponse:
    try:
        activity = await garmin_service.get_latest_activity()
    except ValueError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Garmin API error: {exc}") from exc

    garmin_calories = float(activity["garmin_calories"])

    calculated_calories = calculate_calories_keytel(
        avg_hr=activity["avg_hr"],
        weight_kg=weight_kg,
        age=age,
        sex_male=sex_male,
        duration_minutes=activity["duration_minutes"],
    )

    is_strength = activity["activity_type"] == STRENGTH_TRAINING_TYPE

    return ActivityResponse(
        activity_date=activity["activity_date"],
        duration_minutes=activity["duration_minutes"],
        avg_hr=activity["avg_hr"],
        max_hr=activity["max_hr"],
        garmin_calories=garmin_calories,
        calculated_calories=calculated_calories,
        difference=garmin_calories - calculated_calories,
        zones=ZoneData(**activity["zones"]),
        confidence="low" if is_strength else "medium",
        confidence_note=STRENGTH_CONFIDENCE_NOTE if is_strength else None,
    )


@app.get("/api/day/{day}", response_model=DaySummaryResponse)
async def get_day_summary(
    day: str,
    weight_kg: float = DEFAULT_WEIGHT_KG,
    age: int = DEFAULT_AGE,
    sex_male: bool = DEFAULT_SEX_MALE,
    cardio_sport: CardioSport = CardioSport.TENNIS_SINGLES,
) -> DaySummaryResponse:
    """Every activity of `day` with a sport-specific net-kcal estimate, plus
    NEAT from the steps taken outside those activities."""
    try:
        date.fromisoformat(day)
    except ValueError as exc:
        raise HTTPException(status_code=422, detail="day must be YYYY-MM-DD") from exc

    try:
        activities = await garmin_service.get_activities_for_day(day)
        summary = await garmin_service.get_daily_summary(day)
        fitness = await garmin_service.get_fitness_profile()
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Garmin API error: {exc}") from exc

    profile = BodyProfile(
        weight_kg=weight_kg, age=age, sex_male=sex_male, vo2max=fitness["vo2max"]
    )

    estimates: list[ActivityEstimate] = []
    activity_steps = 0
    for raw in activities:
        activity_input = ActivityInput(
            activity_type=raw["activity_type"],
            duration_minutes=raw["duration_minutes"],
            avg_hr=raw["avg_hr"],
            moving_duration_minutes=raw["moving_duration_minutes"],
            distance_m=raw["distance_m"],
            elevation_gain_m=raw["elevation_gain_m"],
            garmin_calories=raw["garmin_calories"],
        )
        estimate = estimate_activity(activity_input, profile, cardio_sport)
        activity_steps += raw["steps"]
        estimates.append(
            ActivityEstimate(
                activity_id=raw["activity_id"],
                start_time=raw["start_time"],
                activity_type=raw["activity_type"],
                sport_label=sport_label(raw["activity_type"], cardio_sport),
                duration_minutes=int(raw["duration_minutes"]),
                avg_hr=raw["avg_hr"],
                max_hr=raw["max_hr"],
                garmin_calories=raw["garmin_calories"],
                gross_kcal=estimate.gross_kcal,
                net_kcal=estimate.net_kcal,
                method=estimate.method,
                confidence=estimate.confidence,
                confidence_note=estimate.note,
            )
        )

    free_steps = max(summary["steps"] - activity_steps, 0)

    return DaySummaryResponse(
        date=day,
        activities=estimates,
        activity_kcal=round(sum(e.net_kcal for e in estimates), 1),
        steps=summary["steps"],
        neat_kcal=neat_kcal_from_steps(free_steps, weight_kg, fitness["height_cm"]),
        garmin_active_kcal=summary["active_kcal"],
    )
