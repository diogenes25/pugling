---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/supervisor]
aliases: [Assistent Dosis Liste, wizardFinish Dosis]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 29/30)
entgangen_bei: [B-04]
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-477 · Der Assistent schickt die Tagesdosis auch an ungeordnete Listen – das Anlegen bricht mittendrin ab

Gefunden in der Nachschau von Sprint 29 (B-04), gelesen, nicht im Browser nachgestellt: Der Plan-Assistent setzt
`useLeitner = true` und `newItemsPerDay = 10` als Vorgabe und schickt beides an **jede** gewählte Übung
(`VaterWizard.tsx:373`, Schleife in `wizardFinish.ts`). Für eine Listen-Übung mit `ordered: false` lehnt der Server
die Dosis seit B-04 (Entscheidung 9) mit 400 ab (`DoseOnSetProblem`). `runWizardFinish` bricht dann mit „… Der Plan
ist angelegt – nimm die Übung ab und versuche es erneut" ab: der Plan steht halb, und die Meldung nennt nicht den
eigentlichen Ausweg (Dosis 0). Der Test `Dosis_AufUngeordneterListe` prüft nur die nackte API.

Naheliegend: im Assistenten für Übungen, die als Menge gewertet werden, `newItemsPerDay: 0` schicken (Typ- und
Config-Prüfung wie die vorhandene `typeOf`-Prüfung für `stage`).

## User Story

Als **Vater** möchte ich im Plan-Assistenten jede Übung wählen können, auch eine Liste, **damit** der Plan in einem
Zug entsteht und nicht halb angelegt mit einer Fehlermeldung stehen bleibt.

## Ist-Stand am Code

- `VaterWizard.tsx:373`: `newItemsPerDay: useLeitner ? Math.max(0, newItemsPerDay) : 0` für **alle** Positionen.
- `wizardFinish.ts`: die Schleife filtert nur `stage` nach Typ (`typeOf(id) === "Vocabulary"`), nicht die Dosis.
- Server `PlanPositionsController.DoseOnSetProblem`: 400, wenn `GradesAsSet` – das gilt nur für `List` mit
  `ordered: false` (`BuiltInExerciseTypes.cs:380`).
- Der Assistent kennt je Übung nur die `ExerciseSummary` (Typ, Titel …), nicht die Config – ob eine Liste geordnet
  ist, sieht er nicht.

## Die echte Lücke

Eine Einstellung „für alle" trifft eine Übung, für die der Server sie verbietet; der Client filtert sie nicht.

## Offene Punkte

1. ~~Filtern im Client oder Wissen vom Server?~~ → Entscheidung 1.

## Entscheidungen

1. **Der Assistent schickt jeder `List` die Dosis 0** – im selben Muster wie die Stufe, die nur Vokabeln bekommen.
   Begründung: die Übersicht kennt `ordered` nicht; ein neues Vertragsfeld nur für diese Weiche wäre teurer als der
   Verlust. Kosten: eine *geordnete* Liste bekommt im Assistenten keine Dosis; auf der Plan-Seite ist sie einstellbar.

## Akzeptanzkriterien

1. Der Abschluss schickt einer `List` `newItemsPerDay: 0`, einer Vokabelübung daneben die eingestellte Dosis.
2. Alles andere aus dem Feinschliff (z. B. `useLeitner`) bleibt für die Liste unverändert.

## Schätzung

- **XS**, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg: `wizardFinish.test.ts` (Vitest).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 29/30 im Backlog-Lauf, `pugling-reviewer`).
- **2026-11-12** — ausformuliert, gegrillt, geschätzt (Defekt, autonom; die Schätzung kam erst nach der roten Probe auf
  Papier – der Fix war XS und offensichtlich, die Reihenfolge trotzdem notiert).
- **2026-11-12** — in-arbeit. Rote Probe `schickt keine Tagesdosis an Listen`: erwartet 0, gemessen 10 an der
  Listen-Zeile (die Vokabel-Zeile war schon grün – sie belegt nur, dass der Filter nicht alles trifft). Fix in
  `wizardFinish.ts`. Mutante `B477-Liste-mit-Dosis` (Weiche auf einen Typ, den es nicht gibt) → rot.
- **2026-11-12** — `frontend-reviewer`: kein 🔴. 🟡 die Oberfläche verschwieg die Ausnahme – das Feld „Neue Inhalte je
  Tag" und der Überblick („höchstens N … je Tag und Übung") galten für Listen nicht mehr. **Fund 1 des Sprints 32
  (zählt)**: Etikett „(nicht für Listen)" wie bei der Test-Stufe, `aria-label` hält den Feldnamen stabil, Überblick
  „(außer Listen)"; belegt in `e2e/assistent.spec.ts`. 🟢 ohne Handlung: der Typname „List" als Literal folgt dem
  vorhandenen `"Vocabulary"`-Gate.
- **2026-11-12** — Mutanten nach der letzten Änderung (`1f537a5 + Arbeitsbaum`, 01:58): Weiche auf einen Typ, den es
  nicht gibt → rot (`wizardFinish.test.ts`); Etikett ohne Hinweis → rot, Überblick ohne Hinweis → rot
  (`assistent.spec.ts`, Feinschliff-Test). Grundlinie nach dem Zurückspielen grün.
- **2026-11-12** — **abgenommen.** Commit `6d885f5`. Volle Suiten nach der letzten Änderung: Backend 1330/1330, Vitest 749/749, E2E 137 passed im zweiten Lauf (der erste: 136 passed, 1 failed in `creator-lehrwerk-weg.spec.ts` – berührt weder Dosis noch Assistent, einzeln und 3× parallel grün, Ursache nicht gemessen). Review-Zähler Sprint 32: 3 von 5. Rollengang (Vater) als E2E: `assistent.spec.ts` fährt den Assistenten bis zum angelegten Plan und liest Etikett und Überblick; der Abschluss mit einer Liste ist im Vitest belegt, nicht im Browser.
