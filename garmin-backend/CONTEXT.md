> Zuletzt aktualisiert: 2026-05-22

## Zweck

FastAPI-Backend das Garmin Connect anbindet, Aktivitätsdaten abruft und als REST-API bereitstellt.
Läuft lokal auf Port 8000 — wird vom MAUI-Frontend konsumiert.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `main.py` | FastAPI-App, Routen: `GET /api/health`, `GET /api/activities/latest` |
| `garmin_service.py` | `GarminService`-Klasse: Login + Daten von garminconnect-Library |
| `calorie_calculator.py` | `calculate_calories_keytel()` ist der aktive Berechnungspfad, aufgerufen aus `main.py` für `calculated_calories` (ersetzt den früheren Garmin-Rohwert-Passthrough). `calculate_calories()` (HRR-Heuristik) bleibt als dokumentierte, aktuell unbenutzte Referenzalternative — ihre `_ACTIVITY_FACTORS`-Konstanten haben keine validierte Quelle |
| `models.py` | Pydantic-Modelle: `ActivityResponse`, `ZoneData` |
| `.env` | Garmin-Credentials (nicht committen!) |
| `.env.example` | Template für neue Entwickler |

## Prozess

1. `.env` mit `GARMIN_EMAIL` und `GARMIN_PASSWORD` befüllen
2. `uvicorn main:app --reload --port 8000` starten
3. `GET /api/activities/latest?weight_kg=&age=&sex_male=` gibt `ActivityResponse` JSON zurück (Query-Parameter kommen vom MAUI-Client, der das Profil kennt; ohne sie greifen `DEFAULT_WEIGHT_KG`/`DEFAULT_AGE`/`DEFAULT_SEX_MALE` in `main.py` als Fallback für manuelle Aufrufe)
4. `GarminService` logged sich lazy ein (beim ersten Request)

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
