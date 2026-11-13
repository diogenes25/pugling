---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Stilles Weiterrücken bei Netzfehler]
status: idee
prio: P2
art: Defekt
wo: frontend
quelle: "Review zu B-526, Etappe 2 (frontend-reviewer, 2026-11-13) – bestand schon vor B-526"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-532 · Ein Netzfehler beim Bewerten lässt eine getippte Karte still weiterrücken

Scheitert im Üben der Aufruf, der eine Antwort bewertet (Netz weg, Serverfehler außer „Plan nicht mehr dran“),
schluckt `judge()` in `SohnPractice.tsx` den Fehler und rückt zur nächsten Karte. Das Kind hat getippt, bekommt
kein Blatt, keine Punkte und keinen Hinweis; die Karte zählt nicht in die Genauigkeit und ist für den Server nie
beantwortet worden. Der Kommentar dort nennt die Bewertung „idempotent genug“ – das stimmt für den Server, aber nicht
für das, was das Kind sieht. Seit B-526 fällt es stärker auf, weil sonst jede getippte Antwort ein Blatt bekommt.
Vom Reviewer am Code gelesen, nicht nachgestellt. Nicht in B-526 mitgenommen: es bestand vorher, und die Abhilfe
(Blatt mit „noch einmal senden“?) ist eine eigene Entscheidung.

## Verlauf

- 2026-11-13 · `idee` – Fund beim Review von B-526; Prio P2 ist ein Vorschlag.
