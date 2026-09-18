"""
API tests for the FastAPI backend.

Tests spin up the full FastAPI app in-process using httpx's AsyncClient.
The GarminService is always mocked — no real Garmin credentials required.
"""

from typing import Any
from unittest.mock import AsyncMock, patch

import pytest
from httpx import ASGITransport, AsyncClient

from calorie_calculator import calculate_calories_keytel
from main import app  # noqa: F401  (re-exported for tests)

# ── Fixtures ─────────────────────────────────────────────────────────────────


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


@pytest.fixture
def mock_strength_activity(mock_activity: dict[str, Any]) -> dict[str, Any]:
    """Same as mock_activity but flagged as strength training."""
    return {**mock_activity, "activity_type": "strength_training"}


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
    with patch(
        "main.garmin_service.get_latest_activity",
        new_callable=AsyncMock,
        return_value=mock_activity,
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    assert response.status_code == 200


async def test_latest_activity_calls_keytel_formula(
    mock_activity: dict[str, Any],
) -> None:
    """calculated_calories must come from calculate_calories_keytel(), not from
    Garmin's daily active-calorie total. Regression guard against silently
    falling back to the old Garmin passthrough.
    """
    with (
        patch(
            "main.garmin_service.get_latest_activity",
            new_callable=AsyncMock,
            return_value=mock_activity,
        ),
        patch(
            "main.calculate_calories_keytel",
            wraps=calculate_calories_keytel,
        ) as keytel_spy,
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get(
                "/api/activities/latest",
                params={"weight_kg": 91.0, "age": 35, "sex_male": True},
            )

    assert response.status_code == 200
    keytel_spy.assert_called_once_with(
        avg_hr=mock_activity["avg_hr"],
        weight_kg=91.0,
        age=35,
        sex_male=True,
        duration_minutes=mock_activity["duration_minutes"],
    )

    expected = calculate_calories_keytel(
        avg_hr=mock_activity["avg_hr"],
        weight_kg=91.0,
        age=35,
        sex_male=True,
        duration_minutes=mock_activity["duration_minutes"],
    )
    body = response.json()
    assert body["calculated_calories"] == pytest.approx(expected)
    # Must not silently equal a Garmin daily-total-shaped value anymore.
    assert body["calculated_calories"] != pytest.approx(450.0)


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
        "confidence",
        "confidence_note",
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

    assert response.status_code == 200
    assert required_fields.issubset(response.json().keys())


# ── Confidence flag ────────────────────────────────────────────────────────────


async def test_strength_training_gets_low_confidence(
    mock_strength_activity: dict[str, Any],
) -> None:
    with patch(
        "main.garmin_service.get_latest_activity",
        new_callable=AsyncMock,
        return_value=mock_strength_activity,
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    body = response.json()
    assert body["confidence"] == "low"
    assert body["confidence_note"]


async def test_cardio_activity_gets_medium_confidence(
    mock_activity: dict[str, Any],
) -> None:
    with patch(
        "main.garmin_service.get_latest_activity",
        new_callable=AsyncMock,
        return_value=mock_activity,
    ):
        async with AsyncClient(
            transport=ASGITransport(app=app), base_url="http://test"
        ) as client:
            response = await client.get("/api/activities/latest")

    body = response.json()
    assert body["confidence"] == "medium"
    assert body["confidence_note"] is None
