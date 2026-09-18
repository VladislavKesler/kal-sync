import os

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from calorie_calculator import calculate_calories_keytel
from garmin_service import GarminService
from models import ActivityResponse, ZoneData

load_dotenv()

app = FastAPI(title="Garmin Calorie Calculator", version="0.3.0")

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

    # Own estimate from the actual activity (HR, duration, body profile) —
    # replaces the old, mislabeled passthrough of Garmin's daily
    # active-calorie total.
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
