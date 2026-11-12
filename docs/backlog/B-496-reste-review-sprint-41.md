---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [no_checkable_content Ausweg, nichts nachzuholen ohne Noten]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 41)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-496 · Zwei Nachbarn derselben Fehlerform außerhalb von Sprint 41

Gefunden im `frontend-reviewer` zu Sprint 41 (🟢, außerhalb des Sprint-Diffs, darum nicht gezählt und nicht gebaut).

1. **Ausweg in einer Sperre:** `frontend/src/vater/ExercisePreviewModal.tsx:46-47` sagt bei `no_checkable_content`
   „Du kannst sie zuweisen, aber nicht durchspielen." – Zuweisen hat eigene Sperren (`exercise_not_executable`,
   `exercise_empty`); die Meldungsregel vom 2026-11-07 verbietet den Ausweg über die eigene Sperre hinaus.
2. **Erfolg ohne Messung:** `frontend/src/vater/VaterClassTests.tsx:259` zeigt „Keine Arbeiten mit Note ≥ … – nichts
   nachzuholen. 👍" auch für ein Kind ganz ohne bewertete Klassenarbeit – dieselbe Form wie B-467 (vertretbar wahr, aber
   als Erfolg gelesen).

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 41, Nachtlauf).
