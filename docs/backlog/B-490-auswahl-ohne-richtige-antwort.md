---
tags: [typ/story, status/idee, bereich/lernen, rolle/student]
aliases: [choices ohne answer, Lese- und Hörverstehen unlösbar]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 38)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-490 · Eine Auswahlfrage, deren Optionen die Antwort nicht enthalten, ist unlösbar

Gefunden bei der Nachschau von Sprint 38 (frischer `pugling-reviewer`, gelesen, nicht gefahren) – dieselbe Klasse wie
B-487, aber ein anderer Typ: `AnswerChecking.ChoicesOf` (`BuiltInExerciseTypes.cs:472`) gibt die `choices` einer Lese-
oder Hörverstehensfrage unverändert weiter. Fehlt die `answer` darunter, kann das Kind die Frage nicht lösen. Nur das
Vater-Web warnt (`exerciseConfig.tsx:149`, `choicesMissAnswer`); Lader (`FIELDS["reading"]` trägt `choices`) und API
lassen es durch. Ob ergänzt (wie B-487) oder abgelehnt wird, ist beim Grillen zu entscheiden.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 38 im Backlog-Lauf).
