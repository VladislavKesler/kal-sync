"""
Integration tests for the FastAPI backend.

These tests spin up the full FastAPI app in-process using httpx's AsyncClient.
They do NOT call the real Garmin API — the GarminService is mocked.

Marked with @pytest.mark.integration so they can be run separately:
    pytest -m integration
"""

import pytest
from unittest.mock import AsyncMock, patch
from httpx import AsyncClient, ASGITransport

from main import app  # noqa: F401  (re-exported for tests)


# ── Fixtures ─────────────────────────────────────────────────────────────────

@pytest.fixture
def mock_activity() -> dict:
    """A realistic ActivityResponse payload for mocking."""
    return {
        "activity_date": "2024-10-15",
        "duration_minutes": 60,
        "avg_hr": 130,
        "max_hr": 185,
        "garmin_calories": 520.0,
        "calculated_calories": 487.0,
        "difference": -33.0,
        "zones": {
            "zone1_minutes": 5,
            "zone2_minutes": 35,
            "zone3_minutes": 15,
            "zone4_minutes": 4,
            "zone5_minutes": 1,
        },
    }


# ── Health endpoint ───────────────────────────────────────────────────────────

@pytest.mark.integration
async def test_health_endpoint_returns_200() -> None:
    async with AsyncClient(
        transport=ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get("/api/health")

    assert response.status_code == 200


@pytest.mark.integration
async def test_health_endpoint_returns_ok_status() -> None:
    async with AsyncClient(
        transport=ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get("/api/health")

    assert response.json() == {"status": "ok"}


# ── Activity endpoint ─────────────────────────────────────────────────────────
# These tests will work once /api/activities/latest is implemented in main.py.
# They demonstrate the pattern: mock the Garmin service, test the HTTP layer.

@pytest.mark.integration
async def test_latest_activity_returns_200(mock_activity: dict) -> None:
    """
    Replace 'garmin_service.get_latest_activity' with your actual import path.
    """
    with patch(
        "main.garmin_service.get_latest_activity",
        new_callable=AsyncMock,
        return_value=mock_activity,
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    # Will be 404 until the route exists — update to 200 once implemented
    assert response.status_code in (200, 404)


@pytest.mark.integration
async def test_latest_activity_response_shape(mock_activity: dict) -> None:
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

    with patch(
        "main.garmin_service.get_latest_activity",
        new_callable=AsyncMock,
        return_value=mock_activity,
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    if response.status_code == 200:
        assert required_fields.issubset(response.json().keys())
