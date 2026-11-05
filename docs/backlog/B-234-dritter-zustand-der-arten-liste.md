---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Keine Arten nur nach Ladung, B-174 Entscheidung 3]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-199, Fall 3 (Nachtlauf 2026-10-17, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-234 · Soll die Arten-Liste einen dritten Zustand bekommen – oder B-174s Entscheidung 3 zurückgenommen werden?

B-174 versprach in Entscheidung 3: „Der Leer-Zweig bleibt, wird aber nachgeschärft – ‚Keine Arten' darf nur
greifen, wenn wirklich geladen wurde", samt angekündigtem Preis „ein dritter sichtbarer Zustand". Gebaut wurde
nur der Ausfall-Banner; `CategoryRows` (`frontend/src/vater/CatalogAdmin.tsx:210`) steht unverändert auf
`if (!categories?.length) return null`. Das Akzeptanzkriterium hält trotzdem – über die Überschriftenzahl
(`Arten (n)` erscheint bei `error` nicht), die die Story nirgends als tragendes Merkmal nennt. Der Verlauf von
B-174 protokolliert die Abweichung von Entscheidung 1 ausdrücklich, die von Entscheidung 3 gar nicht.

Zwei Antworten, beide Produktentscheidung über einen sichtbaren Zustand: (a) den dritten Zustand nachbauen
(„Arten werden geladen…" / „Keine Arten" nur nach erfolgreicher Ladung), oder (b) Entscheidung 3 in B-174
ausdrücklich zurücknehmen, weil die Überschriftenzahl den Fall schon trägt. Darum eine `Frage` – aus
[B-199](B-199-drei-behauptungen-die-ihr-eigener-sprint-aufgehoben-hat.md) herausgelöst, dessen andere zwei
Fälle Handwerk sind.

## User Story

Als **Creator**, der unter einem Fach Arten anlegt, möchte ich unterscheiden können, ob das Fach keine Arten hat oder ob
sie noch laden, damit ich keine Dublette im geteilten Katalog anlege.

## Ist-Stand am Code (vor dem Bau, Stand `37e6f6e`)

- `frontend/src/vater/CatalogAdmin.tsx:212`: `CategoryRows` gab bei `!categories?.length` `null` zurück – ladend und leer sahen
  gleich aus; die Überschrift zählte nach einem Fachwechsel die gefilterte (leere) Liste als „Arten (0)".

## Entscheidungen

1. **(Nutzer, 2026-10-23)** Der dritte Zustand wird nachgebaut.
2. **(Bau)** `CategorySection` bekommt `pending` = `data === null || stale` (`artenPending`, `CatalogAdmin.tsx:128`). Ladend steht
   „Arten werden geladen…", geladen und leer „Keine Arten."; neben einem Fehler keins von beiden. Die Zählung in der
   Überschrift erscheint nur, wenn die Arten dieses Fachs vorliegen.

## Akzeptanzkriterien

1. Im Ladefenster eines Fachwechsels steht „Arten werden geladen…", weder „Keine Arten." noch eine Zahl.
2. „Keine Arten." steht nur nach einer geglückten Ladung.
3. Neben einem Fehler steht weder „lädt" noch „keine".
4. Die Verdrahtung (`stale`) hat einen eigenen Test.

## Schätzung

`XS`, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg: Vitest am Baustein, Mutant je Fall.

## Verlauf

- **2026-10-18** — angelegt aus B-199, Fall 3, im zweiten Lauf des Nachtlaufs 2026-10-17: der einzige der drei
  Fälle, der eine Entscheidung des Menschen braucht (Freigabe 1).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): der dritte Zustand wird **nachgebaut**, wie B-174 Entscheidung 3 versprochen hat („wird geladen…“, „Keine Arten“ nur nach erfolgreichem Laden). **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben, README):** ein Nachtlauf darf diese Story auf Grundlage der getroffenen Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-14); trifft er dabei auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an und notiert sie. Rollengang und Reviewer bleiben Pflicht.
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Sprint 1 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `CatalogAdmin.test.tsx:300` („sagt im Ladefenster …"), Mutant „`pending` ignorieren" → rot (1 von 20). AK 2 → `:309`, Mutant „Leer-Zweig weg" → rot (1 von 20). AK 3 → `:316` und `:324`, Mutanten „`!error` am Lade- bzw. Leer-Zweig weg" → je rot (1 von 20). AK 4 (Review-Fund 🟡 1, `frontend-reviewer`) → `artenPending`-Fall am Dateiende, Mutant „`|| stale` weg" → rot (1 von 34 in zwei Dateien).
- **2026-11-05** — **abgenommen.** Vitest 648/648, `tsc -b` sauber, E2E 110/110 (volle Suite, vor den Review-Fixes, die nur eine Funktion herauslösen und Tests ergänzen). Review `frontend-reviewer`: zwei 🟡 (gezählt, Zähler Sprint 1: 2), zwei 🟢 (ohne Zählung) – alle behoben. Rollengang: kein eigener E2E-Weg (ein Ladefenster ist im Browser nicht zuverlässig herzustellen ohne künstlich langsamen Server); belegt über die Vitest-Fälle, die Creator-Specs der Lehrwerke- und Katalog-Seiten liefen grün. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `c6876b4`.
