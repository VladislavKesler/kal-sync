"""
Unit tests for calorie_calculator.py

These tests are pure unit tests — no network, no database, no FastAPI app.
They run in milliseconds and should always pass in CI.
"""

import pytest

from calorie_calculator import (
    calculate_calories,
    calculate_calories_keytel,
    calculate_hrr,
    calculate_intensity,
)

# ── HRR tests ───────────────────────────────────────────────────────────────


class TestHeartRateReserve:
    def test_hrr_is_difference_of_max_and_resting(self) -> None:
        assert calculate_hrr(max_hr=185, resting_hr=55) == 130

    def test_hrr_with_low_resting_hr(self) -> None:
        assert calculate_hrr(max_hr=190, resting_hr=40) == 150


# ── Intensity tests ──────────────────────────────────────────────────────────


class TestIntensity:
    def test_intensity_at_max_effort_is_one(self) -> None:
        # When avg_hr == max_hr, intensity should be 1.0
        hrr = calculate_hrr(max_hr=185, resting_hr=55)
        intensity = calculate_intensity(avg_hr=185, resting_hr=55, hrr=hrr)
        assert intensity == pytest.approx(1.0)

    def test_intensity_at_rest_is_zero(self) -> None:
        # When avg_hr == resting_hr, intensity should be 0.0
        hrr = calculate_hrr(max_hr=185, resting_hr=55)
        intensity = calculate_intensity(avg_hr=55, resting_hr=55, hrr=hrr)
        assert intensity == pytest.approx(0.0)

    def test_intensity_at_midpoint(self) -> None:
        hrr = calculate_hrr(max_hr=185, resting_hr=55)  # HRR = 130
        avg_hr = 55 + 65  # exactly halfway = 120
        intensity = calculate_intensity(avg_hr=avg_hr, resting_hr=55, hrr=hrr)
        assert intensity == pytest.approx(0.5)


# ── Full calorie calculation ─────────────────────────────────────────────────


class TestCalorieCalculation:
    """
    Reference values computed manually:
        HRR = max_hr - resting_hr
        intensity = (avg_hr - resting_hr) / HRR
        kcal/min = intensity * (BMR / 1440) * activity_factor
        total = kcal/min * duration_minutes
    """

    def test_zone2_cardio_session(self) -> None:
        # Typical Zone 2 session: 60 min, avg HR 130, max 185, resting 55
        # HRR = 130, intensity = 75/130 ≈ 0.577
        # kcal/min ≈ 0.933 → total ≈ 56.0 kcal
        result = calculate_calories(
            avg_hr=130,
            max_hr=185,
            resting_hr=55,
            duration_minutes=60,
            bmr_kcal=1942.0,
            activity_factor=1.2,
        )
        assert result == pytest.approx(56.0, rel=0.01)

    def test_hiit_session_burns_more_than_zone2(self) -> None:
        # Same duration, higher intensity (avg HR 160) and higher factor (1.8)
        zone2 = calculate_calories(130, 185, 55, 60, 1942.0, 1.2)
        hiit = calculate_calories(160, 185, 55, 60, 1942.0, 1.8)
        assert hiit > zone2

    def test_longer_session_burns_more_calories(self) -> None:
        short = calculate_calories(130, 185, 55, 30, 1942.0, 1.2)
        long_ = calculate_calories(130, 185, 55, 60, 1942.0, 1.2)
        assert long_ == pytest.approx(short * 2, rel=0.001)

    def test_higher_bmr_increases_calories(self) -> None:
        low_bmr = calculate_calories(130, 185, 55, 60, 1500.0, 1.2)
        high_bmr = calculate_calories(130, 185, 55, 60, 2200.0, 1.2)
        assert high_bmr > low_bmr


# ── Keytel (2005) formula ────────────────────────────────────────────────────


class TestKeytelFormula:
    """
    Reference values verified by hand against Keytel 2005 coefficients.
    Male:   (0.6309·HR + 0.1988·W + 0.2017·age − 55.0969) / 4.184
    Female: (0.4472·HR − 0.1263·W + 0.074·age  − 20.4022) / 4.184
    """

    def test_typical_male_60min(self) -> None:
        # avg HR 140, 91 kg, age 30, male, 60 min
        # kcal/min ≈ 13.71 → ×60 ≈ 822.5
        result = calculate_calories_keytel(140, 91.0, 30, True, 60)
        assert result == pytest.approx(822.5, rel=0.01)

    def test_female_burns_less_than_male_same_inputs(self) -> None:
        male = calculate_calories_keytel(140, 70.0, 30, True, 60)
        female = calculate_calories_keytel(140, 70.0, 30, False, 60)
        assert male > female

    def test_longer_duration_scales_linearly(self) -> None:
        half = calculate_calories_keytel(130, 80.0, 28, True, 30)
        full = calculate_calories_keytel(130, 80.0, 28, True, 60)
        assert full == pytest.approx(half * 2, rel=0.01)

    def test_negative_kcal_per_min_is_clamped_to_zero(self) -> None:
        # Very low HR could produce negative raw value → should be ≥0
        result = calculate_calories_keytel(
            avg_hr=50,
            weight_kg=50.0,
            age=20,
            sex_male=True,
            duration_minutes=30,
        )
        assert result >= 0.0
