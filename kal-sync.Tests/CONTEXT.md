> Zuletzt aktualisiert: 2026-05-22

## Zweck

xUnit-v3-Testprojekt für die C#-Business-Logik von kal-sync.
Zielt auf `net9.0` (nicht auf MAUI-Frameworks) — MAUI-Projekt kann nicht direkt referenziert werden.

## Wichtige Einschränkung

Das MAUI-Projekt (`kal-sync.csproj`) zielt auf `net9.0-android`, `net9.0-ios`, `net9.0-windows10.0.19041.0` — nicht auf `net9.0`.
Daher: Getestete Klassen (Services, Models, Calculators) müssen hier **dupliziert** oder in ein separates `kal-sync.Core`-Klassenbibliotheks-Projekt ausgelagert werden.
**Langfristiger Plan**: `kal-sync.Core` (net9.0-Klassenbibliothek) als gemeinsame Basis.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `Services/` | Gespiegelte/isolierte Service-Klassen zum Testen (Logik wird dupliziert, nicht referenziert) |

## Standards

- Testmethoden: `MethodName_Scenario_ExpectedResult` Konvention
- Moq für Mocking, FluentAssertions für Assertions
- Kein `[Fact]` für Tests die externe Ressourcen brauchen (stattdessen `[Trait("Category","Integration")]`)

## Gute Arbeit bedeutet

- Alle Tests laufen ohne Garmin-Credentials
- `dotnet test` läuft im CI ohne setup
- Jeder Service hat mindestens Tests für Happy Path + Fehlerfall

## Nicht tun

- Kein direktes Referenzieren von MAUI-spezifischen APIs (Shell, Application.Current etc.)
- Keine Integration-Tests ohne explizite Markierung
- Keine Test-Datei ohne Assertion (leere Tests vermeiden)
