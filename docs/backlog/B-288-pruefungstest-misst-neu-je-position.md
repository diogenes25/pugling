---
tags: [typ/story, status/idee, bereich/test, bereich/training, rolle/student]
aliases: [Prüfungstest misst „neu je Position“ statt „neu je Versuch“]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-283]
wartet_auf: ""
---

# B-288 · Der Prüfungstest zu „Buchstaben ordnen“ verspricht „neu je Versuch“, misst aber „neu je Position“

`LetterScrambleTests.Pruefung_Liefert_Kacheln_Fest_Je_Versuch_Und_Neu_Je_Versuch` (`:121-146`) startet den zweiten
Versuch auf einer **zweiten Position**. Name und Summary („a new attempt shuffles anew. Red if the seed ignores the
attempt id“) versprechen mehr, als der Aufbau unterscheidet: eine Mutante, die den Seed aus `positionId` statt
`attemptId` zieht (`PositionTestsController.cs:71/232`), bleibt grün – nur die in B-283 gefahrene Mutante
„Konstante 1“ fällt. Der ältere Übungs-Test (`:56-92`) hat dieselbe Bauart mit begründeter Ausnahme („Karte nicht mehr
fällig“); diese Begründung gilt für die Klausur nicht.

**Behebung:** den ersten Versuch durch Beantworten des einen Items abschließen (`CompletedAt != null`; danach gibt
`POST …/tests` laut `PositionTestsController.cs:149-153` einen neuen Versuch mit neuer Id, solange
`MaxAttemptsPerDay` nicht erreicht ist) und den Kachel-Vergleich zwischen zwei Versuchen **derselben** Position ziehen;
Probe: Seed aus `positionId` → rot. Alternativ den Summary ehrlich auf „zwei Versuchs-Ids auf zwei Positionen“
zurücknehmen.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 1 (`4e50370`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 2 des frischen Prüfers; `entgangen_bei: B-283`.
