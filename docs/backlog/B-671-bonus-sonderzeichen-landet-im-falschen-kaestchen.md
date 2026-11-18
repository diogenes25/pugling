---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Bonusrunde Sonderzeichen falsches Kästchen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von B-661 (96a3491d, frontend-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-661]
---

# B-671 · In der Bonusrunde schreibt die Sonderzeichen-Leiste ins Kästchen der vorigen Karte

Aus der Nachschau von B-661, am Code gelesen, nicht im Browser nachgestellt: `LetterBoxes` merkt sich das zuletzt
fokussierte Kästchen (`frontend/src/components/LetterBoxes.tsx:83-84`) und nutzt es, solange es kleiner als die Länge ist.
Üben und Test setzen den Fokus bei jedem Kartenwechsel aufs erste Kästchen (`SohnPractice.tsx:198`, `SohnTest.tsx:93`),
die Bonusrunde nur bei einem Fehler (`SohnBonus.tsx:181`). Zwei Kästchen-Karten nacheinander, auf der ersten Kästchen 5
gefüllt und per Knopf abgegeben ⇒ das erste „é“ der nächsten Karte landet in Kästchen 5 statt 1.

## Naheliegend

Das gemerkte Kästchen beim Kartenwechsel zurücksetzen (in `LetterBoxes`, an der Senke) statt den Fokus je Aufrufer zu
setzen.
