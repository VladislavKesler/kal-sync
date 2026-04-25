import os

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from calorie_calculator import ActivityType, calculate_calories, get_activity_factor
from garmin_service import GarminService
from models import ActivityResponse, ZoneData

load_dotenv()

app = FastAPI(title="Garmin Calorie Calculator", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Personal constants — override via env vars for flexibility
BMR_KCAL = float(os.getenv("BMR_KCAL", "1942"))
RESTING_HR = int(os.getenv("RESTING_HR", "60"))
MAX_HR = int(os.getenv("MAX_HR", "181"))

garmin_service = GarminService(
    email=os.getenv("GARMIN_EMAIL", ""),
    password=os.getenv("GARMIN_PASSWORD", ""),
)


@app.get("/api/health")
async def health_check() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/api/activities/latest", response_model=ActivityResponse)
async def get_latest_activity() -> ActivityResponse:
    try:
        data = await garmin_service.get_latest_activity()
    except ValueError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Garmin API error: {exc}") from exc

    try:
        activity_type = ActivityType(data.get("activity_type", "strength"))
    except ValueError:
        activity_type = ActivityType.STRENGTH

    factor = get_activity_factor(activity_type)
    calculated = calculate_calories(
        avg_hr=data["avg_hr"],
        max_hr=data["max_hr"],
        resting_hr=RESTING_HR,
        duration_minutes=data["duration_minutes"],
        bmr_kcal=BMR_KCAL,
        activity_factor=factor,
    )

    return ActivityResponse(
        activity_date=data["activity_date"],
        duration_minutes=data["duration_minutes"],
        avg_hr=data["avg_hr"],
        max_hr=data["max_hr"],
        garmin_calories=data["garmin_calories"],
        calculated_calories=calculated,
        difference=data["garmin_calories"] - calculated,
        zones=ZoneData(**data["zones"]),
    )
