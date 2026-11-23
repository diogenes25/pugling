---
tags: [typ/story, status/abgenommen, bereich/backend]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-660)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-847 · B-660 AK 7 ist nur für die Selbsteinschätzungs-Form gemessen

## Ist-Stand am Code

`backend/Pugling.Api.Tests/ExtraTrainingTests.cs:948-969`: `SetupAsync()` legt keine früheren Runden an, die geprüfte Runde läuft darum als `SelfAssess`. Eine Mutante, die `PositionItemProgress` nur bei geprüften Formen (Auswahl, getippt) schreibt, bliebe grün; ebenso eine, die nur `IntroducedAt`/`ImprintedAt` setzt (fehlen im Schnappschuss). Heute verzweigt der Pfad nicht nach Form, der Fehler ist hypothetisch. Vorschlag: `SeedCompletedRuns(s, 4)` vor die Runde, `IntroducedAt`/`ImprintedAt` in den Schnappschuss.

## User Story

Als Vater möchte ich sicher sein, dass eine Bonusrunde in **jeder** Form den Kasten der Plan-Position unberührt lässt,
damit das Bonus-Training die Fälligkeit der Pflicht nicht verschiebt.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **Theorie über die drei Formen** (0, 2, 4 frühere Runden → Selbsteinschätzung, Auswahl, getippt), die Form wird
   zugesichert, damit der Fall seine Form wirklich trifft. Kosten: zwei Testläufe mehr.
2. **`IntroducedAt`/`ImprintedAt` in den Schnappschuss.**

## Akzeptanzkriterien

1. Für jede Form: der Schnappschuss nach der Runde gleicht dem davor. Rot mit einer Mutante, die nur bei getippter Form
   `PositionItemProgress` schreibt; ebenso bei Auswahl.
2. Rot mit einer Mutante, die nur `ImprintedAt` setzt.

## Schätzung

XS, backend (nur Tests), keine Migration, kein Vertragsbruch. Testweg: `ExtraTrainingTests`, Mutanten in
`ExtraTrainingService`.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-660 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 118). Grundlinie 3/3 grün (die Form jedes Falls wird zugesichert). Gegen den alten
  Test (nur Selbsteinschätzung) wären K1/K2 grün geblieben – gelesen, nicht gemessen; gemessen ist der neue Stand:
  - AK 1 → Fall `Typed`, K1 (nur getippt: `Box++`) → rot; Fall `Choice`, K2 (nur Auswahl) → rot; Fall `SelfAssess`, K3 → rot.
  - AK 2 → K4 (nur `ImprintedAt`) und K5 (nur `IntroducedAt`) bei getippter Form → rot.
  Review (`pugling-reviewer`): 🟡 ein Stempel mit `??=` blieb unsichtbar, weil `SeedLeitnerPosition` beide Tage schon
  setzt → eine Zeile ohne Prägetag, eine ohne Einführungstag (**gezählt**, 1/5); 🟢 vier Spalten fehlten im Schnappschuss
  (`NewBonusPaidAt`, `LastMissed`, `RemediationDoneAt`, `SolvedByStage`) → ergänzt; 🟢 plan-weiter Lernstand
  (`ItemProgress`, `ItemReviewEvent`) ungeprüft → ergänzt (beide ohne Zählung).
- **2026-11-23** — abgenommen (Sprint 118, `08a421cb`). Verifikation nach
  der letzten Änderung: Backend 2130/2130 (Release), Vitest 1361/1361, `tsc -b` sauber; Mutanten F1–F3, E1–E2, K1–K10 rot
  (K10 erst nach plan-weiten Lernstand-Zeilen im Aufbau, vorher grün = ohne Beleg). Rollengang Sohn per E2E gegen frisch
  gestartete Server (Ausweichports 5211/5184): `nochmal-nach-pflicht`, `bonus`, `bonus-deckel`, `bonus-deckel-voll`,
  `bonus-satz-je-karte`, `nacharbeit`, `nacharbeit-bis-erledigt` – 7 passed. Vater und Creator Regressionszeugen (Suite
  grün, `bonus-deckel` fährt die Vater-Seite des Deckels).
