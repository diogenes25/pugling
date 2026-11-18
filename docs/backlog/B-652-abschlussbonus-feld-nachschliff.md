---
tags: [typ/story, status/idee, bereich/klassenarbeit, rolle/supervisor]
aliases: [Abschlussbonus-Feld, Nacharbeit-Nachschliff]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "PM-Sitzung 2026-11-18, Review Sprint 86 (frontend-reviewer, pugling-reviewer)"
grund: ""
ersetzt_durch: []
---

# B-652 · Das Abschlussbonus-Feld und die beendete Nacharbeit glätten

Vier kleine Reste aus dem Review von Sprint 86 ([B-636](B-636-pflicht-bis-erledigt-mit-abschlussbonus.md),
[B-637](B-637-nacharbeit-vater-sicht-je-aufgabe.md)). Bewusst nicht im Sprint: das Sprint-Ziel hält ohne sie, und
keiner ändert eine Buchung.

## Beobachtung

1. Wechselt der Vater von „täglich, bis erledigt“ auf einen anderen Rhythmus, verschwindet das Bonusfeld, der alte
   Wert wird aber weiter gespeichert (`frontend/src/vater/PlanPositions.tsx`, `withRemediation`). Er zahlt nur bei
   „bis erledigt“ – harmlos, aber unsichtbar gespeichert.
2. Das Emoji in „Abschlussbonus 🪙“ ist Teil des zugänglichen Namens; ein Screenreader liest „Münze“ mit.
3. Ein Bonus über 1000 scheitert erst am Server mit englischem `validation_error`-Text; das Formular hat keine
   eigene Grenze am Speichern-Knopf.
4. Nacharbeiten, die vor B-636 fertig wurden und `Daily` bleiben, zeigen `completedOn: null`, obwohl jede Aufgabe ein
   `doneAt` hat (keine Rückfüllung in der Migration). Nur Anzeige; das Umstellen auf „bis erledigt“ stempelt richtig.

## Offen

Ob (4) eine Rückfüllung wert ist oder die Anzeige „geschafft am“ aus den `doneAt` der Aufgaben abgeleitet wird.
