---
tags: [typ/story, status/geschaetzt, bereich/katalog, bereich/frontend, rolle/creator]
aliases: [Übungen je Unit, Unit ohne Zähler]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-creator
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-428 · Lehrwerke: eine Unit zeigt nicht, wie viele Übungen sie schon trägt

Im Rollengang vom 2026-11-08 klappte der Lehrer „Green Line 1" auf: drei Units, je „kein Stoff hinterlegt"
und „+ Übung". Ob Unit 1 schon drei Übungen hat und Unit 2 keine, steht nirgends – dafür muss er unter
Übungen Fach, Reihe und Unit filtern. Wer Material plant, sieht die Lücken im Katalog nicht.

## User Story

Als *Creator* möchte ich in der Unit-Liste je Unit sehen, **wie viele Übungen** sie trägt (und mit einem Klick
dorthin), damit ich erkenne, wo Stoff fehlt, ohne drei Filter zu setzen.

## Ist-Stand am Code

- `SeriesUnitResponse(Id, SeriesId, Grade, OrderIndex, Label, BookType, Topics, Grammar, VocabularyNotes,
  CreatedAt, GrammarTopicIds)` (`TextbookSeriesDtos.cs:51-53`) – kein Zähler.
- `SeriesUnitsController.List` (`:36-46`) materialisiert die Units und mappt sie; `Exercise.SeriesUnitId`
  ist die Pflicht-Beziehung (B-106), ein Count je Unit ist eine Zeile in der Projektion.
- `VaterLehrwerke.tsx:641` zeigt nur die Pille „kein Stoff hinterlegt", `:661` den Knopf „+ Übung"
  (`createExerciseHref`); die Übungsliste nimmt `?subjectId=&seriesId=&seriesUnitId=` (Hausregel „eine Auswahl
  reist als Query mit").

## Die echte Lücke

Ein additives Feld und eine Spalte.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Additives Feld `ExerciseCount` auf `SeriesUnitResponse`**, als DB-Count in der Projektion (kein Laden der
   Übungen). Kosten: ein Feld im Vertrag (additiv), Capture neu.
2. **Spalte „Übungen" in der Unit-Tabelle** mit der Zahl als Link auf `/vater/exercises?subjectId=&seriesId=&seriesUnitId=`;
   `0` steht als Zahl, nicht als Pille – die Pille „kein Stoff hinterlegt" meint die Stoffnotizen, nicht die
   Übungen. Kosten: eine Spalte, ein Link.
3. **Zählung ohne Rollenfilter** (alle Übungen der Unit, auch fremde): die Unit ist geteilter Katalog, die Frage
   ist „ist die Unit versorgt", nicht „habe ich hier etwas". Kosten: keine.

## Akzeptanzkriterien

1. `GET creator/textbook-series/{s}/units` liefert je Unit `exerciseCount`; eine Unit mit drei Übungen → 3, eine
   leere → 0 (Integrationstest, Mutante: Count durch `0` ersetzen → rot).
2. Die Unit-Tabelle zeigt die Spalte „Übungen" mit der Zahl als Link; der Link öffnet die Übungsliste mit den
   drei Filtern gesetzt.
3. Zurückgezogene Übungen zählen mit (Zuweisbarkeit ist eine andere Frage, B-140).

## Schätzung

**Größe: S** — Projektion + Feld + Capture im Backend, Spalte + Test im Frontend. Kein Schema, kein
Vertragsbruch.

**Risiken:** `DocsCaptureTests` ändern sich; `VaterLehrwerke.test.tsx` hat Fixtures ohne das Feld (ergänzen).

**Angriffsplan:** (1) Projektion in `SeriesUnitsController.List`/`Get` + DTO; (2) Test in
`CatalogManagementTests` oder der Units-Testklasse; (3) `gen:contract`; (4) Spalte + Link, Vitest;
(5) Rollengang Creator als Fall in `lehrwerke.spec.ts` („Zahl steht, Link filtert").

**Testweg:** Integrationstest der Units-Liste, `VaterLehrwerke.test.tsx`, `lehrwerke.spec.ts`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Creator ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt.
