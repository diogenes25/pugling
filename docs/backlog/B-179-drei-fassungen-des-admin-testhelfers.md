---
tags: [typ/story, status/abgenommen, bereich/tests]
aliases: [Admin-Helfer dreimal, Token nach dem Flag]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
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
Klassen-DB — im Bauen von B-178 hat genau das `Arten_EinesSeedFachs_SindFuerJedenGesperrt` rot gemacht
(der Fall heißt seit B-170 `Geseedete_Arten_Sind_Fuer_Jeden_Gesperrt`; der alte Name behauptete eine Regel,
die B-170 aufgehoben hat). Ein
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

## User Story

Als **Entwickler, der den nächsten Admin-Test schreibt**, möchte ich einen Helfer rufen, der den tragenden
Merksatz (Flag **vor** dem Login) selbst einhält, statt ihn aus dem Kommentar einer fremden Testdatei zu
lernen – oder nicht zu lernen und ein grundloses `403` zu sehen.

## Akzeptanzkriterien

1. `TestApi` trägt `AdminAdultAsync(factory, pin)` (frischer Erwachsener, geflaggt, dann angemeldet) und
   `MakeAdmin(factory, adultId)` (nur das Flag, für einen bestehenden Erwachsenen); der Merksatz steht
   **einmal**, am Helfer.
2. Die drei bisherigen Fassungen (`FachEigentumTests`, `VerlagLoeschenSperreTests`, `ExerciseGrantsTests`)
   sind entfernt, ihre fünf Aufrufstellen rufen `TestApi`.
3. Die Suite bleibt so grün wie vorher (Abnahmeform `Aufräumen`).

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Aufräumen`).

1. **Nach `TestApi`** (Punkt 1), neben `AdultAsync`/`ChildAsync`. **Kosten:** `TestApi` wächst um zwei
   Funktionen; die Datei ist ohnehin der Ort der geteilten Primitive.
2. **Zwei Funktionen, kein Schalter** (Punkt 2): `AdminAdultAsync` für den Normalfall, `MakeAdmin` für
   `ExerciseGrantsTests`, das einen *bestimmten* Erwachsenen flaggt. `AdminAdultAsync` ruft `MakeAdmin` –
   der Merksatz steht an `MakeAdmin`, `AdminAdultAsync` verweist. **Kosten:** keine.
3. **Alle drei Aufrufer in einem Zug** (Punkt 3): fünf Aufrufstellen (`FachEigentumTests.cs:380/554`,
   `VerlagLoeschenSperreTests.cs:143`, `ExerciseGrantsTests.cs:199/227`). Die nun unbenutzten `using`s in
   `FachEigentumTests` (DI, `Pugling.Api.Data`) fallen weg; in `VerlagLoeschenSperreTests` bleiben sie, dort
   gibt es weitere DB-Zugriffe. **Kosten:** keine.
4. **Keine rote Probe** – `Aufräumen`: kein Verhalten ändert sich, die Suite ist der Beleg. Der Merksatz
   selbst („Flag vor Login") ist in `ExerciseGrantsTests` ohnehin gelebt (Flag, *dann* `AdultAsync`).

## Schätzung

**Größe XS** (Anker B-02). `wo: backend` (Tests), keine Migration, kein Vertrag.

**Angriffsplan:** Helfer in `TestApi` → drei Fassungen entfernen, fünf Aufrufe umstellen → Build → Suite →
`pugling-reviewer` → Commit (gemeinsam mit B-158).

**Testweg:** volle Suite (877 + 2 neue aus B-158); Rollen als Regressionszeugen.

## Verlauf

- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 3). `pugling-reviewer`: Semantik aller drei Fassungen
  erhalten (frischer Adult, Flag per DB, **dann** Login; `VerlagLoeschenSperreTests` von async auf sync
  DB-Zugriff – harmlos, wie `SeedLeitnerPosition`), kein neues Leck in der geteilten Klassen-DB, verbleibende
  `using`s gebraucht; **ein Fund mit Codewirkung (Zähler 1 des Sprints):** der `/// <summary>`-Block des
  entfernten `AdminCreatorAsync` stand verwaist über dem nächsten Test (`FachEigentumTests.cs:325-334`,
  Roslyn hängt ihn als Trivia an, kein Tor meldet es) – gelöscht. Suite **879/879**. **Rollengang:** Rollen
  als Regressionszeugen (reine Test-Änderung). Commit: siehe Nachtrag.
- **2026-10-23** — **gebaut** (Nachtlauf, Sprint 3): `TestApi.MakeAdmin` + `TestApi.AdminAdultAsync`, drei
  private Fassungen entfernt, fünf Aufrufe umgestellt, zwei `using`s in `FachEigentumTests` entfernt.
- **2026-10-23** — gegrillt und geschätzt im Nachtlauf (Freigabe 1); User Story und AK nachgetragen.
- 2026-08-14 · Aufgenommen aus dem `pugling-reviewer` zu Sprint 2 des Nachtlaufs. `ausformuliert` direkt: die
  drei Fundorte sind benannt, und der zweite Fallstrick ist in diesem Lauf **gemessen** worden, nicht
  vermutet.
