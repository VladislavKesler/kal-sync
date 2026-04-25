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

    def _ensure_logged_in(self) -> Garmin:
        if self._client is None:
            self._client = Garmin(self._email, self._password)
            self._client.login()
            logger.info("Garmin login successful")
        return self._client

    async def get_user_stats(self) -> dict[str, Any]:
        """Fetch today's stats: resting HR from Garmin."""
        client = self._ensure_logged_in()
        stats = client.get_stats(date.today().isoformat())
        return {
            "resting_hr": int(stats.get("restingHeartRate", 60)),
        }

    async def get_latest_activity(self) -> dict[str, Any]:
        client = self._ensure_logged_in()

        activities = client.get_activities(0, 1)
        if not activities:
            raise ValueError("No activities found in Garmin Connect")

        activity = activities[0]
        logger.debug("Fetched activity: %s", activity.get("activityId"))

        max_hr = int(activity.get("maxHR", 0)) or 181

        return {
            "duration_minutes": int(activity.get("duration", 0) / 60),
            "avg_hr": int(activity.get("averageHR", 0)),
            "max_hr": max_hr,
            "garmin_calories": float(activity.get("calories", 0)),
            "activity_date": activity.get("startTimeLocal", date.today().isoformat())[
                :10
            ],
            "activity_type": activity.get("activityType", {}).get(
                "typeKey", "strength_training"
            ),
            "zones": self._extract_zones(activity),
        }

    def _extract_zones(self, activity: dict[str, Any]) -> dict[str, int]:
        return {
            f"zone{i}_minutes": int(activity.get(f"hrTimeInZone_{i}", 0) / 60)
            for i in range(1, 6)
        }
