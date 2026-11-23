---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Aufbau-Weg im Editor, Vorstufe von]
status: abgenommen
prio: P1
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-704
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-840 · Übungseditor: eine Übung als Vorstufe einer anderen derselben Unit markieren

Teil von [B-704](B-704-aufbau-weg-in-der-oberflaeche.md) (geteilt). Die Oberfläche zu
[B-700](B-700-aufbau-stufe-am-katalog.md): jeder Backend-Endpunkt braucht ein UI, und der Vollersatz-PUT des Editors
hätte ohne die Felder jede Vorstufe beim nächsten Speichern gelöscht.

## User Story

Als *Creator* möchte ich im Übungseditor eine Übung als Vorstufe einer anderen markieren, damit der Lernweg im Katalog
steht und nicht nur in meinem Kopf.

## Entscheidungen

Geerbt aus B-704, dort am 2026-11-19 bestätigt: **Entscheidung 2** – zwei Felder, „Vorstufe von" (Auswahl der
Ziel-Übungen derselben Unit) und „Stufe" (1 oder 2), spiegelt B-700 1:1.

Beim Bauen ohne Produktfolge festgelegt (Darstellung, keine neue Regel):

- Ist die Übung selbst Ziel von Vorstufen, zeigt der Abschnitt das als Satz statt einer Auswahl, die der Server mit 400
  ablehnen würde (B-700 Entscheidung 2: eine Ebene tief).
- Wählt der Creator ein Ziel, beginnt „Stufe" bei 1; ohne Ziel ist „Stufe" gesperrt. Ein halbes Paar geht als „keine
  Vorstufe" hinaus statt als sicherer 400.

## Akzeptanzkriterien

1. (B-704 AK 1) Eine Übung lässt sich im Editor als Vorstufe einer anderen derselben Unit mit einer Stufe speichern; nach
   dem Neuöffnen zeigt der Editor beide Werte, und ein Speichern ohne Änderung lässt sie stehen.
2. (B-704 AK 2) „Vorstufe von" bietet nur Übungen der eigenen Unit an, nicht die Übung selbst und keine Vorstufe.
3. (B-704 AK 7) Die Felder sind per Tastatur erreichbar und beschriftet; der Abschnitt hat eine Feld-Erklärung.

## Schätzung

**Größe: S** – ein Formularabschnitt, eine reine Auswahlregel, die Durchreichung im Vollersatz. Kein neuer Endpunkt
(die Felder kamen mit B-700 auf `ExerciseSummary`/`ExerciseDetail`).

## Verlauf

- **2026-11-22** — Aus B-704 geteilt und im selben Sprint wie B-700 gebaut (Nachtlauf 2026-11-22-c, Opus).
- **2026-11-22** — Verlauf je AK-Fall: AK 1 Rundlauf → `frontend/src/vater/ExerciseEditModal.test.ts:94` (F1 rot), halbes Paar →
  `:107` (F2, F3 rot), Neuöffnen und Speichern im echten Dialog → `frontend/e2e/aufbau-weg-editor.spec.ts:31`
  (1 passed). AK 2 → `frontend/src/vater/BuildPathField.test.ts:18` (F5 rot), `:23` (F4 rot), `:28` (F6 rot). AK 7: native
  `<select>` mit `<label htmlFor>`, Gruppe mit `FieldLabel topic="exerciseBuildPath"` (Review bestätigt). Vitest
  1356/1356, `tsc -b` sauber.
- **2026-11-22** — Review `frontend-reviewer`: kein 🔴/🟡. 🟢 ohne Zählung, behoben: Platzhalter „– erst Ziel wählen –";
  Hinweis, wenn eine Unit mehr Übungen hat als geladen. 🟢 offen: Komponententest der Wechsel-Semantik (Ziel wählen setzt
  Stufe 1), E2E mit zwei Speichern im selben Dialog.
- **2026-11-22** — `abgenommen` (Nachtlauf 2026-11-22-c): Commit `e36a5c79`. Creator abgenommen im Maschinengang; Vater und Sohn als Regressionszeugen (Suite grün, kein Pfad ihrer Ebenen im Diff).
- **2026-11-23** — nachgeschaut (Retro Sprint 117, `pugling-reviewer`, nur gelesen). Prüfpunkte: Feldnamen
  `buildLevel`/`buildTargetExerciseId` in `CreateExercisePayload` gleich dem DTO; einziger PUT-Aufrufer
  `ExerciseEditModal.tsx:150` schickt beide aus dem Formular (`:271-272`), halbes Paar → null/null, `ExerciseEditModal.test.ts`
  fällt ohne die Zeilen; `buildTargetOptions` je Regel ein Fall; AK 1 trägt `aufbau-weg-editor.spec.ts` mit Rücklesen über
  die API. Kein Befund; die zwei 🟢 des Abnahme-Reviews (kein Komponententest „Ziel wählen setzt Stufe 1“, kein doppeltes
  Speichern im selben Dialog) bleiben offen, sie sind Testlücken ohne Fehlverhalten.
