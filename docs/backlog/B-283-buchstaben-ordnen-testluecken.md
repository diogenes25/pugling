---
tags: [typ/story, status/abgenommen, bereich/test, bereich/training, rolle/student]
aliases: [Buchstaben ordnen – Prüfung und Vorschau ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu B-05 in Lauf D (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-10-27"
wartet_auf: ""
---

# B-283 · Buchstaben ordnen: neue Anordnung je Prüfungsversuch und die Vorschau sind ungetestet

[B-05](B-05-buchstaben-tausch.md) mischt die Kacheln je Sitzung bzw. je Versuch neu und zeigt sie auch in der
Vorschau des Vaters. Zwei Mutanten bleiben grün (`pugling-reviewer`, Lauf D):

1. **Prüfung:** Ein fester Seed statt `attempt.Id` (`PositionTestsController`) bleibt grün. Der Test prüft nur, dass
   zwei Abrufe in *einem* Versuch gleich sind, nicht dass ein neuer Versuch neu mischt. Er prüft auch nicht, dass
   `reveal`/`answerLength` dort leer sind. Vorschlag: ein zweiter Versuch auf einer zweiten Position, wie im
   Übungsfall, dazu die Null-Prüfungen.
2. **Vorschau:** Fällt `LetterTiles` in `ExercisePreviewService.Present` weg, bleibt alles grün. Kein Test ruft die
   Vorschau mit `stage=7` auf. Vorschlag: Vorschau-Aufruf mit Stufe 7, `letterTiles` muss gesetzt sein.

**In Lauf D bewusst nicht behoben:** Der Zähler stand nach vier wichtigeren Behebungen bei 4 (Großschreibung verrät
den ersten Buchstaben, Item-Index im Seed, Länge beim Rückfall, Klausur-Beschriftung „Stufe 7“), und der Frontend-
Review stand noch aus.

## Verlauf

- **2026-10-26** — angelegt aus dem `pugling-reviewer` zu B-05 in Lauf D ([Protokoll](../pm-sitzung-2026-10-26-tag.md)),
  Funde 2b und 2d.
- **2026-10-27** — in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1 ausformuliert, geschätzt (XS, nur Tests, keine Migration) und gebaut. **Prüfung:** `Pruefung_Liefert_Kacheln_Fest_Je_Versuch_Und_Neu_Je_Versuch` startet einen zweiten Versuch auf einer zweiten Position (Abgrenzung: zwei Versuchs-Ids), die Kacheln müssen sich unterscheiden, `reveal`/`answerLength` sind null. Mutante Seed `attemptId` → Konstante 1: **1 failed** (vorher grün). **Vorschau:** `Preview_Ordnen_LiefertKacheln_FestJeUebung_OhneLoesung` ruft `preview?stage=7`, Kacheln gesetzt, Lösung verborgen, zwei Aufrufe gleich. Mutante `f.LetterTiles` → `null` in `ExercisePreviewService.Present`: **1 failed** (vorher grün). Dazu Fund (b) aus dem Frontend-Review: die E2E-Spec wertet jede Antwort über die Review-Antwort des Servers (`wasCorrect`). Filterlauf **13/13 `exit=0`**.
- **2026-10-27** — `pugling-reviewer` in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)): zwei Kommentar-Funde (Begründung der zweiten Position, Umbau-Erzählung im Summary), berichtigt, ohne Zählung; Abgrenzung je Fall bestätigt. Gesamtsuite **949/949 `exit=0`**. `abgenommen` zusammen mit B-05; Commit `4e50370`.
- **2026-10-27** — Nachschau in Lauf E, Teil 2 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), frischer Prüfer am Commit `4e50370`: Mutanten „Seed konstant“ und „`LetterTiles` → null“ je rot nachvollzogen. **Ein Test-Entgleiten:** der Prüfungstest startet den zweiten Versuch auf einer zweiten Position, ein Seed aus `positionId` bliebe grün → [B-288](B-288-pruefungstest-misst-neu-je-position.md) (`entgangen_bei: B-283`).
