# kal-sync

Kalorie-Dashboard-App (MAUI Cross-Platform) + Python-Backend.
Holt Aktivitätsdaten von Garmin Connect, berechnet BMR/TDEE/Tagesziel.
Zwei Workspaces: MAUI-App (C#) und FastAPI-Backend (Python).

## Tech Stack

| Bereich | Technologie |
|---------|-------------|
| App | .NET 9, .NET MAUI, C# 12, CommunityToolkit.Mvvm 8.4 |
| Backend | Python 3.12, FastAPI, garminconnect, Pydantic v2 |
| Tests (C#) | xUnit v3, Moq, FluentAssertions |
| Tests (Python) | pytest, pytest-asyncio, Coverage ≥ 70 % |
| Linting | Ruff, mypy (strict), StyleCop |

## Befehle

```bash
uvicorn main:app --reload --port 8000   # Backend (aus garmin-backend/)
cd garmin-backend && pytest              # Python-Tests
dotnet build kal-sync.sln -f net9.0-windows10.0.19041.0
dotnet test kal-sync.Tests/kal-sync.Tests.csproj
```

## Routing

| Aufgabe | Ordner | Lies |
|---------|--------|------|
| Neue Seite / UI-Layout | /Views | Views/CONTEXT.md |
| ViewModel / Zustand / Commands | /ViewModels | ViewModels/CONTEXT.md |
| Services, HTTP, Berechnungen | /Services | Services/CONTEXT.md |
| Garmin-Backend, FastAPI-Routen | /garmin-backend | garmin-backend/CONTEXT.md |
| C#-Unit-Tests | /kal-sync.Tests | kal-sync.Tests/CONTEXT.md |

## Konventionen

- MVVM: Views binden nur an ViewModels; Code-behind nur Navigation
- ViewModels: `partial class` + `[ObservableProperty]` auf Backing-Fields
- Services als Singleton in `MauiProgram.cs` registrieren
- XAML: lokale Styles in `ContentPage.Resources`, benannte Farb-Keys
- Python: snake_case Dateien/Funktionen, strikte mypy-Typ-Annotationen
- Dateinamen C#: PascalCase, Dateiname = Typ-Name

## Verboten

- Kein Logik-Code in `.xaml.cs` (nur Navigation/Event-Handler)
- `garmin-backend/.env` nicht committen
- `kal-sync.Tests` referenziert MAUI-Projekt nicht direkt (Target-Konflikt)
- Keine neuen NuGet-Packages ohne Absprache
