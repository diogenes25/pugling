---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [aenderst du sie bleibt die Aenderung, Feldhilfe pointsGoalMet]
status: ausformuliert
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 2
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-191]
wartet_auf: ""
---

# B-201 · Die Feldhilfe zu den Zielpunkten argumentiert seit Sprint 2 in beide Richtungen

## User Story

Als **Vater** möchte ich, dass der Hilfetext eines Feldes beschreibt, was das Feld tut, damit ich mich
nicht auf ein Verhalten verlasse, das es an dieser Stelle nie gab.

## Ist-Stand am Code

`frontend/src/lib/fieldHelp.ts` (Eintrag `pointsGoalMet`) sagt:

> „Beim Hinzufügen einer Übung steht hier deren Empfehlung – **änderst du sie, bleibt die Änderung**;
> bereits angelegte Positionen berührt eine spätere Empfehlung nie."

**Beide Hälften gelten seit `bd6994a` nicht mehr gleichzeitig.** Der Reviewer hat den Reset
(`setPunkteGetippt(false)` beim Anlegen) ausdrücklich **mit der ersten Hälfte begründet** — und genau
dieser Reset macht die zweite Hälfte über die Positionsgrenze hinweg unwahr: Die Änderung bleibt
*innerhalb* einer Position, nicht darüber hinaus.

**Und eine zweite Ungenauigkeit, älter:** Derselbe Hilfetext wird über `topic="pointsGoalMet"` auch im
**Assistenten** gerendert (`VaterWizard.tsx`). Dort trifft „Beim Hinzufügen einer Übung steht hier deren
Empfehlung" **nie** zu — der Assistent schickt immer seine Intensitäts-Zahl und erbt nichts. Das ist
Altbestand aus B-45 (der Vorbehalt wurde damals nur bei `exercisePoints` nachgetragen) und hängt an der
offenen Frage [B-186](B-186-assistent-und-planseite-setzen-zielpunkte-verschieden.md).

## Die echte Lücke

Ein Hilfetext, der an zwei Orten gerendert wird und nur an einem stimmt — plus ein Satz, den eine spätere
Reparatur zur Hälfte aufgehoben hat, ohne ihn anzufassen.

## Offene Punkte

1. Nur die zweite Hälfte präzisieren, oder den Text je Ort trennen? Empfehlung: **erst B-186 klären** —
   solange offen ist, ob der Assistent überhaupt erben soll, wäre eine getrennte Fassung eine Antwort auf
   eine Frage, die niemand entschieden hat.
2. Trägt `HelpTopic` überhaupt zwei Fassungen desselben Begriffs? `fieldHelp.ts` ist ausdrücklich gebaut,
   damit **eine** Formulierung je Begriff existiert — zwei Fassungen wären ein Bruch mit seiner
   Begründung. Nachsehen, nicht vermuten.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 2. `entgangen_bei: [B-191]` für die erste
  Hälfte — sie wurde durch jene Story unwahr. Die zweite Hälfte ist älter und gehört zu B-186.
