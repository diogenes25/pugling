---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/qualitaet]
aliases: [Wizard-Doppelklick, zwei Kinder zwei Pläne]
status: ausformuliert
prio: P2
art: Defekt
quelle: docs/testabdeckung-plan.md
---

# B-53 · Zwei Klicks im Lehrplan-Assistenten legen zwei Kinder und zwei Pläne an

Abgespalten von [B-43](B-43-frontend-komponententests.md) beim Grillen des Testabdeckungs-Pakets
([testabdeckung-plan.md](../testabdeckung-plan.md), Entscheidung 6). Dieselbe Fehlerklasse, andere Bauform:
der Assistent benutzt das geteilte Primitiv gar nicht.

## User Story

Als **Vater**, der sich zum ersten Mal durch den Assistenten klickt und beim „Fertig" zweimal erwischt,
möchte ich **ein** Kind und **einen** Lehrplan bekommen – nicht zwei, die ich anschließend von Hand
auseinandersortiere.

## Ist-Stand am Code

- `VaterWizard.tsx:88` hält `busy` als **State** (`useState`), nicht als Ref. Wie überall gilt: `disabled`
  greift erst nach dem Re-Render, zwei Klicks im selben Tick kommen beide durch.
- `finish()` (`:172`) prüft `if (mode === "new" && done.childId == null)` und setzt `done.childId` erst
  **nach** dem `await` (`:186-187`). Danach dasselbe Muster für den Plan: `planId == null` → anlegen →
  `done.planId = planId` (`:191-200`).
- Der Ref existiert bereits – aber für einen anderen Zweck. `VaterWizard.tsx:89-95`: „Was beim Abschluss
  schon geschrieben wurde. Ein Ref, nicht State: es wird mitten in `finish()` gelesen und geschrieben […] der
  laufende Durchgang würde seinen eigenen Fortschritt nicht sehen und Positionen doppelt anlegen." Er sichert
  also die **Wiederaufnahme nach einem Fehler** (sequenziell), nicht den **Wiedereintritt** (nebenläufig).
- Der Assistent ist der Einstiegsweg: `/vater/wizard` legt Kind, Plan und Positionen in einem Zug an.

## Die echte Lücke

Nicht „ein weiterer Doppelklick", sondern der **teuerste** Fall der Klasse – und er liegt außerhalb des
Primitivs, das [B-43](B-43-frontend-komponententests.md) repariert. Ein doppelt abgeschicktes Speichern
erzeugt anderswo eine doppelte Mutation; hier entstehen **zwei Kinder samt zwei Lehrplänen und allen
Positionen**, und das auf dem meistbegangenen Weg eines neuen Vaters. Die Reparatur ist eine andere Bauform:
der `progress`-Ref ist schon da, er muss nur zusätzlich als Wiedereintritts-Sperre dienen.

## Akzeptanzkriterien

1. Ein Test klickt „Fertig" zweimal im selben Tick und weist nach, dass **ein** Kind und **ein** Plan
   entstehen. Er war **vor** der Sperre rot (zwei Kinder) – belegt, nicht behauptet.
2. Die Wiederaufnahme nach einem Fehler bleibt erhalten: bricht der Plan-Aufruf ab, legt der zweite Anlauf
   **kein** zweites Kind an (das kann der `progress`-Ref heute schon, es darf nicht verloren gehen).
3. `disabled={busy}` bleibt – die Sperre ist additiv, nicht ihr Ersatz (dieselbe Auflage wie in E5: die
   Playwright-Actionability hängt daran).
4. Der Vater→Sohn-Durchstich (`vater-von-null.spec.ts`) bleibt grün.

## Offene Punkte

1. Rendert der Regressionstest den Assistenten (er hängt an `api.ts` und am Router) oder wird `finish()`
   testbar herausgelöst? **Empfehlung:** herauslösen – die Grenze aus B-43/Entscheidung 3 („nur
   `components/` und `lib/`") soll nicht gleich wieder eine Ausnahme bekommen.

## Verlauf

- **2026-08-01** — angelegt beim Grillen des Testabdeckungs-Pakets; Ist-Stand direkt am Code belegt (der
  Kommentar bei `progress` sagt selbst, wogegen der Ref gebaut wurde – und wogegen eben nicht).
