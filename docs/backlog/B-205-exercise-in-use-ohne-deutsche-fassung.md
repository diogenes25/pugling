---
tags: [typ/story, status/idee, bereich/frontend, bereich/vertrag]
aliases: [exercise_in_use englisch, dritter Geschwisterfall, GERMAN_PROBLEM_TEXT unvollstaendig]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer 2026-09-02 zu B-204
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-205 · `exercise_in_use` fehlt in `GERMAN_PROBLEM_TEXT` — die zwei Geschwister stehen drin

Der Vater sieht beim Löschversuch einer benutzten Reihe den englischen Servertext („Content in this series
is still used in a study plan, a class test or an objective milestone; remove it there first."), weil
`exercise_in_use` keine Zeile in `GERMAN_PROBLEM_TEXT` hat (`frontend/src/lib/api.ts:80 ff.`).

Der `frontend-reviewer` hat es beim Review zu [B-204](B-204-vierter-weg-in-den-doppelten-meldungsplatz.md)
gefunden und begründet, warum das kein bewusster Ausschluss ist: Der Code erfüllt beide Hälften des
Aufnahmetests, den die Abbildung sich selbst gibt — der Creator sieht ihn an einem Knopf, den er gedrückt
hat, und kann etwas dagegen tun. Genau darum stehen die zwei Geschwister `subject_in_use` und
`publisher_in_use` bereits drin. Es ist also der dritte Fall derselben Familie, nicht eine i18n-Frage.

**Kopplung, die beim Bauen zählt:** `frontend/e2e/creator-lehrwerk-weg.spec.ts` sichert heute den
**englischen** Text zu (Schritt 6a). Kommt die deutsche Zeile, muss diese Zusicherung mitwandern — der Satz
steht als Kommentar in der Spec.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem `frontend-reviewer`-Lauf zu B-204. `unverifiziert: true` — der Fundort
  ist belegt, aber ob die Abbildung wirklich alle Geschwister aufnehmen *soll* (statt die Familie anders zu
  schneiden), ist beim Ausformulieren gegen den Code zu prüfen.
