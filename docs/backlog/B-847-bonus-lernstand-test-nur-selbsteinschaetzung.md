---
tags: [typ/story, status/idee, bereich/backend]
aliases: []
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: offen
vertragsbruch: offen
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-660)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-847 · B-660 AK 7 ist nur für die Selbsteinschätzungs-Form gemessen

## Ist-Stand am Code

`backend/Pugling.Api.Tests/ExtraTrainingTests.cs:948-969`: `SetupAsync()` legt keine früheren Runden an, die geprüfte Runde läuft darum als `SelfAssess`. Eine Mutante, die `PositionItemProgress` nur bei geprüften Formen (Auswahl, getippt) schreibt, bliebe grün; ebenso eine, die nur `IntroducedAt`/`ImprintedAt` setzt (fehlen im Schnappschuss). Heute verzweigt der Pfad nicht nach Form, der Fehler ist hypothetisch. Vorschlag: `SeedCompletedRuns(s, 4)` vor die Runde, `IntroducedAt`/`ImprintedAt` in den Schnappschuss.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-660 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
