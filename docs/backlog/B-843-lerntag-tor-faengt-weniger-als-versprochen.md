---
tags: [typ/story, status/abgenommen, bereich/backend]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-730)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
entgangen_bei: [B-730]
wartet_auf: ""
---

# B-843 · Das Tor gegen UTC-Tagesbildung fängt weniger Formen, als B-730 AK 2 verspricht

## Ist-Stand am Code

`backend/Pugling.Api.Tests/ConventionGuardTests.cs:686-694` (`Tage_Entstehen_Nur_Im_Lerntag`): durch das Muster gehen `.Date` auf einer Variablen mit UTC-Zeitpunkt (Beleg, dass die Form vorkommt: `ShopRefillSchedule.cs:60/86`, dort zufällig Wanduhr), `new DateOnly(t.Year, t.Month, t.Day)`, `DateOnly.FromDayNumber`, `.DayOfWeek` auf Zeitpunkten, Datumsformat in interpolierten Strings (siehe B-844), `.ToDateTime(default)` und alles außerhalb von `Controllers/` und `Services/` (z. B. `Exercises/`). Heute versteckt sich dahinter kein Verstoß; das Tor schützt nur weniger, als es behauptet.

## User Story

Als Entwickler möchte ich, dass das Tor `Tage_Entstehen_Nur_Im_Lerntag` die Formen fängt, die B-730 AK 2 verspricht,
damit eine neue UTC-Tagesbildung nicht durch eine Lücke im Muster rutscht.

## Die echte Lücke

Das Tor prüft nur `Controllers/` und `Services/` und nur die Schreibweisen, die vor B-730 im Code standen. Die
Gegenprobe (2026-11-23, `grep` über `Pugling.Api` außer `Data/Migrations`) findet heute außerhalb davon nur `Seed.cs`
(gewollt) und an neuen Formen nur B-844 und `ShopRefillSchedule.cs:60/86` (Wanduhr, gewollt).

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **Ganzes `Pugling.Api`** statt zweier Ordner; ausgenommen `Data/Migrations` (generiert), `Data/Seed.cs` (Demo-Daten,
   wie bisher bewusst) und `LearningDay.cs`. Kosten: ein Lauf über mehr Dateien (Millisekunden).
2. **Neue Formen:** `.Date` auf jedem Ausdruck, `new DateOnly(x.Year, x.Month, x.Day)`, `DateOnly.FromDayNumber`,
   `.ToDateTime(default)` und ein Datumsformat (`dd`, `yyyy`) in einem Interpolationsloch oder `ToString("…")` – außer das
   Loch liest selbst `DayOf(`/`Today`. Ausnahmen bleiben benannte Zeilen: `ShopRefillSchedule` `Utc(now.Date)`.
3. **`.DayOfWeek` bleibt ungedeckt** – bewusst: ein Regex sieht den Typ nicht, und alle heutigen Aufrufe laufen auf
   `DateOnly`, Wanduhr oder einer Entitäts-Spalte. Der Kopf des Tors sagt das, statt es zu versprechen.
4. Das Tor bekommt einen Selbsttest: jede neue Form an einem Beispiel-Satz, damit ein kaputtes Muster nicht still nichts
   findet.

## Akzeptanzkriterien

1. Jede Form aus Entscheidung 2 trifft ein Beispiel im Selbsttest; rot, wenn ihre Alternative im Muster fehlt.
2. Das Tor ist auf dem Stand nach B-844 grün; vor B-844 meldet es `WeakWordReviewService.cs:111`.
3. Eine Datei außerhalb `Controllers/`/`Services/` (etwa `Exercises/`) wird gescannt; rot, wenn die Ordnerliste wieder
   auf zwei schrumpft.

## Schätzung

S, backend (nur Tests), keine Migration, kein Vertragsbruch. Angriffsplan: Muster und Selbsttest erweitern → gegen den
Ausgangsstand laufen lassen (muss B-844 finden) → B-844 bauen → grün. Testweg: `ConventionGuardTests`.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-730 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 117). Rote Probe: das erweiterte Tor gegen den Stand ohne B-844 erwartet rot mit
  `WeakWordReviewService.cs:111`, gemessen rot (1/1, genau diese Zeile); Selbsttest 12/12 grün.
  - AK 1 → `ConventionGuardTests.Lerntag_Tor_Faengt_Jede_Form`; je Alternative eine Mutante, die sie aus dem Muster
    nimmt: M5 `.Date`, M6 `ToDateTime(default)`, M7 `new DateOnly(…Year, …Month, …Day)`, M8 `FromDayNumber`, M9
    Interpolationsloch, M10 Ausnahme `DayOf(`/`Today` im Loch, M11 `ToString("…dd…")` → alle rot.
  - AK 2 → `Tage_Entstehen_Nur_Im_Lerntag`, M3 (B-844 zurück) → rot.
  - AK 3 → `Tage_Entstehen_Nur_Im_Lerntag` (`Assert.Contains … Exercises`), M4 (Scan nur `Services/`) → rot.
  Review (`pugling-reviewer`): 🟡 die Ausnahme `RemarkExportService` wurde am zweiten Doppelpunkt (`HH:mm`) abgeschnitten und prüfte `'UTC'` nie → Ausnahmen als `(Datei, Fragment)`-Tupel (**gezählt**, 1/5); 🟢 Ausschluss `LearningDay.cs` und Dateinamen ohne Pfadtrenner, Filter auf dem absoluten Pfad, Selbsttest ohne die älteren Formen, `.DayOfWeek` auf UTC-Uhren schmal fangbar, Fehlalarm-Klassen ungenannt → alle behoben (ohne Zählung). Nachgemessen: M12 (`DayOfWeek`-Alternative raus) und M13 (`TimeOnly.FromDateTime` raus) → rot; der Tupel-Abgleich selbst ist ungemessen (es gibt heute keine verletzende Zeile mit `HH:mm`), nur gelesen.
- **2026-11-23** — abgenommen (`e3f51ee7`, Sprint 117). Verifikation: Backend 2126/2126 (Release, nach der letzten
  Änderung), Mutanten M1–M13 alle rot, Grundlinie danach grün; `dotnet format --verify-no-changes` meldet nur vier Dateien
  außerhalb des Diffs (Altbestand). Rollengang: Sohn (Wochenvergleich) bzw. Vater (Titel) maschinell über die
  Integrationstests der Berlin-Fabrik – der Fall braucht eine eingefrorene Uhr in einer Umstellungswoche, die ein laufender
  Server nicht hat; die anderen Rollen Regressionszeugen (volle Suite grün, keine Oberfläche im Diff).
