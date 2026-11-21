---
tags: [typ/story, status/idee, bereich/a11y, rolle/student]
aliases: [Bonus-Fokus Ergebnis und Aufdecken]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Review von B-671 im Nachtlauf 2026-11-21 (frontend-reviewer), docs/pm-sitzung-2026-11-21.md"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-778 · Die Bonusrunde lässt den Fokus nach „Aufdecken“ und auf dem Ergebnisblatt auf `body` fallen

In der Bonusrunde (`frontend/src/sohn/SohnBonus.tsx`) ersetzt „Aufdecken“ einer Karteikarte den Knopf durch die
Urteils-Knöpfe, und nach der letzten Antwort ersetzt das Ergebnisblatt die Lektion. In beiden Fällen verschwindet das
fokussierte Element, und der Fokus fällt auf `body`. Ein Tastatur- oder Screenreader-Kind muss neu suchen. Der Test
macht beides schon richtig: Er setzt die Absicht in `onReveal` (`SohnTest.tsx:279`, `revealed` in den Abhängigkeiten)
und fokussiert die Überschrift des Ergebnisses (`resultTitleRef`, `SohnTest.tsx:95`). B-671 Entscheidung 4 („wie im
Üben“) ist damit nur für den Kartenwechsel eingelöst. Das war schon vor B-671 so. Bewusst nicht in B-671 mitgenommen,
weil dessen Ziel (das Sonderzeichen landet in der neuen Karte) ohne das erfüllt ist.

## Verlauf

- **2026-11-21** — angelegt aus dem Review von B-671 im Nachtlauf (`idee`).
