from enum import StrEnum

from pydantic import BaseModel, Field


class CardioSport(StrEnum):
    """What the watch's generic "Cardio" profile actually records."""

    TENNIS_SINGLES = "tennis_singles"
    TENNIS_DOUBLES = "tennis_doubles"
    GENERIC = "generic"


class ZoneData(BaseModel):
    zone1_minutes: int = Field(ge=0)
    zone2_minutes: int = Field(ge=0)
    zone3_minutes: int = Field(ge=0)
    zone4_minutes: int = Field(ge=0)
    zone5_minutes: int = Field(ge=0)


class ActivityResponse(BaseModel):
    activity_date: str
    duration_minutes: int = Field(gt=0)
    avg_hr: int = Field(gt=0)
    max_hr: int = Field(gt=0)
    garmin_calories: float = Field(ge=0)
    calculated_calories: float = Field(ge=0)
    difference: float
    zones: ZoneData
    confidence: str = Field(default="medium")
    confidence_note: str | None = Field(default=None)


class ActivityEstimate(BaseModel):
    activity_id: str
    start_time: str
    activity_type: str
    sport_label: str
    duration_minutes: int = Field(ge=0)
    avg_hr: int = Field(ge=0)
    max_hr: int = Field(ge=0)
    garmin_calories: float = Field(ge=0)
    gross_kcal: float = Field(ge=0)
    net_kcal: float = Field(ge=0)
    method: str
    confidence: str
    confidence_note: str | None = None


class DaySummaryResponse(BaseModel):
    date: str
    activities: list[ActivityEstimate]
    activity_kcal: float = Field(ge=0)
    steps: int = Field(ge=0)
    neat_kcal: float = Field(ge=0)
    garmin_active_kcal: float | None = None
