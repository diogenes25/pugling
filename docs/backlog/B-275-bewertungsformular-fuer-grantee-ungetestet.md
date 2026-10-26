---
tags: [typ/story, status/idee, bereich/frontend, bereich/test, rolle/supervisor]
aliases: [Grantee sieht das Bewertungsformular – ungetestet an der Aufrufstelle]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-12 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-275 · Ob ein Schreib-Grantee das Bewertungsformular sieht, prüft kein Test an der Aufrufstelle

[B-12](B-12-uebungen-kuratieren.md) AK 7 verlangt, dass das Formular dieselbe Grenze zieht wie der Server: Bewerten
darf, wer eine Übung nicht verwaltet (`isOwner`), auch wenn er sie bearbeiten darf (`isOwn`, Schreib-Grant). Seit Lauf B
steht die Regel im Helfer `canRate`, und Vitest belegt den Grantee-Fall. Die **Aufrufstelle**
(`frontend/src/vater/ExerciseRatingPanel.tsx`, `!canRate(exercise)`) prüft aber nichts. Eine Vertauschung dort
(`exercise.isOwn ? … : …` oder `canRate({ isOwner: exercise.isOwn })`) bleibt grün, weil die E2E-Spec keinen Grantee
fährt: Autorin und Vater haben jeweils beide Felder gleich belegt.

Vorschlag des Reviewers: In `e2e/uebungen-bewerten.spec.ts` gibt die Autorin dem Kollegen einen Schreib-Grant, der
Kollege meldet sich an, und das Formular „Bewertung speichern“ muss sichtbar sein.

**In Lauf B bewusst nicht behoben:** Der geplante Teil von Fund 6 (Helfer, Vitest, rote Probe `isOwn`) ist gebaut. Die
Aufrufstelle ist ein neuer 🟢-Hinweis, und der Zähler stand bei 4 vor dem Delta-Review.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-12 in Lauf B ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Hinweis 2.
