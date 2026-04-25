import os

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware

from calorie_calculator import (
    ActivityType,
    calculate_calories,
    calculate_calories_keytel,
    get_activity_factor,
)
from garmin_service import GarminService
from models import ActivityResponse, ZoneData

load_dotenv()

app = FastAPI(title="Garmin Calorie Calculator", version="0.2.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# BMR fallback — used only when the MAUI app has not yet saved a user profile.
BMR_KCAL = float(os.getenv("BMR_KCAL", "1942"))

garmin_service = GarminService(
    email=os.getenv("GARMIN_EMAIL", ""),
    password=os.getenv("GARMIN_PASSWORD", ""),
)


@app.get("/api/health")
async def health_check() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/api/activities/latest", response_model=ActivityResponse)
async def get_latest_activity(
    weight_kg: float | None = Query(default=None, description="Body weight in kg (enables Keytel formula)"),
    age: int | None = Query(default=None, description="Age in years (enables Keytel formula)"),
    sex: int | None = Query(default=None, description="0 = male, 1 = female (enables Keytel formula)"),
) -> ActivityResponse:
    try:
        stats = await garmin_service.get_user_stats()
        activity = await garmin_service.get_latest_activity()
    except ValueError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Garmin API error: {exc}") from exc

    resting_hr = stats["resting_hr"]

    # ── Choose formula ───────────────────────────────────────────────────────
    use_keytel = weight_kg is not None and age is not None and sex is not None

    if use_keytel:
        calculated = calculate_calories_keytel(
            avg_hr=activity["avg_hr"],
            weight_kg=weight_kg,  # type: ignore[arg-type]
            age=age,              # type: ignore[arg-type]
            sex_male=(sex == 0),  # type: ignore[operator]
            duration_minutes=activity["duration_minutes"],
        )
    else:
        # Fallback: HRR / intensity formula with env BMR
        try:
            activity_type = ActivityType(activity.get("activity_type", "strength"))
        except ValueError:
            activity_type = ActivityType.STRENGTH

        factor = get_activity_factor(activity_type)
        calculated = calculate_calories(
            avg_hr=activity["avg_hr"],
            max_hr=activity["max_hr"],
            resting_hr=resting_hr,
            duration_minutes=activity["duration_minutes"],
            bmr_kcal=BMR_KCAL,
            activity_factor=factor,
        )

    return ActivityResponse(
        activity_date=activity["activity_date"],
        duration_minutes=activity["duration_minutes"],
        avg_hr=activity["avg_hr"],
        max_hr=activity["max_hr"],
        garmin_calories=activity["garmin_calories"],
        calculated_calories=calculated,
        difference=activity["garmin_calories"] - calculated,
        zones=ZoneData(**activity["zones"]),
    )
