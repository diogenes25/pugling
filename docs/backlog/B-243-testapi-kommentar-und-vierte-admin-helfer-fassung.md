---
tags: [typ/story, status/ausformuliert, bereich/tests]
aliases: [AdminAdultAsync-Summary falsche Richtung, RegisterAdminFatherAsync in RemarkTests]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 3, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-179]
wartet_auf: ""
---

# B-243 · B-179 hat den Admin-Helfer geteilt – die Summary behauptet die Gegenrichtung des gemessenen Effekts, und eine vierte Fassung blieb stehen

Nachschau zu Sprint 3 des ersten Laufs vom 2026-10-23 (Commit `98c8431`), frischer Agent, von mir am Code
gegengelesen. B-179 ist in seinen drei Akzeptanzkriterien erfüllt; beide Funde liegen daneben.

## User Story

Als **Entwickler, der den vierten Admin-Test schreibt**, möchte ich, dass der geteilte Helfer die einzige
Fassung ist und seine Summary den Effekt richtig herum beschreibt, damit ich weder eine fünfte Kopie anlege
noch aus der Begründung das Falsche lerne.

## Ist-Stand am Code

1. **Ein Satz, der durch B-179 falsch wurde** – bereits korrigiert in diesem Sprint, hier als Beleg:
   `backend/Pugling.Api.Tests/TestApi.cs:63-65` (`AdminAdultAsync`-Summary) sagte: „flagging the seeded
   adult would … turn every later ‚foreign creator' check of the same class **green** from the wrong end".
   Gemessen (alte Summary in `FachEigentumTests`, Diff von `98c8431`): „Flagging adult 1 makes
   `Geseedete_Arten_Sind_Fuer_Jeden_Gesperrt` **fail**" – der Fall (`FachEigentumTests.cs:305-320`) erwartet
   403, mit Admin-Flag greift das Break-Glass, die Antwort wird 200 und der Test **rot**. Die Summary
   behauptete die Gegenrichtung. Fehlerklasse B-112: ein Satz beim Zusammenziehen umformuliert, den Effekt
   nicht nachgelesen.
2. **Eine vierte private Fassung existiert weiter:** `backend/Pugling.Api.Tests/RemarkTests.cs:659-668`
   `RegisterAdminFatherAsync` setzt `father.IsAdmin = true` per EF (async, ohne Login; der Aufrufer `:764-765`
   meldet danach selbst an). B-179 nannte drei Fassungen; der Grep dahinter (`AdminCreatorAsync|AdminAsync|
   MakeAdmin`) traf diesen Namen nicht. Die eigene Summary (`:650-657`: geteilte Factory, Break-Glass umgeht
   RWX) ist dort wertvoll und bliebe.

## Die echte Lücke

Dieselbe wie bei B-166/B-241: ein Grep, der nicht alles fasst, gilt als Inventar – hier über drei Namen,
die der Autor kannte, statt über das Merkmal (`IsAdmin = true`). Und ein Kommentar, den kein Build liest.

## Akzeptanzkriterien

1. `TestApi.cs:63-65` beschreibt den gemessenen Effekt (403-Fälle werden **rot**) – in `98c8431`s Nachfolger
   bereits so, nachgelesen.
2. `RemarkTests.RegisterAdminFatherAsync` nutzt `TestApi.MakeAdmin` (oder ist durch `RegisterFatherAsync` +
   `TestApi.MakeAdmin` ersetzt); `rg "IsAdmin = true" backend/Pugling.Api.Tests` trifft nur noch `TestApi.cs`.
3. Suite grün, Zahl im Verlauf.

## Offene Punkte

1. Helfer behalten (mit `MakeAdmin` darin) oder ganz auflösen? *Empfehlung: behalten* – die Summary erklärt,
   warum dieser Test einen Admin braucht; das ist lokal richtig.

## Verlauf

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 3 des ersten Laufs (Commit `98c8431`),
  `entgangen_bei: B-179`. Punkt 1 ist im selben Sprint (Sprint 4, zweiter Lauf) korrigiert – ein Satz von
  heute; Punkt 2 bleibt offen, weil er ein Test-Umbau mit eigener roter Zahl ist und der Sprint sich nicht
  erweitert (README: ein Fund beim Bauen wird eine eigene Story).
