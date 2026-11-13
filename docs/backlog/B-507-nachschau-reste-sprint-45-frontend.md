---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Aufsatz-Vorschau rät zum Zuweisen, Lösch-Frage ungeprüft am Aufrufer, Einzelheiten nach Kinderwechsel]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 45 in der Retro von Sprint 46)
unverifiziert: false
entgangen_bei: [B-496, B-502]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
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

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf 2026-11-12, Nutzerrolle übertragen):

1. Punkt 1: der Satz der „nur ansehen"-Sicht nennt die Folge ohne Ausweg (wie B-496 oben): „Diese Übung lässt sich nicht
   automatisch bewerten – durchspielen lässt sie sich darum nicht."
2. Punkt 2: der Test prüft die Frage dort, wo sie gestellt wird – `confirmAction` am Aufrufer ist eine Fläche; in Vitest
   genügt die Zusicherung auf den genauen Satzteil „geht im Lernstand mit verloren" statt `/Lernstand/` (der Helfer ist die Senke, der
   Aufrufer reicht ihn nur durch – am Code gelesen, `ExerciseEditModal.tsx:580`).
3. Punkt 3 geht in [B-509](B-509-lernstand-kopfzahl-und-zeilen-des-vorigen-kindes.md) auf (dieselbe Stelle).
4. Punkt 4: `VaterClassTests.test.tsx` sichert den genauen neutralen Satz zu, statt zwei Erfolgswörter auszuschließen.

## Akzeptanzkriterien

1. Die „nur ansehen"-Sicht sagt nichts mehr über Zuweisen.
2. Die beiden Tests fallen bei einem anderen Erfolgs- bzw. Bleibt-Satz.

## Schätzung

**Größe: XS** — ein Satz, zwei Tests.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 45, Retro Sprint 46). Punkt 1 am Code bestätigt.
- **2026-11-12** — ausformuliert, autonom gegrillt (Defekt), geschätzt XS (Tageslauf, Sprint 49).
- **2026-11-13** — gebaut (Sprint 49). Punkt 3 in B-509 aufgegangen.
  - AK 1 → `ExercisePreviewModal.test.tsx` `bietet Zuweisen nicht als Ausweg an` (neu); F11 → rot.
  - AK 2 → `nennt den Lernstand, der mitgeht…` auf „geht im Lernstand mit verloren" (F12 → rot);
    `VaterClassTests.test.tsx` auf den genauen neutralen Satz (F13 → rot).
  - Verifikation: Vitest 810/810 (78 Dateien), `tsc -b` sauber; Mutanten-Probe nach der letzten Änderung 17/17 rot je am
    genannten Test (Tabelle im Protokoll `docs/pm-sitzung-2026-11-12-tageslauf.md`, Sprint 49). Playwright 140/140 auf
    Ausweichports (5201/5174) – vor den Review-Nachzügen H1–H4, die tragen Komponententests.
  - Rollengang Vater: E2E-Suite gegen echten Server und echtes UI; Creator, Sohn: Regressionszeugen (Suite grün, kein
    Pfad ihrer Ebenen außer `SohnKonto` unverändert). Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 50/51, frischer `frontend-reviewer` an `8a5f20a`, gelesen, Vitest 45/45 der
  sieben Dateien). Geprüft: Pager-Sprung kann nicht schleifen (`floor((total-1)/take)*take < total`), alle acht Aufrufer
  übergeben einen stabilen Setter, Hook vor dem frühen `return`; je AK von B-509 und den Review-Nachzügen am Code
  nachvollzogen, dass der Test im Ausgangszustand rot wäre; neun Leerfälle auf `total` gelesen. Fund →
  [B-512](B-512-leerfall-waechter-uebersieht-formen.md) (🟡 Wächter übersieht Formen; 🟢 Leerfall-Verlust ungeprüft).
