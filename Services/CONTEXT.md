> Zuletzt aktualisiert: 2026-09-22

## Zweck

Business-Logik, externe API-Kommunikation und Datenzugriff.
Services werden als Singletons per DI in `MauiProgram.cs` registriert.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `GarminApiServices.cs` | HTTP-Client für das Python-Backend: `GetDaySummaryAsync(day)` → `/api/day/{date}` (Produktivpfad), `GetLatestActivityAsync()` → `/api/activities/latest` (Altpfad), `/api/health`; sendet Gewicht/Alter/Geschlecht/`cardio_sport` als Query-Parameter, da das Backend zustandslos ist |
| `UserProfileService.cs` | Lädt/speichert Benutzerprofil (Gewicht, Körperfett-%, Alter, Geschlecht, `CardioSport`-Mapping für das Garmin-Cardio-Profil) lokal; berechnet BMR via Katch-McArdle (`370 + 21.6 × LBM`) |
| `GaintainingService.cs` | Reine Berechnungs-Logik: TDEE = `BMR + netto Aktivkalorien` (Aktivkalorien müssen netto sein, Backend liefert netto). Kein TEF-Aufschlag — TEF hängt an der tatsächlichen Nahrungsaufnahme, die die App nicht kennt, ein pauschaler Aufschlag hätte das Tagesziel unbegründet aufgebläht (entfernt 2026-09-22). Tagesziel (Surplus/Defizit), automatische Defizit-Empfehlung (`CalculateRecommendedAdjustment`) |
| `CalibrationService.cs` | Selbst-Kalibrierung: vergleicht vorhergesagte Kalorienbilanz (`DailyBalance`-Historie) mit gemessenem Gewichtstrend (`BodyMeasurement`-Historie) und leitet einen linearen Korrekturfaktor für die Keytel-Schätzung ab |

## Prozess

1. ViewModel ruft Service-Methode auf (async, Task-basiert)
2. Service kommuniziert mit Backend oder lokalem Speicher
3. Ergebnis als Model-Objekt zurück ans ViewModel
4. Fehler als Exception — ViewModel fängt und setzt `ErrorMessage`

## Standards

- `GarminApiService` implementiert `IDisposable` (HttpClient korrekt freigeben)
- Debug-URL für Android-Emulator: `http://10.0.2.2:8000`, sonst `http://localhost:8000`
- `#if DEBUG` / `#if ANDROID` für plattformspezifische URLs
- Alle async-Methoden enden auf `Async` (z. B. `GetLatestActivityAsync`)

## Gute Arbeit bedeutet

- Kein Netzwerk-Code im ViewModel
- Exception-Typen klar kommunizieren was schief ging (`InvalidOperationException` für Parse-Fehler)
- `JsonSerializerOptions` als `static readonly` gecacht (CA1869)

## Nicht tun

- Keinen direkten Garmin-Connect-SDK-Aufruf aus C# (das macht das Python-Backend)
- Kein `HttpClient` ohne `HttpClientHandler` (SSL-Bypass in Debug nötig)
- Keine Secrets / Credentials in C#-Code oder Config
