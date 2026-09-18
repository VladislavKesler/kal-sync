# PLAN — Realistisches, nicht Garmin-verzerrtes Kalorienziel

> Branch: `feature/real-calorie-calculation`
> Status dieses Dokuments: Ausgangsplan, committed vor jeder Code-Änderung.

## 0. Verifikation des Ist-Zustands (gegengecheckt im Code, Stand dieses Commits)

Alle vom anderen Agenten gemeldeten Punkte sind bestätigt:

- `calculate_calories()` und `calculate_calories_keytel()` (`garmin-backend/calorie_calculator.py:29,57`)
  werden nur in `garmin-backend/tests/test_calorie_calculator.py` aufgerufen. Kein Aufruf in
  `main.py`, keinem C#/MAUI-Code.
- `garmin-backend/main.py:44,53` liest `stats["active_calories"]` (Garmins
  `activeKilocalories`-Tageswert) und gibt ihn als `calculated_calories` zurück — unverändert
  durchgereicht, nicht berechnet.
- Dieser Wert fließt über `Services/GarminApiServices.cs` →
  `Models/ActivityResponse.CalculatedCalories` → `ViewModels/HomeViewModel.cs:130`
  (`ActiveCalories = activity.CalculatedCalories`) → `Services/GaintainingService.CalculateTdee`
  → `TargetKcal`.
- `Services/UserProfileService.cs:69-83` — BMR ist korrekt Katch-McArdle
  (`370 + 21.6 × LBM`), Inputs: Gewicht + manuell eingetragener Körperfett-%.
  `Services/CONTEXT.md:14` behauptet fälschlich Mifflin-St-Jeor — Doku-Fehler, wird korrigiert.
- `Views/HomePage.xaml:194` — `CalorieAdjustment` ist ein manueller `Slider Minimum="-500"
  Maximum="500"`, keine automatische Deckelungs-/Pufferlogik im Code.
- `Models/BodyMeasurement.cs` hat nur `WeightKg`/`BodyFatPercent`, kein Waist/Neck-Feld.
  Keine Yazio-/Ernährungs-App-Anbindung im gesamten Repo gefunden.

**Zusätzlicher Befund, der den Plan beeinflusst:** Das Backend (`main.py`) ist zustandslos und
kennt weder Gewicht noch Alter noch Geschlecht des Nutzers — diese Daten liegen ausschließlich
client-seitig in `UserProfile` (`Preferences`). `calculate_calories_keytel()` braucht aber genau
diese drei Werte. Lösung: Der C#-Client (`GarminApiService`, der `UserProfileService` bereits
injiziert bekommt) hängt sie als Query-Parameter (`weight_kg`, `age`, `sex_male`) an
`/api/activities/latest` an. Das Backend bleibt dadurch stateless (Architekturprinzip aus
`garmin-backend/CONTEXT.md` bleibt erhalten), es gibt keine Profildaten-Duplikation im Backend.
Für direkte/manuelle Aufrufe (curl, Tests) hat der Endpoint Fallback-Defaults
(80 kg / 30 J. / männlich) — nur zur Vermeidung eines harten Fehlers, nicht als Produktivpfad.

**Wichtiger Trade-off, hier bewusst offengelegt (nicht versteckt):** Bisher floss Garmins echter
*Tages*-Summenwert (`activeKilocalories`, alle Bewegung des Tages) in die TDEE-Berechnung ein.
Nach diesem Umbau fließt stattdessen die Keytel-Schätzung für die **letzte einzelne Aktivität**
ein (so wie in Punkt 2 der Aufgabe explizit verlangt). Das behebt die Grundverzerrung
(Original-Ziel: „nicht Garmin-verzerrt"), ändert aber die Semantik: an Ruhetagen ohne neue
Garmin-Aktivität liefert der Endpoint weiterhin die letzte bekannte Aktivität, nicht „0 kcal
heute". Das ist kein Bug dieses Umbaus, sondern bereits so in der bisherigen Architektur
angelegt (`get_latest_activity()` liefert immer die *letzte* Aktivität, nicht „heute"); es wird
durch den Wegfall von Garmins Tages-Aggregat nur sichtbarer. Abhilfe wäre ein echtes
Tages-Aggregat mehrerer Aktivitäten — das ist expliziter Nicht-Teil dieses Tickets und wird hier
nicht mitgelöst, aber im Abschlussbericht als offener Punkt benannt.

## 1. Backend — echte Keytel-Berechnung statt Garmin-Rohwert

**Dateien:** `garmin-backend/main.py`, `garmin-backend/models.py`, `garmin-backend/calorie_calculator.py`

- `main.py`: Endpoint `GET /api/activities/latest` bekommt drei Query-Parameter
  `weight_kg: float`, `age: int`, `sex_male: bool` (mit Fallback-Defaults als benannte
  Konstanten `DEFAULT_WEIGHT_KG`, `DEFAULT_AGE`, `DEFAULT_SEX_MALE`).
- Ruft `calculate_calories_keytel(avg_hr, weight_kg, age, sex_male, duration_minutes)` auf und
  setzt das Ergebnis als `calculated_calories` — jetzt tatsächlich berechnet.
- `garmin_calories` bleibt unverändert als Vergleichswert (`activity["garmin_calories"]`).
- Unterscheidung nach `activity["activity_type"]`: bei `"strength_training"` wird die Antwort
  zusätzlich mit `confidence="low"` und einer erklärenden `confidence_note` markiert; bei allen
  anderen Typen `confidence="medium"` (auch Keytel ist eine Schätzung, kein Ground Truth —
  daher bewusst nicht `"high"`), `confidence_note=null`.
- Der bisherige `stats = await garmin_service.get_user_stats()`-Aufruf entfällt, weil sein
  einziger Verwendungszweck (`active_calories`) wegfällt. Die verwaiste Methode
  `GarminService.get_user_stats()` (einziger Aufrufer war genau diese Zeile) wird aus
  `garmin_service.py` entfernt — sie war die Quelle des ursprünglichen Bugs.
- `calculate_calories()` bleibt bestehen, wird **nicht** gelöscht, bekommt aber einen Docstring,
  der explizit sagt: aktuell nicht im Produktivpfad verwendet, weil die
  `_ACTIVITY_FACTORS`-Werte (1.2/1.3/1.5/1.8) keine validierte Quelle haben — reine
  Referenz-/Alternativimplementierung.
- `models.py`: `ActivityResponse` bekommt `confidence: str` (default `"medium"`) und
  `confidence_note: str | None` (default `None`).

## 2. C#-Client — Profildaten an Backend übergeben, Response-Felder übernehmen

**Dateien:** `Services/GarminApiServices.cs`, `Models/ActivityResponse.cs`

- `GarminApiService.GetLatestActivityAsync()` lädt das `UserProfile` und hängt
  `weight_kg`/`age`/`sex_male` als Query-String an (Invariant-Culture-Formatierung wegen
  Dezimaltrennzeichen).
- `ActivityResponse.cs` bekommt `Confidence` (string) und `ConfidenceNote` (string?).

## 3. Automatische Defizit-Empfehlung statt reinem Slider

**Dateien:** `Services/GaintainingService.cs`, `ViewModels/HomeViewModel.cs`, `Views/HomePage.xaml`

- Neue benannte Konstanten in `GaintainingService` (PascalCase nach C#/StyleCop-Konvention,
  inhaltlich = `MAX_DEFICIT_KCAL` / `MAX_BUFFER_KCAL` aus der Aufgabenstellung):
  `MaxDeficitKcal = -500.0`, `HighActivityThresholdKcal = 600.0`, `MaxBufferKcal = 100.0`.
- Neue Methode `CalculateRecommendedAdjustment(double activeCalories)`: liefert
  `MaxDeficitKcal`, oder `MaxDeficitKcal + MaxBufferKcal`, wenn `activeCalories >
  HighActivityThresholdKcal`.
- `HomeViewModel` bekommt `RecommendedAdjustment` (computed, hängt an `ActiveCalories`) und
  einen Label-Text „Empfohlen: −420 kcal" o. ä.
- Der Slider in `HomePage.xaml` bleibt unverändert bedienbar (manuelle Übersteuerung bleibt
  möglich); daneben wird der Empfehlungswert als Referenz angezeigt.
- Test-Fix: `kal-sync.Tests/Services/UserProfileServiceTests.cs:90-95`
  (`DeficitCap_DefaultShouldBe500`, Scheintest ohne Codebezug) wird entfernt. Ein echter Test
  für `CalculateRecommendedAdjustment` (inkl. der neuen Konstanten, gespiegelt wie der Rest der
  Test-Suite es für `GaintainingService` bereits tut) kommt stattdessen in
  `GaintainingServiceTests.cs`, wo die übrige `GaintainingService`-Logik bereits gespiegelt ist.

## 4. Low-Confidence-Hinweis im Frontend

**Dateien:** `ViewModels/HomeViewModel.cs`, `Views/HomePage.xaml`

- `HomeViewModel` bekommt `IsLowConfidenceActivity` und `ConfidenceNote` (computed, hängen an
  `Activity`).
- In der „Letzte Aktivität"-Karte: kleines ⚠-Glyph-Label (kein neues Bild-Asset nötig), nur
  sichtbar wenn `IsLowConfidenceActivity`, mit `ToolTipProperties.Text` = `ConfidenceNote`. Kein
  Dialog/Alert — dezent, wie gefordert.

## 5. Selbst-Kalibrierung gegen reale Trenddaten

**Dateien:** `Models/BodyMeasurement.cs`, `Services/CalibrationService.cs` (neu),
`ViewModels/HomeViewModel.cs`, `ViewModels/SettingsViewModel.cs`, `Views/SettingsPage.xaml`,
`Views/BodyMeasurementsPage.xaml`, `ViewModels/BodyMeasurementViewModel.cs`, `MauiProgram.cs`

- `BodyMeasurement`: neue optionale Felder `WaistCm` (`double?`), `NeckCm` (`double?`).
  sqlite-net-pcl fügt bei `CreateTableAsync` automatisch fehlende Spalten zu bestehenden
  Tabellen hinzu (keine manuelle Migration nötig). UI-Erfassung (optionale Stepper) wird in
  `BodyMeasurementsPage.xaml` ergänzt, damit das Feld auch wirklich befüllt werden kann.
- Neuer `CalibrationService`:
  - Konstanten: `CalibrationIntervalDays = 21` (alle 2–4 Wochen, wie gefordert),
    `MinBalanceDaysForCalibration = 14`, `MinReliablePredictedBalanceKcal = 700.0` (Signal zu
    klein/verrauscht darunter → keine Korrektur), `KcalPerKg = 7700.0`,
    `MinCorrectionFactor = 0.85`, `MaxCorrectionFactor = 1.15`.
  - Reine, testbare statische Methode `CalculateCorrectionFactor(predictedBalanceKcal,
    actualWeightChangeKg)`: vergleicht die Summe der historischen `DailyBalance.BalanceKcal`
    (= Vorhersage) mit der tatsächlichen Gewichtsänderung × 7700 kcal/kg, clamped auf
    [0.85, 1.15]. Einfache lineare Korrektur, kein ML.
  - Instanzmethode `RunCalibration(balances, measurements)`: prüft Mindestdatenmenge,
    berechnet Faktor, persistiert ihn + Kalibrierungsdatum in `Preferences`.
  - `CorrectionFactor`-Property (persistiert, Default `1.0`) wird in `HomeViewModel` auf
    `ActiveCalories` (den Keytel-Schätzwert) angewendet, **nicht** auf den BMR.
  - `HomeViewModel.LoadLatestActivity()` prüft bei jedem Laden `CalibrationService.IsDue` und
    stößt bei Bedarf `RunCalibration` mit den letzten 21 Tagen `DailyBalance` +
    `BodyMeasurement` an.
- Sichtbarkeit: `SettingsViewModel` bekommt `CorrectionFactor`/`CorrectionFactorLabel`,
  angezeigt in einer neuen Karte „Kalibrierung" auf der Settings-Seite.
- Tests: neue `kal-sync.Tests/Services/CalibrationServiceTests.cs` mit gespiegelter
  `CalculateCorrectionFactor`-Logik (Clamping an beiden Grenzen, Rauschsperre bei kleinem
  Signal, Vorzeichen-Richtung).

## 6. Doku und Regressionstests

- `Services/CONTEXT.md`: Mifflin-St-Jeor → Katch-McArdle korrigieren; `GaintainingService`-Zeile
  um `CalculateRecommendedAdjustment` ergänzen; `CalibrationService` in der Dateitabelle
  ergänzen.
- `garmin-backend/CONTEXT.md`: `calorie_calculator.py`-Beschreibung von „ohne externe
  Abhängigkeiten, unbenutzt" auf „`calculate_calories_keytel()` ist der aktive
  Berechnungspfad in `main.py`; `calculate_calories()` bleibt als dokumentierte, unbenutzte
  Referenzalternative" ändern.
- `garmin-backend/tests/test_api_integration.py`: bestehender Test
  `test_latest_activity_uses_garmin_active_calories` wird ersetzt durch einen Test, der
  `calculate_calories_keytel` tatsächlich patcht/mockt und verifiziert, dass sein Rückgabewert
  (nicht `active_calories`) im Response landet — das verhindert einen zukünftigen stillen
  Rückfall auf den Garmin-Rohwert. Zusätzlicher Test für `confidence="low"` bei
  `strength_training`.

## Reihenfolge der Umsetzung

1. Dieser Plan (Commit 1).
2. Backend (`main.py`, `models.py`, `garmin_service.py`, `calorie_calculator.py`) + Backend-Tests.
3. C#-Client: `ActivityResponse.cs`, `GarminApiServices.cs`.
4. `GaintainingService.cs` + Tests.
5. `CalibrationService.cs` (neu) + Tests + `BodyMeasurement.cs` + DI-Registrierung.
6. ViewModels (`HomeViewModel.cs`, `SettingsViewModel.cs`, `BodyMeasurementViewModel.cs`).
7. Views (`HomePage.xaml`, `SettingsPage.xaml`, `BodyMeasurementsPage.xaml`).
8. CONTEXT.md-Korrekturen.
9. Abschlussbericht (Dateien, neue Konstanten, nächste Schritte für den Nutzer).

Kein PR, kein Merge — Branch wird gepusht, Review bleibt beim Nutzer.
