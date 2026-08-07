---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/frontend, lerntechnik/vokabeln, rolle/student]
aliases: [Buchstaben-Tausch, Anagramm, Idee 2]
status: geschaetzt
prio: P5
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#runde-1--idee-2-buchstaben-tausch-eingabe-anagramm
---

# B-05 · Buchstaben-Tausch-Eingabe (Anagramm)

## User Story

Als Sohn möchte ich eine Vokabel auch lernen, indem ich durcheinandergewürfelte Buchstaben in die richtige
Reihenfolge bringe, als Abwechslung zum Tippen und Auswählen.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, **Idee 2**, Entscheidungen 5–7:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#runde-1--idee-2-buchstaben-tausch-eingabe-anagramm).

Kern, belegt: Die Übungsschleife ist bewusst **stage-agnostisch** (branched auf mitgelieferte Felder, nicht
auf einen Stufen-Enum), `components/LetterBoxes.tsx` ist reine Einzelfeld-Eingabe ohne Anagramm, und es gibt
**keine Drag-&-Drop-Library** im Projekt. Die echte Lücke: auf getippten Stufen kennt das Kind die Lösung
nicht (`Reveal == null`), der Server muss die **gemischten Buchstaben** mitschicken — ein neues Facet-Feld,
kein neuer Mechanismus. `StageMechanics.Normalize` trimmt und kleinschreibt, Groß-/Kleinschreibung der
Kacheln ist grading-irrelevant.

## Akzeptanzkriterien

1. Neuer `TestStage`-Wert (z. B. `LetterScramble`) im Vokabel-Typ, im Creator wählbar.
2. `StageFacets` liefert die gemischten Buchstaben, gemischt aus einem **je Sitzung eingefrorenen Seed**.
3. Nicht geeignete Items (mehrwortig, > ~12 Zeichen) werden **für sich** als `LetterBoxes` ausgespielt.
4. Neue Komponente rendert die Kacheln; das zusammengesetzte Wort geht wie gehabt als `GivenAnswer` —
   **kein** Änderungsbedarf am Grading.
5. Gleichwertige Tastatur-/Screenreader-Bedienung neben dem Ziehen.
6. Die Stufe zählt als „getippt" für `RequireTypedTest`.

## Schätzung

**Größe: M** — Server-Teil klein und musterkonform; Frontend mittel wegen **neuer Library** und **zweiter
Bedienart**, die dieselbe Logik nochmal abbilden muss.

- **`vertragsbruch: nein`** — `StageFacets` bekommt ein Feld *additiv*; kein bestehendes fällt weg.
- **Einzige Story mit neuer Abhängigkeit** (Drag & Drop). Darum zuletzt in der Reihe.
- **Nebenbefund aus dem Grillen, hier mit erledigt:** `LetterBoxes` hat heute **keine Obergrenze und keine
  Leerzeichen-Behandlung** — ein Leerzeichen wäre ein leer zu lassendes Kästchen mitten im Wort. Das
  Mehrwort-Problem ist im Projekt ungelöst, nur bisher nicht aufgefallen.
- **Testweg:** Server-Test auf Facet + Sitzungs-Seed (zweimal `/next` liefert dieselbe Anordnung, neue
  Sitzung eine andere); Frontend-E2E auf beide Bedienarten.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme). Bleibt P5 — einzige Story mit neuer
  Abhängigkeit (Drag & Drop), zuletzt in der Reihe.
