> Zuletzt aktualisiert: 2026-09-19

## Zweck

FastAPI-Backend das Garmin Connect anbindet, Aktivitätsdaten abruft und als REST-API bereitstellt.
Läuft lokal auf Port 8000 — wird vom MAUI-Frontend konsumiert.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `main.py` | FastAPI-App, Routen: `GET /api/health`, `GET /api/day/{YYYY-MM-DD}` (Produktivpfad der App), `GET /api/activities/latest` (Altpfad, nur letzte Aktivität via Keytel) |
| `garmin_service.py` | `GarminService`: Login, `get_activities_for_day()`, `get_daily_summary()` (Schritte, Garmin-Aktivkalorien), `get_fitness_profile()` (VO2max + Größe, gecacht), `get_latest_activity()` |
| `calorie_calculator.py` | Sportartspezifische Schätzer, alle liefern **brutto + netto** (netto = minus 1 MET Ruheanteil, damit BMR nicht doppelt zählt): `estimate_running` (ACSM-Distanzformel, Fallback Keytel ohne Distanz), `estimate_cycling` (Keytel mit VO2max-Term), `estimate_tennis` (Mittel aus MET 8,0/6,0 und Keytel), `estimate_strength` (Satz/Pausen-Modell 6,0/1,8 MET aus `movingDuration`, HR bewusst ignoriert), `estimate_keytel` (Default). `estimate_activity()` dispatcht nach Garmin-`typeKey`; `indoor_cardio` wird per `CardioSport` auf Tennis Einzel/Doppel/Allgemein gemappt. `neat_kcal_from_steps()` für Alltagsbewegung. `calculate_calories()` (HRR-Heuristik) bleibt unbenutzte Referenz |
| `models.py` | Pydantic-Modelle: `DaySummaryResponse`, `ActivityEstimate`, `CardioSport`, `ActivityResponse`, `ZoneData` |
| `.env` | Garmin-Credentials (nicht committen!) |
| `.env.example` | Template für neue Entwickler |

## Prozess

1. `.env` mit `GARMIN_EMAIL` und `GARMIN_PASSWORD` befüllen
2. `uvicorn main:app --reload --port 8000` starten
3. `GET /api/day/2026-09-17?weight_kg=&age=&sex_male=&cardio_sport=tennis_singles` gibt alle Aktivitäten des Tages mit Netto-kcal, Methode und Confidence zurück, plus `neat_kcal` aus den Schritten außerhalb der Aktivitäten. Ruhetag = leere Liste, `activity_kcal = 0`. Query-Parameter kommen vom MAUI-Client; ohne sie greifen `DEFAULT_*` in `main.py` als Fallback für manuelle Aufrufe
4. `GarminService` logged sich lazy ein (beim ersten Request); für `today` liefert Garmin die Tagessumme erst nach Uhr-Sync (vorher `steps = 0`)

## Standards

- Python 3.12, strenge Typ-Annotationen überall (mypy strict)
- Ruff für Linting + Formatierung (Line-Length 88, LF-Zeilenenden)
- Pydantic v2 für alle Datenmodelle
- Tests in `tests/` — Unit-Tests ohne `@pytest.mark.integration` laufen im CI
- Coverage-Minimum: 70 %

## Gute Arbeit bedeutet

- `mypy --strict` läuft ohne Fehler
- `ruff check .` ohne Fehler
- Neue Endpunkte haben ein zugehöriges Pydantic-Modell als `response_model`
- Tests unterscheiden klar zwischen `unit` und `integration` Markern

## Nicht tun

- `.env` nicht committen (steht in `.gitignore`)
- Keine synchronen Garmin-Calls direkt in FastAPI-Routen (via Service kapseln)
- Keine `Any`-Typen ohne explizite mypy-Ausnahme
- Keine neue Python-Version ohne `requires-python` in `pyproject.toml` anzupassen
