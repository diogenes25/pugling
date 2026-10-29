---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator, rolle/student]
aliases: [Refs-only-PUT ohne Fall]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf J, Teil 1 (docs/pm-sitzung-2026-10-28-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-323]
wartet_auf: ""
nachgeschaut: "2026-10-29"
---

# B-332 · Ein `PUT` nur mit `refs`, der ein gespieltes Wort streicht, ist von keinem Fall gehalten

## Ist-Stand (Lauf J, am Code belegt)

[B-323](B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md) sperrt den ganzen `PUT` einer gespielten Vokabelübung. Der
Code prüft auch einen `PUT`, der nur `refs` trägt (`ExerciseControllers.cs` `UpdateConflictAsync` über
`HasPayloadItems`) – aber kein Test hält das: `refsWin` schickt zusätzlich `items` mit (die Prüfung liefe also auch ohne
Refs-Zweig), `refsAppend` erwartet `200`, das ohne Prüfung genauso käme. Die Mutante „nur `items` prüfen“
(`config.Items.Count == 0` statt `!HasPayloadItems`) blieb grün. Ausgerechnet der Refs-only-`PUT` ist der naheliegende
Weg eines API-Clients oder KI-Agenten.

## User Story

Wie B-323: Als Creator, der eine gespielte Vokabelübung über die API pflegt, möchte ich, dass auch ein `PUT` nur mit
`refs` den Lernstand nicht verschieben kann – und dass ein Test das hält.

## Entscheidungen (autonom, Freigabe 1 – `Defekt`)

1. Nur ein Fall, kein Code: das Verhalten ist richtig, es fehlte der Beleg. *Kosten:* keine.

## Akzeptanzkriterien

1. Ein Fall schickt `refs` ohne `items`, die ein gespieltes Wort streichen, und erwartet `409`; die Mutante „nur `items`
   prüfen“ macht ihn rot.

## Schätzung

`XS`, Backend-Test. Testweg: `ExerciseItemsAndProgressTests`, Mutante, volle Suite.

## Verlauf

- **2026-10-29** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show 1a3a73f`) in Lauf J, im selben
  Sprint behoben: Fall `refsOnlyDrop` ergänzt; gegen den Ausgangscode grün (Verhalten war richtig), Mutante „nur `items`
  prüfen“ **1 failed** an Z. 365 (erwartet `Conflict`, gemessen `OK`).
- **2026-10-29** — Delta-Review (`pugling-reviewer`, `git show 599e217`): Fall trifft nur den Refs-Zweig von
  `HasPayloadItems`, kein anderer Weg im `PUT` liefert `409`; Doku-Sätze gegen `ExerciseControllers.cs:123` und `:233-239`
  bestätigt. 🟢 Code und unveränderte Items zusichern → übernommen. Volle Suite **1004/1004**. Rollengang: reine
  Testergänzung ohne Laufzeitwirkung – der Weg selbst ist B-323s Live-Probe (Creator, `409` beim `PUT`); E2E aus Teil 2
  (**99 passed**) unberührt. **Abgenommen**, Commits `599e217` und der Abschluss-Commit von Lauf J.
- **2026-10-29** — Nachschau in Lauf K (frischer `pugling-reviewer`, `git show 599e217` und die Nachträge in `2a470be`):
  Mutante „nur `items` prüfen" (`UpdateConflictAsync`) nachgerechnet – Refs-only-`PUT` bliebe ungeprüft, Z. 365 fiele;
  „Refs beim Ermitteln ignoriert" hält `refsWin` (Z. 318-334); leere `refs` reconcilen nicht (`AfterSaveAsync` nutzt
  dasselbe `HasPayloadItems`). Doku-Sätze in `wiki/03-uebungstypen.md` und `docs/endpunkt-beziehungen.md:87` gegen
  `RefsFromTags`/`PatchItem` geprüft: stimmen. 🟢 Titel und AK sagen „gespieltes Wort"; die Sperre greift schon, wenn die
  Übung in einem Plan steht (`ExerciseInPlanAsync`, der Test legt nur eine Position an) – sachlich richtig, nur zu eng
  formuliert; hier benannt statt umbenannt.
