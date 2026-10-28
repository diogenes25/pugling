---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Study plan not found, veraltete Plan-Id im Trophäenweg]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (dritter Lauf, Rollengang Sprint 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-10-28"
entgangen_bei: []
---

# B-254 · Der Trophäenweg zeigt dem Kind „Study plan not found." statt einer Erklärung

Im Rollengang des dritten Nachtlaufs (Wegwerf-DB, Kind #1 ohne Plan, im Browser eine gemerkte Plan-Id
`pugling.plan.1` aus einer älteren DB) zeigte die Startseite richtig „Noch keine Mission", der Tab „Weg"
(`/sohn/progress`) aber die rohe englische Serverantwort „Study plan not found. (Ref: …)". Vermutung, noch
nicht am Code belegt: `SohnProgress` lädt mit der in `localStorage` gemerkten Plan-Id, ohne sie gegen die
Pläne des Kindes zu prüfen – derselbe Zustand entsteht, wenn der Vater den gemerkten Plan löscht. Zu klären:
woher die Startseite ihre richtige Aussage nimmt und warum der Weg sie nicht teilt; ob die gemerkte Id
verworfen werden muss, sobald der Server 404 meldet. Außerhalb des Diffs von Sprint 1 gefunden, darum eigene
Story und kein Zählerfund.

## User Story

Als Kind möchte ich auf dem Tab „Weg“ meinen Fortschritt oder einen Hinweis sehen – nie eine englische Serverzeile, nur
weil sich mein Browser einen Plan merkt, den es nicht mehr gibt.

## Ist-Stand am Code

`frontend/src/sohn/SohnProgress.tsx:12-20` (vor dem Fix) lud `api.overview(planId)` mit der Id aus `localStorage`
(`SohnApp.tsx:46,75`) ungeprüft; die Startseite (`SohnHome.tsx:25-34`) prüft sie gegen `api.plans()`. Der Server
antwortet bei einem gelöschten Plan `404`, bei der Id eines fremden Plans `403` (`PlanOwnershipFilter.cs:24-25`).

## Akzeptanzkriterien

1. Ohne Plan zeigt der Weg den Hinweis zur Basis, ohne den Plan-Endpunkt zu rufen.
2. Mit Plänen zeigt der Weg denselben Plan wie die Startseite (gemerkt, sonst spielbar, sonst erster) und merkt ihn.

## Entscheidungen

1. Eine gemeinsame Auswahl (`useChosenPlan`) für Startseite und Weg statt einer zweiten Regel. Kosten: der Weg lädt
   erst die Planliste, dann den Plan – ein Aufruf mehr.
2. Der Plan-Teil des Wegs ist eine eigene Komponente, erst mit der Wahl gemountet (Review): so beginnt ihr Laden mit
   `loading`, statt einen Render lang den Fehlerkasten zu zeigen.

## Schätzung

S, `frontend`, keine Migration, kein Vertragsbruch. Testweg: Komponententest `SohnProgress.test.tsx`.

## Verlauf

- **2026-10-23** — angelegt im Rollengang von NP-1 (Browser, Freigabe 6); ungeprüft.
- **2026-10-28** — in Lauf H, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) ausformuliert (Ursache am Code bestätigt), geschätzt (S) und gebaut.
  **Gemessen:** zwei neue Fälle → **2 failed** gegen den Ausgangsstand (Zeilen 58 und 67). **Review-Funde 🟡 (zwei,
  gezählt):** (a) nach dem Laden der Planliste blitzte einen Render lang der Fehlerkasten auf (Antwort für „kein Plan“
  als fehlende Daten gelesen) → `ProgressForPlan` erst mit der Wahl gemountet; das Aufblitzen selbst ist einen Render
  lang und hängt an keinem Test, strukturell kann es nicht mehr entstehen; (b) die Testantwort passte nicht zum
  Bildschirm (der Erfolgsweg wäre beim Rendern abgestürzt, ohne dass der Fall es merkte) → vollständige Antwort,
  positive Zusicherung auf den Titel des gewählten Plans. Vitest **555/555**. Rollengang: die volle E2E-Suite gegen frisch gestartete Server (**99 passed**, `exit=0`, nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
- **2026-10-28** — Commit `f96d358` (Lauf H, Teil 2).
- **2026-10-28** — nachgeschaut (Lauf I, frische Reviewer am Commit `f96d358`): Reihenfolge `useChosenPlan.ts:16-18`, Fall 2 kommt nur über die Antwort für Plan 5, `SohnHome` äquivalent umgestellt. Entgangen: [B-324](B-324-gemerkter-plan-zweig-ungetestet.md) (Zweig „gemerkt“ ohne Fall, 🟡); außerhalb der AKs [B-326](B-326-ueben-und-klausur-nehmen-ungepruefte-plan-id.md).
