---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Aufsatz-Vorschau rät zum Zuweisen, Lösch-Frage ungeprüft am Aufrufer, Einzelheiten nach Kinderwechsel]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 45 in der Retro von Sprint 46)
unverifiziert: false
entgangen_bei: [B-496, B-502]
grund: ""
ersetzt_durch: []
---

# B-507 · Nachschau-Reste Sprint 45 (Frontend): ein Ausweg, der stehen blieb, und zwei Tests, die weniger tragen

Nachschau von Sprint 45 (frischer `frontend-reviewer` an `1965741`, gelesen; 11/11 Vitest der unveränderten Dateien).
Alle vier geänderten Sätze stimmen mit dem Server. Ein 🟡, drei 🟢:

1. 🟡 `frontend/src/vater/ExercisePreviewModal.tsx:156`: die „nur ansehen"-Sicht (`gradable === false`, etwa ein Aufsatz)
   sagt „du kannst sie zuweisen, aber nicht durchspielen" – genau der Ausweg, den B-496 110 Zeilen höher entfernt hat
   (Zuweisen hat eigene Sperren, `exercise_not_executable`/`exercise_empty`). Der neue Kommentar `:41-43` zeigt sogar
   hierher. Am Code bestätigt.
2. 🟢 `ExerciseEditModal.test.ts:146-151` prüft `removeItemQuestion`, nicht die Frage am Aufrufer (`:580`); `/Lernstand/`
   träfe auch „der Lernstand bleibt erhalten".
3. 🟢 `VaterLernstand.tsx:94-106`: im Ladefenster nach einem Kinderwechsel stehen die Zeilen des vorigen Kindes bedienbar,
   `openWord` überlebt – „Einzelheiten" öffnet `WordDetail` mit neuem `childId` und alter `vocabularyId` (gegen B-169
   „sperren, nicht ausblenden"). Nur per URL-Wechsel ohne Remount erreichbar.
4. 🟢 `VaterClassTests.test.tsx:16-17` schließt nur 👍 und „nichts nachzuholen" aus; ein anderes Erfolgswort bliebe grün.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 45, Retro Sprint 46). Punkt 1 am Code bestätigt.
