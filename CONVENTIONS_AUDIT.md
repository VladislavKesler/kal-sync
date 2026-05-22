# Conventions Audit — kal-sync

> Erstellt: 2026-05-22

Geprüfte Konventionen:
- C# / XAML: PascalCase, beschreibende Namen, Dateiname = Typ-Name
- Python: snake_case für Dateien/Funktionen

---

## C# / XAML — Dateinamen

| Datei | Status | Anmerkung |
|-------|--------|-----------|
| `ActivityResponse.cs` | ✅ | PascalCase, Name = Typ |
| `App.xaml / App.xaml.cs` | ✅ | MAUI-Standard |
| `AppShell.xaml / .cs` | ✅ | MAUI-Standard |
| `DoubleToIntStringConverter.cs` | ✅ | PascalCase, klarer Name |
| `EmptyStateView.xaml / .cs` | ✅ | PascalCase |
| `GaintainingService.cs` | ⚠️ | Tippfehler im Namen — sollte `GainingService.cs` oder `GainingService.cs` heißen; Klasse lautet aktuell `GaintainingService` (zusammengesetzt aus "Gaining" + "Maintaining"?) — bitte klären, ob der Name beabsichtigt ist |
| `GarminApiServices.cs` | ⚠️ | Plural `Services` im Dateinamen, Klasse heißt `GarminApiService` (Singular) — sollte `GarminApiService.cs` heißen |
| `GlobalSuppressions.cs` | ✅ | Analyzer-Standard |
| `GlobalXmlns.cs` | ✅ | Projektspezifischer Standard |
| `HomePage.xaml / .cs` | ✅ | PascalCase |
| `HomeViewModel.cs` | ✅ | PascalCase |
| `InvertedBoolConverter.cs` | ✅ | PascalCase |
| `LeanBodyMassConverter.cs` | ✅ | PascalCase |
| `MainPage.xaml / .cs` | ✅ | MAUI-Standard (aktuell ungenutzt — Einstieg über HomePage) |
| `MauiProgram.cs` | ✅ | MAUI-Standard |
| `SettingsPage.xaml / .cs` | ✅ | PascalCase |
| `SettingsViewModel.cs` | ✅ | PascalCase |
| `StringNotNullEmptyConverter.cs` | ✅ | PascalCase |
| `SurplusToColorConverter.cs` | ✅ | PascalCase |
| `TargetRingDrawable.cs` | ✅ | PascalCase |
| `TrafficLight.cs` | ✅ | PascalCase |
| `UserProfile.cs` | ✅ | PascalCase |
| `UserProfileService.cs` | ✅ | PascalCase |

### Plattform-spezifische Dateien (Platforms/)

| Datei | Status | Anmerkung |
|-------|--------|-----------|
| `AppDelegate.cs` | ✅ | iOS/macCatalyst MAUI-Standard |
| `MainActivity.cs` | ✅ | Android MAUI-Standard |
| `MainApplication.cs` | ✅ | Android MAUI-Standard |
| `Main.cs` | ✅ | iOS MAUI-Standard |
| `Program.cs` | ✅ | Windows MAUI-Standard |

---

## Python — Dateinamen

| Datei | Status | Anmerkung |
|-------|--------|-----------|
| `calorie_calculator.py` | ✅ | snake_case |
| `garmin_service.py` | ✅ | snake_case |
| `main.py` | ✅ | snake_case, FastAPI-Konvention |
| `models.py` | ✅ | snake_case |

---

## Zusammenfassung

| | Anzahl |
|---|--------|
| ✅ Konform | 29 |
| ⚠️ Umzubenennen (Vorschlag) | 2 |

### Vorgeschlagene Umbenennungen (bitte bestätigen vor Durchführung)

1. `GarminApiServices.cs` → `GarminApiService.cs`
   - Klasse heißt `GarminApiService` (Singular) — Dateiname sollte übereinstimmen

2. `GaintainingService.cs` → Klärung nötig
   - Ist "Gaintaining" (Gaining + Maintaining) ein bewusster App-Begriff? Falls ja: ✅ Behalten, sonst umbenennen.
