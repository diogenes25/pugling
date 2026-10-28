---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Deaktivierter Plan zeigt englische Serverzeile]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf I, Teil 2 in Lauf J (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-326]
wartet_auf: ""
---

# B-330 · Ein deaktivierter Plan zeigt dem Kind nach einem Reload die englische Serverzeile

[B-326](B-326-ueben-und-klausur-nehmen-ungepruefte-plan-id.md) nennt in seiner Problembeschreibung „Löschen **oder
Deaktivieren**“ und in der User Story „wenn mein Plan inzwischen weg ist“, behandelt aber nur `404`. Ein deaktivierter
Plan antwortet `403 plan_inactive` (`PositionPracticeController.cs:71` u. a., `PositionTestsController.cs:138` u. a.).
Der Code steht weder in `GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts`) noch in `PROBLEM_KEYS`
(`frontend/src/lib/chromeText.tsx`) – außer in `contract.ts` kommt er im Frontend nirgends vor (`grep`, Lauf J).

**Szenario** (aus Code gelesen): der Vater deaktiviert den Plan, das Kind lädt `/sohn/practice/<id>` neu → die deutsche
Arcade zeigt „This study plan is not currently active. Ask your parent. (Ref: …)“ – genau das Bild, das B-326 beheben
sollte.

Zu klären beim Ausformulieren: deutsche/englische Fassung für `plan_inactive` (Vater-Web und Arcade) **oder** wie bei
404 zur Basis umleiten; vermutlich beides nötig, weil `plan_inactive` auch mitten im Spiel kommen kann.

Mitgenommene 🟢-Hinweise derselben Nachschau (ohne Zählung, beim Ausformulieren mit prüfen):
`queryByText(/Study plan not found/)` in `SohnTest.test.tsx`/`SohnPractice.test.tsx:110` kann allein nie fallen
(Absichtserklärung beschriften oder streichen); der 404-Fang in `SohnTest.tsx:55-60` umfasst auch `submitTest`/`overview`.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show b64006c`) in Lauf J; Fehlen von
  `plan_inactive` im Frontend per `grep` bestätigt, Szenario nicht gemessen.
