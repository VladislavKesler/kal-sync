> Zuletzt aktualisiert: 2026-09-22 (2)

## Zweck

FastAPI-Backend das Garmin Connect anbindet, Aktivitätsdaten abruft und als REST-API bereitstellt.
Läuft lokal auf Port 8000 — wird vom MAUI-Frontend konsumiert.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `main.py` | FastAPI-App, Routen: `GET /api/health`, `GET /api/day/{YYYY-MM-DD}` (Produktivpfad der App), `GET /api/activities/latest` (Altpfad, nur letzte Aktivität via Keytel) |
| `garmin_service.py` | `GarminService`: Login, `get_activities_for_day()`, `get_daily_summary()` (Schritte, Garmin-Aktivkalorien), `get_fitness_profile()` (VO2max + Größe, gecacht), `get_latest_activity()` |
| `calorie_calculator.py` | Sportartspezifische Schätzer, alle liefern **brutto + netto** (netto = minus 1 MET Ruheanteil, damit BMR nicht doppelt zählt): `estimate_running`/`estimate_cycling` (seit 2026-09-22: **Garmins eigener `calories`-Wert direkt als netto übernommen** — `estimate_garmin_native()`, Confidence "high"; Formel nur Fallback, falls Garmin keinen Wert liefert — Laufen: ACSM-Distanzformel, dann Keytel; Radfahren: Keytel), `estimate_tennis` (reines Keytel+VO2max, **kein** MET-Blend mehr — s. u.; bewusst *nicht* auf Garmin-Wert umgestellt, da die "Cardio"-Sammelkategorie keinen zuverlässigen Aktivitäts-eigenen Wert hat), `estimate_strength` (Satz/Pausen-Modell 6,0/1,8 MET aus `movingDuration`, HR bewusst ignoriert), `estimate_keytel` (Default/Fallback). `estimate_activity()` dispatcht nach Garmin-`typeKey`; `indoor_cardio` wird per `CardioSport` auf Tennis Einzel/Doppel/Allgemein gemappt (Einzel/Doppel unterscheiden sich seit 2026-09-22 nur noch im Label, nicht mehr in der Formel). `neat_kcal_from_steps()` für Alltagsbewegung. `calculate_calories()` (HRR-Heuristik) bleibt unbenutzte Referenz |
| `models.py` | Pydantic-Modelle: `DaySummaryResponse`, `ActivityEstimate`, `CardioSport`, `ActivityResponse`, `ZoneData` |
| `.env` | Garmin-Credentials (nicht committen!) |
| `.env.example` | Template für neue Entwickler |

## Prozess

1. `.env` mit `GARMIN_EMAIL` und `GARMIN_PASSWORD` befüllen
2. `uvicorn main:app --reload --port 8000` starten
3. `GET /api/day/2026-09-17?weight_kg=&age=&sex_male=&cardio_sport=tennis_singles` gibt alle Aktivitäten des Tages mit Netto-kcal, Methode und Confidence zurück, plus `neat_kcal` aus den Schritten außerhalb der Aktivitäten. Ruhetag = leere Liste, `activity_kcal = 0`. Query-Parameter kommen vom MAUI-Client; ohne sie greifen `DEFAULT_*` in `main.py` als Fallback für manuelle Aufrufe
4. `GarminService` logged sich lazy ein (beim ersten Request); für `today` liefert Garmin die Tagessumme erst nach Uhr-Sync (vorher `steps = 0`)
5. `estimate_tennis()` mischte bis 2026-09-22 einen festen MET-Wert (8,0 "Einzel") zur HR-Schätzung dazu. Mit echten Sessions verifiziert: das schwankte je nach Match-Intensität zwischen Garmin-Wert ±0 % und +35 % (Ainsworth-MET kennt die tatsächliche Intensität nicht). Reines Keytel+VO2max liegt bei allen drei getesteten Intensitäten konsistent 15–21 % über Garmin — bewusst kein Fix auf 1:1-Parität, sondern ein stabiler statt eines schwankenden Unterschieds. Phase-3-Kalibrierung (offen) kann den systematischen Offset später ausgleichen
6. `estimate_running()`/`estimate_cycling()` nutzten bis 2026-09-22 dieselbe Keytel-Formel wie Tennis und lagen dadurch ebenfalls 12–20 % über Garmin. Auf Nutzerwunsch umgestellt: für Lauf-/Rad-Sessions (nicht die "Cardio"-Sammelkategorie) wird jetzt Garmins eigener `calories`-Wert direkt übernommen, weil Garmin bei diesen Sportarten zusätzliche Sensordaten hat (GPS, Höhenmeter, Trittfrequenz/Power), die eine reine Puls-Formel nicht kennt. Wichtig fürs Datenmodell: Garmins Tagessumme erfüllt `activeKilocalories + bmrKilocalories = totalKilocalories` — der `calories`-Wert einer Aktivität ist also bereits **netto** (ohne Ruheanteil) und wird 1:1 als `net_kcal` übernommen, nicht nochmal um den Ruheanteil reduziert

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
