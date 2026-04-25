from pydantic import BaseModel, Field


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
