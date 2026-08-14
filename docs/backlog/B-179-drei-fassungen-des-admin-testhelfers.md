---
tags: [typ/story, status/ausformuliert, bereich/tests]
aliases: [Admin-Helfer dreimal, Token nach dem Flag]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu Sprint 2 des Nachtlaufs 2026-08-14
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: ""
---

# B-179 · Drei Fassungen desselben Admin-Testhelfers, und die tragende Regel steht in jeder neu

## Ist-Stand am Code (aus dem Review, Fundorte benannt)

Drei Stellen erzeugen einen Plattform-Admin für einen Test, jede auf ihre Weise:

| Ort | Form |
|---|---|
| `backend/Pugling.Api.Tests/FachEigentumTests.cs` (`AdminCreatorAsync`) | registriert, flaggt, meldet an — async EF |
| `backend/Pugling.Api.Tests/VerlagLoeschenSperreTests.cs:159-176` (`AdminAsync`) | bis auf sync/async identisch |
| `backend/Pugling.Api.Tests/ExerciseGrantsTests.cs:57-63` (`MakeAdmin`) | nur das Flag, ohne Anmeldung |

## Die echte Lücke

Nicht die Dopplung an sich — drei kurze Helfer sind kein Schaden. Die Lücke ist, dass der **tragende
Merksatz in jeder Kopie neu steht**: *Das Rollen-Claim entsteht beim Login aus `adult.IsAdmin`, das Flag muss
also **vor** dem Anmelden gesetzt sein.* Wer die vierte Kopie schreibt und den Satz nicht kennt, bekommt
einen Test, der ohne erkennbaren Grund `403` sieht — und die Ursache steht in einem Kommentar einer fremden
Datei.

Dazu ein zweiter, im Nachtlauf **gemessener** Fallstrick, der bisher nur in einer der drei Fassungen
dokumentiert ist: Wird der **geseedete** Erwachsene geflaggt, überlebt das Flag in der geteilten
Klassen-DB — im Bauen von B-178 hat genau das `Arten_EinesSeedFachs_SindFuerJedenGesperrt` rot gemacht. Ein
gemeinsamer Helfer, der immer einen **frischen** Erwachsenen nimmt, macht den Fallstrick unbetretbar statt
ihn zu beschreiben.

## Offene Punkte

1. Wohin? Empfehlung: `TestApi` — dort liegen die geteilten Test-Primitive (`AdultAsync`, `ChildAsync`,
   `UniqueName`, `IdAsync`), und `Pugling.Api.Tests/CLAUDE.md` behandelt Wegwerf-Bestand ohnehin als
   Disziplinfrage dieser Ebene.
2. Eine Funktion oder zwei? Empfehlung: **zwei** — `TestApi.AdminAdultAsync(factory, pin)` für den
   Normalfall (frisch, geflaggt, angemeldet) und `TestApi.MakeAdmin(factory, id)` für den Fall, dass ein
   *bestehender* Erwachsener das Flag braucht (`ExerciseGrantsTests` braucht genau das). Eine Funktion, die
   beides kann, hätte einen Schalter, und ein Schalter ist hier die Rückkehr der zwei Situationen in einer
   Bedingung.
3. Werden die drei Aufrufer umgestellt? Empfehlung: **ja, in einem Zug** — sonst ist die vierte Fassung der
   gemeinsame Helfer und es gibt vier.

## Verlauf

- 2026-08-14 · Aufgenommen aus dem `pugling-reviewer` zu Sprint 2 des Nachtlaufs. `ausformuliert` direkt: die
  drei Fundorte sind benannt, und der zweite Fallstrick ist in diesem Lauf **gemessen** worden, nicht
  vermutet.
