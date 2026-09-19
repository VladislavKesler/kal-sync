> Zuletzt aktualisiert: 2026-09-19

## Zweck

Enthält alle ViewModels — die Verbindungsschicht zwischen Services und Views.
Kein UI-Code, kein HTTP-Code — nur Zustandsverwaltung und Kommandos.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `HomeViewModel.cs` | Dashboard-Zustand: BMR, `ActivityKcal` (Σ netto aller heutigen Aktivitäten × Kalibrierfaktor), `NeatKcal`, `ActiveCalories = ActivityKcal + NeatKcal`, TDEE, TargetKcal, CalorieAdjustment; `Activities`-Liste für die Tageskarte; lädt `/api/day/{heute}` — Ruhetag ist gültiger Zustand (`HasData = true`, leere Liste) |
| `SettingsViewModel.cs` | Benutzerprofil-Eingaben: Gewicht, Körperfett, Alter, Geschlecht, Cardio-Profil-Zuordnung (`SelectedCardioSport`, index-aligned mit `CardioSport`-Enum), Benachrichtigungen, Widget |

## Prozess

1. ViewModel erbt von `ObservableObject` (CommunityToolkit.Mvvm)
2. Observable Properties als Backing-Field mit `[ObservableProperty]` Attribut
3. Berechnete Properties (`TargetKcal`, `SurplusLabel`) als get-only Properties mit `[NotifyPropertyChangedFor]` auf abhängigen Feldern
4. Commands mit `[RelayCommand]` auf `async Task`-Methoden
5. View bindet per `x:DataType` compiled — keine Laufzeit-Reflection

## Standards

- Klassen immer `partial` (CommunityToolkit Source Generator braucht das)
- `[ObservableProperty]` auf `private`-Backing-Field (z. B. `_bmr`) — generiert public `Bmr`
- `[NotifyPropertyChangedFor(nameof(X))]` für computed Properties die von anderen abhängen
- Laden von Daten in `PageAppearing`-Command (lazy, nur wenn `!HasData`)
- `IsLoading`, `ErrorMessage`, `HasData` als State-Trias für alle async-Operationen

## Gute Arbeit bedeutet

- ViewModel ist testbar ohne MAUI-Laufzeit (kein `Application.Current`, kein `Shell`)
- Fehler landen in `ErrorMessage`, nicht in einer Exception die die App crasht
- `RefreshData` ruft intern `LoadLatestActivity` auf (kein doppelter Code)

## Nicht tun

- Kein `Navigation.PushAsync` im ViewModel — Navigation im Code-behind
- Keine direkten `HttpClient`-Aufrufe — über Services
- Kein `Thread.Sleep` oder `Task.Delay` ohne Grund
- Keine `static`-ViewModels
