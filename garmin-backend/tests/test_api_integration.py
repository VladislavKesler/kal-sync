"""
API tests for the FastAPI backend.

Tests spin up the full FastAPI app in-process using httpx's AsyncClient.
The GarminService is always mocked — no real Garmin credentials required.
"""

from typing import Any
from unittest.mock import AsyncMock, patch

import pytest
from httpx import ASGITransport, AsyncClient

from main import app  # noqa: F401  (re-exported for tests)

# ── Fixtures ─────────────────────────────────────────────────────────────────

_STATS_PAYLOAD: dict[str, Any] = {
    "resting_hr": 55,
    "active_calories": 450,
}


@pytest.fixture
def mock_activity() -> dict[str, Any]:
    """A realistic activity payload for mocking GarminService."""
    return {
        "activity_date": "2024-10-15",
        "duration_minutes": 60,
        "avg_hr": 130,
        "max_hr": 185,
        "garmin_calories": 520.0,
        "activity_type": "zone2_cycling",
        "zones": {
            "zone1_minutes": 5,
            "zone2_minutes": 35,
            "zone3_minutes": 15,
            "zone4_minutes": 4,
            "zone5_minutes": 1,
        },
    }


# ── Health endpoint ───────────────────────────────────────────────────────────


async def test_health_endpoint_returns_200() -> None:
    async with AsyncClient(
        transport=ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get("/api/health")

    assert response.status_code == 200


async def test_health_endpoint_returns_ok_status() -> None:
    async with AsyncClient(
        transport=ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get("/api/health")

    assert response.json() == {"status": "ok"}


# ── Activity endpoint ─────────────────────────────────────────────────────────


async def test_latest_activity_returns_200(mock_activity: dict[str, Any]) -> None:
    with (
        patch(
            "main.garmin_service.get_user_stats",
            new_callable=AsyncMock,
            return_value=_STATS_PAYLOAD,
        ),
        patch(
            "main.garmin_service.get_latest_activity",
            new_callable=AsyncMock,
            return_value=mock_activity,
        ),
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    assert response.status_code == 200


async def test_latest_activity_uses_garmin_active_calories(
    mock_activity: dict[str, Any],
) -> None:
    """calculated_calories in the response must equal today's Garmin active calories."""
    with (
        patch(
            "main.garmin_service.get_user_stats",
            new_callable=AsyncMock,
            return_value=_STATS_PAYLOAD,
        ),
        patch(
            "main.garmin_service.get_latest_activity",
            new_callable=AsyncMock,
            return_value=mock_activity,
        ),
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    assert response.status_code == 200
    assert response.json()["calculated_calories"] == pytest.approx(450.0)


async def test_latest_activity_response_shape(
    mock_activity: dict[str, Any],
) -> None:
    """Validates that the response contains all required fields."""
    required_fields = {
        "activity_date",
        "duration_minutes",
        "avg_hr",
        "max_hr",
        "garmin_calories",
        "calculated_calories",
        "difference",
        "zones",
    }

    with (
        patch(
            "main.garmin_service.get_user_stats",
            new_callable=AsyncMock,
            return_value=_STATS_PAYLOAD,
        ),
        patch(
            "main.garmin_service.get_latest_activity",
            new_callable=AsyncMock,
            return_value=mock_activity,
        ),
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    assert response.status_code == 200
    assert required_fields.issubset(response.json().keys())
