---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Keine Arten nur nach Ladung, B-174 Entscheidung 3]
status: idee
prio: P3
art: Frage
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: B-199, Fall 3 (Nachtlauf 2026-10-17, zweiter Lauf)
unverifiziert: true
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

## Verlauf

- **2026-10-18** — angelegt aus B-199, Fall 3, im zweiten Lauf des Nachtlaufs 2026-10-17: der einzige der drei
  Fälle, der eine Entscheidung des Menschen braucht (Freigabe 1).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): der dritte Zustand wird **nachgebaut**, wie B-174 Entscheidung 3 versprochen hat („wird geladen…“, „Keine Arten“ nur nach erfolgreichem Laden). **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben, README):** ein Nachtlauf darf diese Story auf Grundlage der getroffenen Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-14); trifft er dabei auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an und notiert sie. Rollengang und Reviewer bleiben Pflicht.
