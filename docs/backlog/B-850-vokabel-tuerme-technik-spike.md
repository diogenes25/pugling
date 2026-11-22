---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-822
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-850 · Vokabel-Türme: Godot-C#-App meldet sich auf dem Android-Handy an

Teil 1 von 4 aus B-822, ohne Abhängigkeit. Ein leeres Godot-4-C#-Projekt unter `game/` wird als APK
gebaut, läuft auf Friedrichs Handy und holt per `auth/child` über die Tailscale-HTTPS-Adresse ein JWT.
Der Spike misst nebenbei, ob `Pugling.Contracts` (`net10.0`) aus Godot referenzierbar ist. Godot ist
auf dem Entwickler-PC noch nicht installiert.
Entscheidungen und Akzeptanzkriterien: [B-822](B-822-vokabel-tuerme-natives-spiel.md); Spielidee und
Technik: [turm-spiel-plan.md](../turm-spiel-plan.md).

## Ergebnis des Spikes (2026-11-22)

**Der Technikweg trägt.** Gemessen auf dem Redmi Note 14 (HyperOS, Mali-G57, Vulkan 1.3) des Vaters:

| Frage | Ergebnis |
| --- | --- |
| Godot-4.7.2-.NET-APK startet auf Android? | Ja: `SPIKE .NET 9.0.19 · Android` im Log, Renderer „Forward Mobile" |
| Login per `auth/child` aus C#? | Ja: „Angemeldet als Sohn (Rolle Student)" gegen eine Wegwerf-API (`:5280`, frisch geseedet) |
| HTTPS aus der eingebetteten .NET-Laufzeit? | Ja: `GET https://godotengine.org/` → 200 — der Weg zur echten Instanz über Tailscale-HTTPS ist offen |
| Klartext-HTTP trotz Android-Sperre? | Ja: `HttpClient` spricht direkt Sockets, die Cleartext-Regel greift nur für Java-HTTP und WebView |
| `Pugling.Contracts` (`net10.0`) nutzbar? | **Nicht als Projektverweis**: die Android-Exportvorlage kann höchstens `net9.0` („C# project targets 'net10.0' but the export template only supports 'net9.0'"; `net10.0` nur über Gradle-Build). **Die Quelldateien kompilieren unter `net9.0`** und sind per `<Compile Include>` eingebunden — eine Quelle der Wahrheit, keine Kopie |

Folgen und Grenzen:

- **Das Spiel zielt auf `net9.0`**, bis Godot `net10.0` im Standard-Export kann. Am Desktop rollt es auf die
  installierte .NET-10-Laufzeit vor (gemessen: `.NET 10.0.11 · Windows`).
- **Risiko der Quell-Einbindung:** Nutzt `Pugling.Contracts` künftig C#-14- oder `net10.0`-only-Sprachmittel,
  bricht der Spiel-Build. Kein Tor fängt das heute, weil `game/` nicht in `Pugling.sln` und nicht in CI ist.
  Gegenmittel ab B-853: `game/PuglingTower.csproj` in CI mitbauen.
- **Die Debug-APK ist 104 MB** (Mono-Laufzeit, nur `arm64-v8a`). Für die Söhne egal, für einen Store später
  ein Thema (Release-Build, Trimming).
- **Auf dem Handy:** USB-Debugging und „Über USB installieren" in den Entwickleroptionen; `adb reverse tcp:5280
  tcp:5280` leitet das Handy-`127.0.0.1` auf den PC, ohne Firewall-Regel und ohne die echte Instanz auf `:5200`.
  Ablauf in [game/tools/handy-installieren.ps1](../../game/tools/handy-installieren.ps1).
- **Werkzeuge** (nicht im Repo): Godot 4.7.2 .NET unter `%LOCALAPPDATA%\Programs\Godot`, Exportvorlagen unter
  `%APPDATA%\Godot\export_templates\4.7.2.stable.mono`, Debug-Keystore unter `%APPDATA%\Godot\keystores`,
  JDK-Pfad in den Godot-Editor-Einstellungen. Headless-Prüfung ohne Klicken:
  `Godot_v4.7.2-stable_mono_win64_console.exe --headless --path game -- --autologin=<PIN>`.

## Verlauf

- 2026-11-22 · `idee` angelegt beim Teilen von B-822 (Entscheidung 8).
- 2026-11-22 · Spike gefahren (vom Nutzer beauftragt, ohne die Stufen bis `in-arbeit` zu durchlaufen):
  Technikweg trägt, Ergebnis oben. Status bewusst unverändert — über die Stufe entscheidet der Nutzer.
