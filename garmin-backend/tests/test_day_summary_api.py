"""
API tests for GET /api/day/{day}. GarminService is always mocked.
"""

from contextlib import AbstractContextManager
from typing import Any
from unittest.mock import AsyncMock, patch

import pytest
from httpx import ASGITransport, AsyncClient, Response

from main import app

Patches = tuple[
    AbstractContextManager[AsyncMock],
    AbstractContextManager[AsyncMock],
    AbstractContextManager[AsyncMock],
]


@pytest.fixture
def mock_day_activities() -> list[dict[str, Any]]:
    """Strength in the morning, tennis (recorded as Cardio) in the evening."""
    zones = {f"zone{i}_minutes": 0 for i in range(1, 6)}
    return [
        {
            "activity_id": "1",
            "start_time": "2026-09-17 11:17:54",
            "activity_type": "strength_training",
            "duration_minutes": 52.8,
            "moving_duration_minutes": 18.2,
            "avg_hr": 119,
            "max_hr": 169,
            "garmin_calories": 346.0,
            "distance_m": 0.0,
            "elevation_gain_m": 0.0,
            "steps": 358,
            "zones": zones,
        },
        {
            "activity_id": "2",
            "start_time": "2026-09-17 18:06:19",
            "activity_type": "indoor_cardio",
            "duration_minutes": 49.7,
            "moving_duration_minutes": 0.0,
            "avg_hr": 118,
            "max_hr": 155,
            "garmin_calories": 346.0,
            "distance_m": 0.0,
            "elevation_gain_m": 0.0,
            "steps": 0,
            "zones": zones,
        },
    ]


def _patch_day(activities: list[dict[str, Any]], steps: int = 6528) -> Patches:
    return (
        patch(
            "main.garmin_service.get_activities_for_day",
            new_callable=AsyncMock,
            return_value=activities,
        ),
        patch(
            "main.garmin_service.get_daily_summary",
            new_callable=AsyncMock,
            return_value={"steps": steps, "active_kcal": 792.0},
        ),
        patch(
            "main.garmin_service.get_fitness_profile",
            new_callable=AsyncMock,
            return_value={"vo2max": 44.0, "height_cm": 182.0},
        ),
    )


async def _get(path: str, params: dict[str, Any] | None = None) -> Response:
    async with AsyncClient(
        transport=ASGITransport(app=app), base_url="http://test"
    ) as client:
        return await client.get(path, params=params)


async def test_day_summary_aggregates_all_activities(
    mock_day_activities: list[dict[str, Any]],
) -> None:
    p_act, p_sum, p_fit = _patch_day(mock_day_activities)
    with p_act, p_sum, p_fit:
        response = await _get(
            "/api/day/2026-09-17",
            {"weight_kg": 89.2, "age": 35, "sex_male": True},
        )

    assert response.status_code == 200
    body = response.json()
    assert body["date"] == "2026-09-17"
    assert len(body["activities"]) == 2
    assert body["activities"][0]["method"] == "strength_set_model"
    assert body["activities"][0]["sport_label"] == "Krafttraining"
    assert body["activities"][1]["method"] == "tennis_keytel_vo2max"
    assert body["activities"][1]["sport_label"] == "Tennis (Einzel)"
    assert body["activity_kcal"] == pytest.approx(
        sum(a["net_kcal"] for a in body["activities"]), abs=0.2
    )
    assert body["garmin_active_kcal"] == 792.0


async def test_day_summary_neat_excludes_activity_steps(
    mock_day_activities: list[dict[str, Any]],
) -> None:
    p_act, p_sum, p_fit = _patch_day(mock_day_activities, steps=6528)
    with p_act, p_sum, p_fit:
        response = await _get("/api/day/2026-09-17", {"weight_kg": 89.2})

    body = response.json()
    assert body["steps"] == 6528
    # 6528 − 358 activity steps → 6170 free steps ≈ 207 kcal
    assert body["neat_kcal"] == pytest.approx(207, rel=0.03)


async def test_day_summary_rest_day_has_zero_activity_kcal() -> None:
    p_act, p_sum, p_fit = _patch_day([], steps=1729)
    with p_act, p_sum, p_fit:
        response = await _get("/api/day/2026-09-18")

    body = response.json()
    assert body["activities"] == []
    assert body["activity_kcal"] == 0.0
    assert body["neat_kcal"] > 0.0


async def test_day_summary_cardio_sport_param_changes_label(
    mock_day_activities: list[dict[str, Any]],
) -> None:
    p_act, p_sum, p_fit = _patch_day(mock_day_activities)
    with p_act, p_sum, p_fit:
        response = await _get("/api/day/2026-09-17", {"cardio_sport": "generic"})

    tennis = response.json()["activities"][1]
    assert tennis["sport_label"] == "Cardio"
    assert tennis["method"] == "keytel_vo2max"


async def test_day_summary_rejects_malformed_date() -> None:
    response = await _get("/api/day/17.09.2026")
    assert response.status_code == 422


async def test_day_summary_maps_garmin_failure_to_502() -> None:
    with patch(
        "main.garmin_service.get_activities_for_day",
        new_callable=AsyncMock,
        side_effect=RuntimeError("boom"),
    ):
        response = await _get("/api/day/2026-09-17")

    assert response.status_code == 502
