---
tags: [typ/story, status/geschaetzt, bereich/medien, bereich/training, rolle/student]
aliases: [Bildwahl einfrieren, Fund 1]
status: geschaetzt
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: ja
quelle: docs/backlog-vokabellernen.md#fund-1--defekt-der-abschlusstest-friert-bildwahlen-ein-die-er-nie-zeigt
---

# B-01 · Abschlusstest friert Bildwahlen ein, die er nie zeigt

**Wirkt heute.** Jeder Abschlusstestlauf schreibt die Motivwahl des Kindes fest — für Bilder, die der Test
selbst nicht rendert. Damit entscheidet der Test still darüber, welches Bild das Kind später in der
Übungsschleife sieht.

## User Story

Als Vater möchte ich, dass ein Abschlusstest die Bildzuordnung meines Sohnes nicht verändert, damit die
Bildkonstanz den Merkeffekt trägt und nicht ein Nebeneffekt des Tests.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, Abschnitt **„Fund 1"** und **Entscheidung 3**:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#fund-1--defekt-der-abschlusstest-friert-bildwahlen-ein-die-er-nie-zeigt).

Kern, belegt: `MediaSelector.SelectForItemsAsync` schreibt die Wahl fest (`MediaSelector.cs:79-90`,
`AddRange` + `SaveFreezeAsync` → `SaveChangesAsync`) und entfernt dabei überholte Wahlen
(`context.Superseded`). `PositionTestsController` reicht `childId` durch, obwohl `SohnTest.tsx` nur
`audioUrl` liest.

## Akzeptanzkriterien

1. `childId` fliegt aus dem Test-Pfad (vier `ItemsOfAsync`-Aufrufe in `PositionTestsController`).
2. `imageUrl`/`imageAlt` fliegen aus `TestItem` und dem Contract-Record — ein Feld, das immer `null` ist,
   ist genau die stille Lüge, gegen die das Projekt sonst mit `unknown_field` kämpft.
3. Ein Testlauf verändert keine `ChildMediaPick`-Zeile mehr.
4. Eine Batch-Abfrage weniger je Testfrage.

## Schätzung

**Größe: S** — eine Durchreichung entfernen, zwei Vertragsfelder streichen.

- **`vertragsbruch: ja`** — `TestItem` verliert zwei Felder; `Pugling.Contracts`, `Pugling.Client` und die
  Frontend-Typen ziehen nach.
- **`migration: nein`** — kein Schema betroffen.
- **Risiko:** `SohnTest.tsx` darf die Felder nicht doch irgendwo lesen (geprüft: liest nur `audioUrl`);
  die Vorschau-Pfade des Vaters gehen über `ExercisePreview`, nicht über `TestItem`.
- **Testweg:** Regressionstest, der nach einem Testlauf die Unverändertheit der `ChildMediaPick`-Zeilen
  prüft (`Pugling.Api.Tests`, bei den Medien-Tests); dazu `/smoke-test` für die HTTP-Schicht.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag; Stufe `geschaetzt` übernommen, weil
  dort schon gegrillt und mit Größe versehen.
