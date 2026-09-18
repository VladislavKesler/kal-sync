"""
Unit tests for the sport-specific estimators in calorie_calculator.py.

Pure unit tests — no network, no FastAPI app.
"""

import pytest

from calorie_calculator import (
    MET_STRENGTH_ACTIVE_SET,
    MET_STRENGTH_REST,
    MET_TENNIS_DOUBLES,
    MET_TENNIS_SINGLES,
    ActivityInput,
    BodyProfile,
    calculate_calories_keytel,
    estimate_activity,
    estimate_running,
    estimate_strength,
    estimate_tennis,
    keytel_kcal_per_min,
    met_kcal,
    neat_kcal_from_steps,
    resting_kcal,
    sport_label,
)
from models import CardioSport

MALE_89 = BodyProfile(weight_kg=89.2, age=35, sex_male=True, vo2max=44.0)
MALE_89_NO_VO2 = BodyProfile(weight_kg=89.2, age=35, sex_male=True)


class TestKeytelVo2maxVariant:
    def test_vo2max_variant_is_used_when_provided(self) -> None:
        without = keytel_kcal_per_min(120, 89.2, 35, True)
        with_vo2 = keytel_kcal_per_min(120, 89.2, 35, True, vo2max=44.0)
        assert with_vo2 != pytest.approx(without)
        # (−95.7735 + 0.634·120 + 0.404·44 + 0.394·89.2 + 0.271·35) / 4.184
        assert with_vo2 == pytest.approx(10.21, rel=0.01)

    def test_female_vo2max_variant(self) -> None:
        # (−59.3954 + 0.450·130 + 0.380·40 + 0.103·65 + 0.274·30) / 4.184
        assert keytel_kcal_per_min(130, 65.0, 30, False, 40.0) == pytest.approx(
            6.99, rel=0.01
        )


class TestRunning:
    def test_flat_10k_in_60min_is_about_one_kcal_per_kg_km(self) -> None:
        run = ActivityInput("running", 60.0, 150, distance_m=10_000)
        est = estimate_running(run, MALE_89)
        # ACSM: net VO2 = 0.2 · 166.7 m/min = 33.3 ml/kg/min → ≈ 892 kcal net
        assert est.net_kcal == pytest.approx(892, rel=0.02)
        assert est.gross_kcal == pytest.approx(est.net_kcal + 89.2, rel=0.01)
        assert est.method == "acsm_running"
        assert est.confidence == "high"

    def test_elevation_gain_adds_calories(self) -> None:
        flat = estimate_running(
            ActivityInput("running", 60.0, 150, distance_m=10_000), MALE_89
        )
        hilly = estimate_running(
            ActivityInput(
                "running", 60.0, 150, distance_m=10_000, elevation_gain_m=300
            ),
            MALE_89,
        )
        assert hilly.net_kcal > flat.net_kcal

    def test_missing_distance_falls_back_to_keytel(self) -> None:
        treadmill = ActivityInput("running", 30.0, 150, distance_m=0)
        est = estimate_running(treadmill, MALE_89)
        assert est.method == "keytel_vo2max"
        assert est.note is not None


class TestStrength:
    def test_set_rest_model_matches_met_arithmetic(self) -> None:
        session = ActivityInput(
            "strength_training", 52.8, 119, moving_duration_minutes=18.2
        )
        est = estimate_strength(session, MALE_89)
        expected_gross = met_kcal(MET_STRENGTH_ACTIVE_SET, 89.2, 18.2) + met_kcal(
            MET_STRENGTH_REST, 89.2, 52.8 - 18.2
        )
        assert est.gross_kcal == pytest.approx(expected_gross, abs=0.1)
        assert est.net_kcal == pytest.approx(
            expected_gross - resting_kcal(89.2, 52.8), abs=0.1
        )
        assert est.method == "strength_set_model"

    def test_strength_ignores_heart_rate(self) -> None:
        low = estimate_strength(
            ActivityInput("strength_training", 50.0, 100, 20.0), MALE_89
        )
        high = estimate_strength(
            ActivityInput("strength_training", 50.0, 160, 20.0), MALE_89
        )
        assert low.net_kcal == high.net_kcal

    def test_strength_is_far_below_keytel(self) -> None:
        # Real session 17.09.: Keytel said 565 kcal, Garmin 346, set model ≈ 255.
        session = ActivityInput("strength_training", 52.8, 119, 18.2)
        est = estimate_strength(session, MALE_89_NO_VO2)
        assert est.gross_kcal < calculate_calories_keytel(119, 89.2, 35, True, 52)

    @pytest.mark.parametrize("moving", [0.0, 60.0])
    def test_invalid_moving_duration_uses_flat_met_fallback(
        self, moving: float
    ) -> None:
        est = estimate_strength(
            ActivityInput("strength_training", 50.0, 120, moving), MALE_89
        )
        assert est.method == "strength_met_fallback"
        assert est.confidence == "low"


class TestTennis:
    def test_ensemble_is_mean_of_met_and_keytel(self) -> None:
        match = ActivityInput("indoor_cardio", 49.7, 118)
        est = estimate_tennis(match, MALE_89, singles=True)
        gross_met = met_kcal(MET_TENNIS_SINGLES, 89.2, 49.7)
        gross_hr = keytel_kcal_per_min(118, 89.2, 35, True, 44.0) * 49.7
        assert est.gross_kcal == pytest.approx((gross_met + gross_hr) / 2, abs=0.1)
        assert est.method == "tennis_ensemble"

    def test_doubles_burns_less_than_singles(self) -> None:
        match = ActivityInput("indoor_cardio", 60.0, 120)
        singles = estimate_tennis(match, MALE_89, singles=True)
        doubles = estimate_tennis(match, MALE_89, singles=False)
        assert doubles.net_kcal < singles.net_kcal
        assert MET_TENNIS_DOUBLES < MET_TENNIS_SINGLES


class TestDispatch:
    def test_indoor_cardio_maps_to_tennis_by_default(self) -> None:
        est = estimate_activity(ActivityInput("indoor_cardio", 50.0, 120), MALE_89)
        assert est.method == "tennis_ensemble"

    def test_indoor_cardio_generic_uses_keytel(self) -> None:
        est = estimate_activity(
            ActivityInput("indoor_cardio", 50.0, 120),
            MALE_89,
            cardio_sport=CardioSport.GENERIC,
        )
        assert est.method == "keytel_vo2max"

    def test_native_tennis_type_is_singles(self) -> None:
        est = estimate_activity(
            ActivityInput("tennis", 50.0, 120), MALE_89, CardioSport.GENERIC
        )
        assert est.method == "tennis_ensemble"

    def test_cycling_uses_keytel_and_plain_keytel_without_vo2max(self) -> None:
        with_vo2 = estimate_activity(ActivityInput("cycling", 32.0, 120), MALE_89)
        without = estimate_activity(ActivityInput("cycling", 32.0, 120), MALE_89_NO_VO2)
        assert with_vo2.method == "keytel_vo2max"
        assert without.method == "keytel"

    def test_unknown_type_falls_back_to_keytel(self) -> None:
        est = estimate_activity(ActivityInput("yoga", 45.0, 90), MALE_89)
        assert est.method == "keytel_vo2max"

    def test_net_never_exceeds_gross_and_never_negative(self) -> None:
        for kind in ["running", "cycling", "strength_training", "indoor_cardio"]:
            est = estimate_activity(ActivityInput(kind, 40.0, 60, 10.0, 5000), MALE_89)
            assert 0.0 <= est.net_kcal <= est.gross_kcal

    @pytest.mark.parametrize(
        ("kind", "sport", "label"),
        [
            ("running", CardioSport.TENNIS_SINGLES, "Laufen"),
            ("strength_training", CardioSport.TENNIS_SINGLES, "Krafttraining"),
            ("indoor_cardio", CardioSport.TENNIS_SINGLES, "Tennis (Einzel)"),
            ("indoor_cardio", CardioSport.TENNIS_DOUBLES, "Tennis (Doppel)"),
            ("indoor_cardio", CardioSport.GENERIC, "Cardio"),
            ("open_water_swimming", CardioSport.GENERIC, "Open Water Swimming"),
        ],
    )
    def test_sport_label(self, kind: str, sport: CardioSport, label: str) -> None:
        assert sport_label(kind, sport) == label


class TestNeat:
    def test_6000_steps_for_182cm_89kg(self) -> None:
        # stride 0.414·1.82 = 0.753 m → 4.52 km · 0.5 · 89.2 ≈ 202 kcal
        assert neat_kcal_from_steps(6000, 89.2, 182.0) == pytest.approx(202, rel=0.02)

    def test_zero_or_negative_steps_is_zero(self) -> None:
        assert neat_kcal_from_steps(0, 89.2, 182.0) == 0.0
        assert neat_kcal_from_steps(-10, 89.2, 182.0) == 0.0

    def test_missing_height_uses_default(self) -> None:
        assert neat_kcal_from_steps(6000, 89.2) > 0.0
