---
tags: [typ/story, status/idee, bereich/klassenarbeit, bereich/frontend, rolle/supervisor]
aliases: [Prognose kommt am Klausurtag, keine Prognose vor dem Termin]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-24-l.md#sprint-161--ziel--umfang
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-980 · „keine Prognose“ steht auch an jeder Arbeit, deren Tag noch nicht da ist

Aus dem Review zu [B-746](B-746-klausur-abweichung-fuer-den-vater.md), dort bewusst nicht gebaut: B-746 AK 1 verlangt für
jede Zeile ohne Schnappschuss genau den Text „keine Prognose“.

- Die Spalte „Prognose“ in `frontend/src/vater/VaterClassTests.tsx` (`PrognoseZelle`) sagt „keine Prognose“ für drei
  verschiedene Lagen: der Klausurtag kommt erst (der Normalfall jeder geplanten Arbeit), am Klausurtag hat niemand die App
  benutzt, oder die Arbeit hat kein Fach bzw. das Fach keine Stufe. Für den Vater liest sich der erste Fall wie ein Fehler.
- Möglicher Wortlaut: „kommt am Klausurtag“ für eine geplante Arbeit mit Termin ab heute; der Vergleich bräuchte den Lerntag
  des Servers (deutsche Zeit), nicht die Uhr des Browsers.

## User Story

Als Vater möchte ich an einer geplanten Arbeit sehen, dass die Prognose noch kommt, statt zu glauben, etwas sei
schiefgegangen.

## Verlauf

- **2026-11-24** — angelegt (Quelle: `frontend-reviewer` zu B-746, Nachtlauf L; Fundort am Code geprüft:
  `PrognoseZelle` hat genau einen Zweig ohne Schnappschuss). Wortlaut ist eine Produktentscheidung, darum `Wunsch`.
