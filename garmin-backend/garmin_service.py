import logging
from datetime import date
from typing import Any

from garminconnect import Garmin

logger = logging.getLogger(__name__)


class GarminService:
    def __init__(self, email: str, password: str) -> None:
        self._email = email
        self._password = password
        self._client: Garmin | None = None
        self._fitness_profile: dict[str, float | None] | None = None

    def _ensure_logged_in(self) -> Garmin:
        if self._client is None:
            self._client = Garmin(self._email, self._password)
            self._client.login()
            logger.info("Garmin login successful")
        return self._client

    async def get_latest_activity(self) -> dict[str, Any]:
        client = self._ensure_logged_in()

        activities = client.get_activities(0, 1)
        if not activities:
            raise ValueError("No activities found in Garmin Connect")

        activity = activities[0]
        logger.debug("Fetched activity: %s", activity.get("activityId"))

        max_hr = int(activity.get("maxHR") or 0) or 181

        return {
            "duration_minutes": int((activity.get("duration") or 0) / 60),
            "avg_hr": int(activity.get("averageHR") or 0),
            "max_hr": max_hr,
            "garmin_calories": float(activity.get("calories") or 0),
            "activity_date": activity.get("startTimeLocal", date.today().isoformat())[
                :10
            ],
            "activity_type": activity.get("activityType", {}).get(
                "typeKey", "strength_training"
            ),
            "zones": self._extract_zones(activity),
        }

    async def get_activities_for_day(self, day: str) -> list[dict[str, Any]]:
        """All activities that started on `day` (YYYY-MM-DD), oldest first."""
        client = self._ensure_logged_in()
        raw = client.get_activities_by_date(day, day, sortorder="asc")
        return [self._normalize(a) for a in raw]

    async def get_daily_summary(self, day: str) -> dict[str, Any]:
        """Wellness totals for `day`; fields are None until the watch has synced."""
        client = self._ensure_logged_in()
        summary = client.get_user_summary(day)
        active = summary.get("activeKilocalories")
        return {
            "steps": int(summary.get("totalSteps") or 0),
            "active_kcal": float(active) if active is not None else None,
        }

    async def get_fitness_profile(self) -> dict[str, float | None]:
        """VO2max and body height from the Garmin user profile (cached)."""
        if self._fitness_profile is not None:
            return self._fitness_profile
        client = self._ensure_logged_in()
        try:
            user_data = client.get_user_profile().get("userData", {})
            vo2max = user_data.get("vo2MaxRunning") or user_data.get("vo2MaxCycling")
            height = user_data.get("height")
            profile: dict[str, float | None] = {
                "vo2max": float(vo2max) if vo2max else None,
                "height_cm": float(height) if height else None,
            }
        except Exception:
            logger.warning("Garmin user profile unavailable", exc_info=True)
            profile = {"vo2max": None, "height_cm": None}
        self._fitness_profile = profile
        return profile

    def _normalize(self, activity: dict[str, Any]) -> dict[str, Any]:
        return {
            "activity_id": str(activity.get("activityId") or ""),
            "start_time": str(activity.get("startTimeLocal") or ""),
            "activity_type": activity.get("activityType", {}).get("typeKey", "other"),
            "duration_minutes": (activity.get("duration") or 0) / 60,
            "moving_duration_minutes": (activity.get("movingDuration") or 0) / 60,
            "avg_hr": int(activity.get("averageHR") or 0),
            "max_hr": int(activity.get("maxHR") or 0),
            "garmin_calories": float(activity.get("calories") or 0),
            "distance_m": float(activity.get("distance") or 0),
            "elevation_gain_m": float(activity.get("elevationGain") or 0),
            "steps": int(activity.get("steps") or 0),
            "zones": self._extract_zones(activity),
        }

    def _extract_zones(self, activity: dict[str, Any]) -> dict[str, int]:
        return {
            f"zone{i}_minutes": int((activity.get(f"hrTimeInZone_{i}") or 0) / 60)
            for i in range(1, 6)
        }
