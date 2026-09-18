> Zuletzt aktualisiert: 2026-09-19

## Zweck

Enthält alle MAUI-Seiten (XAML + Code-behind) und eine wiederverwendbare View-Komponente.
Dieser Ordner ist rein darstellend — keine Business-Logik hier.

## Dateien

| Datei | Beschreibung |
|-------|--------------|
| `HomePage.xaml/.cs` | Kalorie-Dashboard (Hauptseite): Zielring, Anpassungs-Slider, Tageskarte „Heute" mit `BindableLayout` über `Activities` (DataTemplate `x:DataType="models:ActivityEstimate"`) + NEAT-Zeile |
| `SettingsPage.xaml/.cs` | Benutzerprofil-Einstellungen |
| `EmptyStateView.xaml/.cs` | Wiederverwendbare Leer-Zustands-Anzeige |

## Prozess

1. XAML definiert Layout, Styles und Bindings (`x:DataType` für compiled bindings pflicht)
2. `x:DataType` muss auf das zugehörige ViewModel zeigen
3. Code-behind (`.cs`) enthält ausschließlich Event-Handler für Navigation (z. B. `OnSettingsTapped`)
4. Alle Commands, Properties und Logik im ViewModel

## Standards

- Lokale Styles in `ContentPage.Resources` (nicht in App.xaml, außer global benötigt)
- Farbnamen konsistent: `Bg`, `Surface`, `Ink`, `Muted`, `Accent`, `Danger`
- Fonts: `JetBrainsMono` für Zahlen/Mono, `Inter` für UI-Text
- Kein `Shell.NavBar` / `Shell.TabBar` sichtbar — eigene Tab-Bar im Layout

## Gute Arbeit bedeutet

- Compiled Bindings ohne `{x:DataType}` Error kompilieren
- Kein `Debug.WriteLine` in XAML-Code-behind
- Leerer Zustand (HasData = false) sauber abgedeckt

## Nicht tun

- Keine API-Calls oder Service-Aufrufe in Code-behind
- Keine `static` Methoden in Views
- Keine Styles direkt auf Elemente schreiben, wenn ein benannter Style existiert
