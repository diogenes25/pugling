---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [flackernder E2E, bilder.spec instabil]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Verifikation zu B-210 (2026-09-04)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-213 · `bilder.spec.ts` fällt im vollen Lauf gelegentlich, allein nie

Beim ersten vollen `playwright test` während der Verifikation zu B-210 fiel
`e2e/bilder.spec.ts:77` („Vater bebildert eine Vokabel, der Sohn sieht sein Bild und kann es
wechseln") — **54 passed, 1 failed**. Derselbe Fall allein nachgefahren: grün. Der volle Lauf danach,
unverändert: **56/56 grün**.

Ein Zusammenhang mit dem B-210-Diff ist ausgeschlossen: `bilder.spec.ts` fasst den Bearbeiten-Dialog
nicht an (kein Treffer für „Bearbeiten"/`ed-title`/„Inhalt speichern"), und Playwright fährt die Specs
alphabetisch — `bilder` lief also **vor** der neuen Spec, konnte deren Daten in der geteilten DB gar
nicht sehen.

**Warum das trotzdem eine Story ist:** Ein Testtor, das gelegentlich ohne Grund rot wird, erzieht dazu,
Rot nicht mehr ernst zu nehmen. Genau davon lebt das Tor aber.

## Offene Punkte

1. Woran hängt es? Verdacht auf einen Wartepunkt, der auf einen Zustand statt auf eine Wirkung wartet —
   die Meldung des ersten Laufs wurde nicht festgehalten, es gibt also noch keinen Fundort.
   *Empfehlung: den vollen Lauf mit `--repeat-each` oder mehreren Durchgängen wiederholen und die
   Fehlermeldung sichern, bevor irgendetwas geändert wird.*
2. Ist es die geteilte DB (Reihenfolge-Abhängigkeit) oder eine Zeitfrage in der Spec selbst?

## Verlauf

- **2026-09-04** — aufgenommen aus der Verifikation zu B-210. `unverifiziert: true`, weil genau **eine**
  Beobachtung vorliegt und die zugehörige Fehlermeldung nicht gesichert wurde.
