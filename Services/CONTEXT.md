> Zuletzt aktualisiert: 2026-05-22

## Zweck

Business-Logik, externe API-Kommunikation und Datenzugriff.
Services werden als Singletons per DI in `MauiProgram.cs` registriert.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `GarminApiService.cs` | HTTP-Client für das Python-Backend (`/api/activities/latest`, `/api/health`) |
| `UserProfileService.cs` | Lädt/speichert Benutzerprofil (Gewicht, Größe, Alter, Geschlecht) lokal; berechnet BMR via Mifflin-St-Jeor |
| `GaintainingService.cs` | Reine Berechnungs-Logik: TDEE, Tagesziel (Surplus/Defizit) |

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
