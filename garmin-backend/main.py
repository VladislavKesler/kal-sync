import os

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from garmin_service import GarminService
from models import ActivityResponse, ZoneData

load_dotenv()

app = FastAPI(title="Garmin Calorie Calculator", version="0.3.0")

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
async def get_latest_activity() -> ActivityResponse:
    try:
        stats = await garmin_service.get_user_stats()
        activity = await garmin_service.get_latest_activity()
    except ValueError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Garmin API error: {exc}") from exc

    # Use Garmin's own daily active-calorie tracking (all movement above BMR).
    active_calories = float(stats["active_calories"])
    garmin_calories = float(activity["garmin_calories"])

    return ActivityResponse(
        activity_date=activity["activity_date"],
        duration_minutes=activity["duration_minutes"],
        avg_hr=activity["avg_hr"],
        max_hr=activity["max_hr"],
        garmin_calories=garmin_calories,
        calculated_calories=active_calories,
        difference=garmin_calories - active_calories,
        zones=ZoneData(**activity["zones"]),
    )
