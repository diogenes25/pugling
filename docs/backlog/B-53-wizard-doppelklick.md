---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/qualitaet]
aliases: [Wizard-Doppelklick, zwei Kinder zwei Pläne]
status: abgenommen
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

1. ~~Rendert der Regressionstest den Assistenten oder wird `finish()` testbar herausgelöst?~~ →
   **herausgelöst** (siehe Verlauf). Die Ortsregel lautet ohnehin nicht „nur `components/` und `lib/`",
   sondern „der Test liegt beim Geprüften" – `wizardFinish.test.ts` liegt neben `wizardFinish.ts`.

## Verlauf

- **2026-08-01** — angelegt beim Grillen des Testabdeckungs-Pakets; Ist-Stand direkt am Code belegt (der
  Kommentar bei `progress` sagt selbst, wogegen der Ref gebaut wurde – und wogegen eben nicht).
- **2026-08-01** — gebaut, im Durchgang von E5. `finish()` liegt jetzt als `runWizardFinish` in
  [wizardFinish.ts](../../frontend/src/vater/wizardFinish.ts); die drei Schreibzugriffe kommen als Parameter
  (`WizardWriter`), damit der Test ohne `api.ts` und Router läuft. Der `progress`-Ref trägt beide Fälle, und
  der Unterschied steht am Typ: `childId`/`planId`/`positions` die **Wiederaufnahme** (sequenziell),
  `running` den **Wiedereintritt** (nebenläufig) – die Verwechslung dieser zwei war der Grund, warum der Ref
  wie eine Sperre aussah, ohne eine zu sein.
  Rot-Probe zu AK 1: Sperre entfernt → „expected […] to have a length of 1 but got **2**" (zwei Kinder),
  Datei danach byte-gleich zurückgelegt. AK 2 hat einen eigenen Fall (Plan scheitert → zweiter Anlauf legt
  **kein** zweites Kind an und hängt den Plan an `childId` 101) und einen für die Positionen (Abbruch bei
  Nr. 2 → der zweite Anlauf schickt nur noch 12 und 13). AK 3: `disabled={busy}` unverändert. AK 4: die 25
  Playwright-Tests grün, `vater-von-null.spec.ts` darunter.
  **Neu gefunden und benannt:** kein E2E fährt den Assistenten zu Ende – die Verdrahtung des Bildschirms mit
  dem echten `api` hängt an `tsc`. Als [B-58](B-58-assistent-e2e.md) erfasst.
  Belegt: 7 neue Unit-Fälle (48 gesamt grün), 25 E2E grün, `tsc -b` grün.
- **2026-08-01** — **abgenommen**, Commit `7891485` (mit E5). Alle vier Akzeptanzkriterien belegt, die
  Rot-Probe zu AK 1 gemessen. Vom `frontend-reviewer` bestätigt: der frühe Ausstieg `if (progress.running)
  return null` steht **vor** dem `try` und fasst das Flag darum nicht an, und `finish()` lässt `busy` auf dem
  verworfenen Pfad absichtlich stehen – identisch mit dem Verhalten davor.
