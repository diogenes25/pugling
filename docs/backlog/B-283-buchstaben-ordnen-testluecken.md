---
tags: [typ/story, status/idee, bereich/test, bereich/training, rolle/student]
aliases: [Buchstaben ordnen – Prüfung und Vorschau ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu B-05 in Lauf D (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
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
