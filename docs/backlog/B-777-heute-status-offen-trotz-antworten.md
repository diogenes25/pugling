---
tags: [typ/story, status/abgenommen, bereich/auswertung, rolle/supervisor]
aliases: [Status offen trotz Antworten]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 97 im Nachtlauf 2026-11-21 (frontend-reviewer und pugling-reviewer), docs/pm-sitzung-2026-11-21.md"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-749, B-770]
---

# B-777 · Die Vater-Startseite zeigt „offen“ bei einem Kind, das heute schon geantwortet hat

`HeuteKind` (`frontend/src/vater/VaterDashboard.tsx:111-114`) wählt „dran“ über `c.practiced`. Das Backend setzt
`Practiced` als `day.GoalsMet > 0 || points > 0` (`ChildrenDashboardService.cs:33-34`), und Punkte gibt es erst für ein
erreichtes Positionsziel. „Geübt“ heißt damit faktisch „mindestens ein Ziel geschafft“. Ein Kind mit 25 Antworten und noch
keinem vollen Ziel steht auf **„offen“**, während dieselbe Karte darunter „25 Antworten“ zeigt. B-770 hat die
Antwortzahl eingeführt, damit der Vater den Fleiß nicht aus Punkten abliest, und der Status liest die Zahl nicht. Kein
Test fängt das: In `VaterDashboard.test.tsx` haben beide Kinder `practiced: true`.

## User Story

Als Vater möchte ich auf der Startseite „dran“ sehen, sobald mein Kind heute geantwortet hat, damit der Status nicht
dem „25 Antworten“ direkt darunter widerspricht.

## Ist-Stand am Code (2026-11-21 nachgelesen)

- `ChildrenDashboardService.cs:31-34`: `answers` wird schon gezählt (`CountAnswersAsync`), fließt aber nicht in
  `Practiced` ein.
- `ChildDay.Practiced` (`Pugling.Contracts/Supervisor/GoalDtos.cs:41`) hat keine eigene Doku. Der einzige Leser ist
  `VaterDashboard.tsx:113` (gegrept in `backend/Pugling.Api`, `Contracts` und `frontend/src`).
- `ChildrenDashboardAnswersTests` prüft `answersToday`, nicht `practiced`.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-21)

1. **Die Reparatur sitzt im Backend:** `Practiced = GoalsMet > 0 || points > 0 || answers > 0`. Begründung: Das Feld ist
   Vertrag, und API-First heißt, dass jeder Leser (Vater-Web, Agenten) denselben Wert bekommt. Das Frontend bleibt
   unverändert. Kosten: Die Bedeutung des Feldes wird enger an seinen Namen gezogen, Typ und Form bleiben gleich, also
   kein Vertragsbruch.
2. **Die bestehenden Bedingungen bleiben**, einschließlich Punkte ohne Antworten. Begründung: Diese Story repariert die
   fehlende Antwortzahl, sie entscheidet nicht neu, was Punkte bedeuten.
3. Das Feld bekommt im Vertrag eine Zeile Doku, damit ein Agent es nicht erraten muss.

## Akzeptanzkriterien

1. Ein Kind mit Antworten heute, aber ohne erreichtes Ziel und ohne Punkte, hat `practiced: true`. *Rot, wenn die
   Antwortzahl nicht einfließt.*
2. Ein Kind ganz ohne Antworten, Ziele und Punkte hat `practiced: false`. *Rot, wenn `Practiced` immer wahr wird.*

## Angriffsplan

Eine Zeile in `ChildrenDashboardService`, die Doku im Vertrag (die Beispiele und `v1.json` erneuert der Testlauf), zwei
Zusicherungen in `ChildrenDashboardAnswersTests`.

## Schätzung

**XS**, backend. Risiko: `DocsCaptureTests`/OpenAPI ziehen die Doku-Zeile nach und müssen byte-stabil bleiben.

## Verlauf

- **2026-11-21** — angelegt aus der Nachschau von Sprint 97 im Nachtlauf (`idee`, `entgangen_bei: [B-749, B-770]`).
- **2026-11-21** — ausformuliert, gegrillt und geschätzt im Nachtlauf (Sprint 101, autonom, `art: Defekt`):
  - Fundstellen nachgelesen, einziger Leser gegrept, `unverifiziert` entfernt.
  - Drei Entscheidungen: Reparatur im Backend statt im Frontend, bestehende Bedingungen bleiben, Doku.
  - Zwei AK, Größe XS.
- **2026-11-21** — in-arbeit (Sprint 101). `ChildrenDashboardService` nimmt `answers > 0` in `Practiced` auf, die
  Vertragszeile an `ChildDay` ist ergänzt (`v1.json` neu erzeugt).
  **Rote Probe vor dem Fix:** `Geuebt_Heisst_Geantwortet_Auch_Ohne_Ziel_Und_Punkte` erwartet `true`, gemessen `false`
  (:101), 1 failed / 1 passed. Danach 2/2.
  - AK 1, Antworten ohne Ziel und Punkte → derselbe Test (Vorbedingungen `goalsMet` 0 und `pointsToday` 0 zugesichert);
    M8 (`|| answers > 0` gestrichen) → rot.
  - AK 2, ohne jede Aktivität, aber mit leerem Plan → derselbe Test; M9 (`answers >= 0`, also immer wahr) → rot.
  - Rollengang `frontend/e2e/heute-dran-nach-antworten.spec.ts`: Die Pille zeigt „dran“ unter „4 Antworten“ und „0 von 1“.
    M8e (M8, gemessen per E2E) → rot.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M8 | `ChildrenDashboardService.cs:35` | red | red |
  | M9 | `ChildrenDashboardService.cs:35` | red | red |
  | M8e | `ChildrenDashboardService.cs:35`, E2E | red | red |

  Gemessen mit `mutant-probe.py`, Grundlinie jeweils grün danach.
  Review (`pugling-reviewer`): zwei 🟢, Zähler 0 von 5.
  - Die schärfere Vertragszeile, die sich an `AnswersToday` bindet, ist übernommen (ohne Zählung).
  - Der Hinweis, die Vertragsdatei mitzucommitten, ist erledigt.
  - Bekannte Grenze, im Vertrag nicht eigens genannt: Übt der Vater im Plan des Kindes, macht das `practiced` wahr, weil
    `ReviewEvents` keine Akteur-Spalte haben (dieselbe Grenze wie bei `AnswersToday`, B-770).
- **2026-11-21** — abgenommen in Commit `6c128f3a` (Sprint 101). Backend 1938/1938 (Release). Rollengang-E2E 4/4 auf den
  Ausweichports (`heute-dran-nach-antworten`, `vater-schmal`, `full-flow`). Vater abgenommen, Sohn und Creator als
  Regressionszeugen.
