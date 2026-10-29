---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [SohnTest.start merkt sich plan_inactive nicht]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf J, Teil 2 in Lauf K (docs/pm-sitzung-2026-10-29-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-330]
wartet_auf: ""
---

# B-335 · Scheitert der erste Schritt nach dem Klausurstart an `plan_inactive`, bietet die Arcade „Weiter versuchen" an

`SohnTest.tsx:83-88`: `start()` ist der einzige Fang, der `planStopped` nicht setzt. Gelingt `startTest` (die
`attemptId` steht), antwortet aber der unmittelbar folgende `nextTest` bzw. `submitTest` mit `403 plan_inactive`, zeigt
die Arcade „Weiter versuchen" – und das holt nur denselben `403`. Das Fenster ist klein (Deaktivierung zwischen zwei
direkt aufeinanderfolgenden Aufrufen, oder die UTC-Tagesgrenze am `EndDate`). Aus Code gelesen (🟢 der Nachschau), nicht
gemessen. Vermutete Behebung: `setPlanStopped(isPlanInactive(e))` auch dort, mit einem Fall, der `nextTest` nach
erfolgreichem Start scheitern lässt.

## Verlauf

- **2026-10-29** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show 02ce632`) in Lauf K; 🟢, ohne
  Zählung, zurückgestellt (nicht billig: braucht einen eigenen Fall mit gestaffelter Mock-Antwort).
