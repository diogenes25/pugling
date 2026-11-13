---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Fokus nach Aufdecken]
status: idee
prio: P3
art: Defekt
wo: frontend
quelle: "Review zu B-526, Etappe 2 (frontend-reviewer, 2026-11-13) – bestand schon vor B-526"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-531 · Im Üben fällt der Fokus nach „Aufdecken“ und nach einer Selbsteinschätzung auf `body`

Der Fokus-Effekt in `SohnPractice.tsx` setzt den Fokus nur, wenn die Karte aus dem Blatt oder aus dem Laden kommt.
Drückt das Kind „Aufdecken“, ersetzt die App den fokussierten Knopf durch „Nochmal“ / „Gewusst!“; nach dem Urteil
kommt die nächste Karte. In beiden Fällen verschwindet der fokussierte Knopf, und der Fokus fällt auf `body` – ein
Kind an der Tastatur oder mit Screenreader verliert die Stelle. Dieselbe Fehlerklasse wie B-439, dort für das
Rundenende behoben. Vom Reviewer gelesen, nicht im Browser nachgestellt. Nicht in B-526 mitgenommen, weil es vorher
schon so war und das Ziel jener Story ohne es erfüllt ist.

## Verlauf

- 2026-11-13 · `idee` – Fund beim Review von B-526; Prio P3 ist ein Vorschlag.
