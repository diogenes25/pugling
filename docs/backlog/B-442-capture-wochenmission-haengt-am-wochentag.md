---
tags: [typ/story, status/abgenommen, bereich/tests, bereich/doku]
aliases: [Capture Wochenmission Wochentag]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-442 · Die Doku-Capture der Wochenmission hängt am Wochentag

Am Montag, 2026-11-09, schrieb ein voller `dotnet test`-Lauf `docs/api-examples/me.md` und
`backend/Pugling.Api/OpenApi/openapi-examples.generated.json` um: die Wochenmission „NewWords" zeigte `"current": 0` statt `2`.
Der Wochen-Zeitraum beginnt montags neu; die geseedeten bzw. im Test erzeugten neuen Wörter liegen dann vor dem Zeitraum. Damit ist
die Capture nicht byte-stabil (Hausregel aus `DocsCaptureTests`: zeitabhängige Werte maskieren oder in der Test-Factory
neutralisieren). Lauf AF hat die beiden Dateien zurückgesetzt, nicht neu committet.

## Verlauf

- **2026-11-09** — angelegt in Lauf AF (Sprint 25) beim Blick auf `git status` nach dem vollen Testlauf; außerhalb des Sprint-Diffs, darum nicht gezählt und nicht in diesem Sprint gebaut.
- **2026-11-13** — Zwischenstand (Tageslauf): die Vermutung „Seed datiert mit `DateTime.UtcNow`" trägt nicht – der Seed setzt
  kein `IntroducedAt` (`grep` über `Data/`). `ProgressMetric.NewWords` zählt `PositionItemProgress.IntroducedAt` im
  Wochenzeitraum (`MetricsService.cs:25`); die Einführungen entstehen also in den Capture-Schritten selbst. Nächster
  Schritt: nachsehen, welcher Schritt vor `me/missions` Wörter mit einem Tag **vor** dem Wochenbeginn einführt (Uhr
  vorgestellt? Tag einer Sitzung?), dann die Uhr der Factory für die Capture auf einen Mittwoch festsetzen.
- **2026-11-13** — Ursache gefunden und behoben (Sprint 63, autonom gegrillt, `art: Defekt`): `TestApi.MarkIntroduced`
  (über `SeedLeitnerPosition`, `DocsCaptureTests.cs:332`) datiert Einführungen auf „gestern"; montags liegt gestern in der
  Vorwoche, und `ProgressMetric.NewWords` (`MetricsService.cs:25`) zählt im Wochenzeitraum 0 statt 2. `CaptureAll` stellt
  die Test-Uhr montags auf Dienstag; an allen anderen Tagen bleibt sie unverändert. **Nicht an einem Montag gemessen**
  (heute ist Freitag) – die Wirkung ist hergeleitet: `DocsCaptureTests` grün, keine Doku-Änderung an einem Freitag.
