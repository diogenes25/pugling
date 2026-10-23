---
tags: [typ/story, status/abgenommen, bereich/doku]
aliases: [B-241 Rest, Uebergabe-Notiz Unit-Liste, IA-Plan zwei Links]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 4, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-241]
wartet_auf: ""
nachgeschaut: ""
---

# B-246 · B-241 hat zwei Sätze beim Wortwechsel falsch gemacht und den Sweep vor der letzten Änderung gemessen

Nachschau zu Sprint 4 des zweiten Laufs (Commit `7c33765`), frischer Agent, von mir am Code gegengelesen.
B-241 hat die Kapitel-Ebene aus sieben Doku-Stellen entfernt – und an zwei davon den Satz drumherum nicht
gelesen, obwohl der Reviewer genau diese Klasse an fünf Nachbarsätzen gefunden hatte.

## User Story

Als **Leser der Übergabe-Notiz und des IA-Plans** möchte ich, dass ein Satz, der eine Datei und ein
UI-Element nennt, dieses Element in dieser Datei beschreibt – sonst suche ich eine Unit-Liste, die es nicht gibt.

## Ist-Stand am Code

| Stelle (Stand `7c33765`) | Behauptung | Code |
| --- | --- | --- |
| `docs/uebergabe-vokabeln.md:57-59` | `VaterExercises.tsx` trägt den `VocabRefPicker` und eine „Unit-Liste mit Verwendung + Löschen" | `VocabRefPicker` liegt in `VaterExerciseCreate.tsx` (einzige Fundstelle); `VaterExercises.tsx` hat **keine** Unit-Liste – Units sind ein Filter-Select (`:133-137`), „Verwendung + Löschen" hängt an der **Übungs**-Zeile (`ExerciseManageRow`, `:340-342`) |
| `docs/vater-informationsarchitektur-plan.md:99` | „Auf der Übungen-Seite stehen zwei Links dorthin [zu den Lehrwerken]" | `VaterExercises.tsx` hat **null** Links auf `/vater/lehrwerke`; die zwei Links (`:106-109`) gehen auf Katalog und Lückentexte; den Lehrwerke-Link trägt `/vater/exercises/neu` (`VaterExerciseCreate.tsx:221`) |
| B-241 AK 2 / Verlauf | „Kapitel-Sweep nachher: 1 Treffer (Kopfnotiz)" | Stand `7c33765`: **4 Treffer** – Kopfnotiz `:9`, die „(heute: …)"-Sätze `:93` und `:130`, `Creator.http:11`. Der Sweep lief **vor** der Umformulierung durch den Reviewer und wurde danach nicht wiederholt |

## Die echte Lücke

Zweimal die Fehlerklasse B-112 (Wort ersetzt, Satz nicht gelesen) in der Story, die sie selbst benennt – und
einmal die Lehre aus B-166 (ein Grep, der zur falschen Zeit lief, gilt als Sweep). Der Reviewer hatte fünf
Nachbarsätze gelesen; die zwei hier waren nicht in seinem ±5-Zeilen-Fenster.

## Akzeptanzkriterien

1. `uebergabe-vokabeln.md:57-60` nennt `VocabRefPicker` bei `VaterExerciseCreate.tsx` und beschreibt
   `VaterExercises.tsx` als Übungsliste mit Verwendung + Löschen und Filter Fach/Reihe/Unit.
2. `vater-informationsarchitektur-plan.md:99` sagt, wohin die zwei Links gehen (Katalog, Lückentexte) und wo
   der Lehrwerke-Link steht (Anlege-Seite).
3. Der Kapitel-Sweep aus B-241 läuft **nach** der letzten Änderung; seine Treffer stehen mit Zeile und Grund
   im Verlauf (erwartet: Kopfnotiz `:9`, Heute-Klammern `:93`/`:130`, `Creator.http:11` – alle gewollt).

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`), Sprint 5.

1. **Sofort behoben, im selben Sprint** – wie B-241s Cascade-Kommentar in Sprint 3 des ersten Laufs: zwei
   Sätze, deren Wahrheit am Code ablesbar ist (`grep -c lehrwerke VaterExercises.tsx` = 0; `VocabRefPicker`
   nur in `VaterExerciseCreate.tsx`). **Kosten:** keine Produktentscheidung.
2. **Die vier Sweep-Treffer werden als Ausnahmen benannt, nicht wegformuliert:** die Heute-Klammern `:93`/`:130`
   nennen „Kapitel" als Geschichte, die Kopfnotiz nennt es, um es abzulösen, `Creator.http:11` sagt, dass es
   die Ebene nicht gibt. **Kosten:** eine längere Ausschlussliste; der Sweep ist damit ein Vergleich gegen
   eine benannte Liste, nicht gegen 0.

## Schätzung

**Größe XS** (zwei Sätze, ein Grep). `wo: doku`, keine Migration, kein Vertrag.

## Verlauf

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 4 (Commit `7c33765`), `entgangen_bei: B-241`, und im
  selben Sprint (Sprint 5) behoben: beide Sätze umgeschrieben, Sweep nach der letzten Änderung wiederholt –
  **4 Treffer, alle in der Ausschlussliste benannt** (Kommando und Liste im Protokoll, „Iteration 5").
  Rollengang: Lesen gegen den Code (`VaterExercises.tsx:106-109/:133-137/:340-342`, `VaterExerciseCreate.tsx:221`),
  kein UI geändert. **Dieser Fund beendet den Lauf nach Sprint 5** (Limit 2: Defekt im Increment eines Sprints
  desselben Laufs). Commit `4954bd2` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 5").
