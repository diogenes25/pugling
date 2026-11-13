---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/student]
aliases: [Üben nach dem Test landet in „Nichts fällig"]
status: ausformuliert
prio: P3
art: Wunsch
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Sprint 64, Review B-430)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-522 · „▶ Üben" nach einem verpatzten Test landet bei Leitner-Positionen oft in „Nichts fällig 🎉"

Gefunden vom `frontend-reviewer` an [B-430](B-430-nach-test-diese-woerter-ueben.md) (c), dort bewusst als Grenze
angenommen. Bei `useLeitner=true` zeigt die Übungsrunde nur fällige Karten; wer erst übt und dann testet, hat heute
nichts mehr fällig. Das Kind, eben durchgefallen, sieht „Nichts fällig 🎉 – Mach den Test oder komm später wieder."
und „🎯 Zum Test" – bei 0 Restversuchen führt das in `test_attempts_exhausted` (`SohnPractice.tsx:291-299`).

Zu klären (Dialog, `art: Wunsch`): (1) Knopf nur zeigen, wenn die Position heute etwas fällig hat (Server liefert ein
Flag, `SohnTest` reicht es durch); (2) ein eigener Leertext nach einer Niederlage ohne 🎉 und ohne verbrauchten Test;
(3) beides. Variante (b) aus B-430 (Test stuft herab) bleibt ausgeschlossen, solange sie Münzen farmbar macht.

## User Story

Als *Sohn* möchte ich nach einem verpatzten Test auf „▶ Üben" einen Bildschirm sehen, der zu meiner Lage passt – entweder
Karten zum Üben oder eine ehrliche Auskunft, wann es weitergeht –, damit ich nicht nach einer Niederlage „🎉" und einen
Test angeboten bekomme, den ich heute nicht mehr schreiben darf.

## Ist-Stand am Code

- `frontend/src/sohn/SohnPractice.tsx:290-300`: Phase `empty` zeigt `practice.nothingDue` („Nichts fällig 🎉") und – wenn
  `testable` – „🎯 Zum Test". Der Bildschirm weiß nicht, dass er von einem verpatzten Test kommt, und nicht, ob heute noch
  Versuche übrig sind.
- `frontend/src/i18n/de.ts:254-256`: die drei Texte; keiner für „nach dem Test".
- `TestSubmitResponse` (`TestResult` in `SohnTest.tsx`) trägt `attemptsRemaining`, aber keine Fälligkeit der Position.
- `PositionStatus.DueCount` (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:20`) liefert die Fälligkeit je Position
  über die Plan-Übersicht schon heute – ein neuer Endpunkt ist für Variante (1) vermutlich nicht nötig.
- Bei `useLeitner=false` filtert die Runde nicht nach Fälligkeit (Review B-430) – das Problem betrifft nur Leitner-Positionen.

## Die echte Lücke

Zwei getrennte Dinge: (a) der Knopf erscheint auch, wenn nichts fällig ist; (b) der Leertext passt nicht zu einer
Niederlage und bietet bei 0 Restversuchen einen Test an, den der Server mit `test_attempts_exhausted` abweist.

## Offene Punkte

1. Variante (1) Knopf nur bei `DueCount > 0` (aus der Übersicht), (2) eigener Leertext nach dem Test (Herkunft per
   Navigation-State, ohne 🎉, ohne Test-Knopf bei 0 Restversuchen), oder (3) beides? Empfehlung PM: (3) – (2) allein ist
   ehrlich, (1) erspart den leeren Umweg.
2. Gilt (2) auch für den Weg von der Startseite (Test schon verbraucht, Übung leer)? Dann gehört die Restversuchs-Info in
   den Übungsbildschirm selbst, nicht in den Navigation-State.

## Verlauf

- **2026-11-13** — angelegt aus dem Review von B-430 (c), Tageslauf Sprint 64.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Ist-Stand am Code belegt, Lücke in (a)/(b) zerlegt, zwei offene Punkte; `art: Wunsch` – Grillen im Dialog.
