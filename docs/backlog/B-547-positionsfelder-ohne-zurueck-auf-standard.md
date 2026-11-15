---
tags: [typ/story, status/abgenommen, bereich/api, bereich/frontend, rolle/supervisor]
aliases: [ClearItemCount, Positionsfelder zurücksetzen]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Review Sprint 74 (pugling-reviewer), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-16]
nachgeschaut: 2026-11-15
---

# B-547 · Vier Positionsfelder kommen nach dem Setzen nicht mehr auf „Standard“ zurück

`PlanPosition.ItemCount` (`null` = alle Inhalte), `GoalThreshold` (`null` = 80 %), `BoxIntervalDays` und
`StageSchedule` (`null` = Methoden-Vorgabe) speichern „Standard“ als `null`. `UpdatePositionDto` kennt dafür keinen
Schalter (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs`, PATCH-`null` heißt „bleibt“): setzt der Vater einmal
`itemCount: 5`, kommt er nie wieder auf „alle“ zurück – das Formular meldet „Gespeichert“, der alte Wert bleibt. B-16
hat das nur für `Stage` gelöst (`ClearStage`); die DTO-Doku behauptete bis zum Review, die übrigen Felder trügen
„inherit“ als eigenen Wert. Bewusst nicht in Sprint 74 mitgenommen: das Sprint-Ziel (Stufe, Inhalte, Tempo im Formular)
ist ohne diese vier erfüllt, und je Feld braucht es einen Schalter nach dem Muster `ClearStage` samt Formularlogik.

## User Story

Als **Vater** möchte ich ein geleertes Feld „Bestehen ab %“ oder „Inhalte“ speichern können und danach wirklich wieder
den Standard haben, damit „Gespeichert“ nicht lügt.

## Ist-Stand am Code

- `PlanPositionsController.cs:265`/`:269`: `ItemCount`/`GoalThreshold` werden nur bei `not null` übernommen; ein
  geleertes Feld schickt `null` (`PlanPositions.tsx:320-321`, `settingsToDto`) und ändert nichts.
- `BoxIntervalDays`/`StageSchedule` (`:273-274`) stehen in keinem Formular (B-16, Entscheidung 2); per API lassen sie
  sich nur überschreiben.

## Entscheidungen

`art: Defekt` – autonom gegrillt (README „Der Backlog-Lauf“).

1. **Zwei Schalter `ClearItemCount`, `ClearGoalThreshold`** in `UpdatePositionDto`, Muster `ClearStage` (Wert, dann
   Schalter). Das Bearbeiten-Formular schickt sie genau dann, wenn das Feld leer ist und die gespeicherte Position einen
   Wert hatte (wie `clearStage`, B-16 AK 4). Begründung: die Konvention aus `backend/CLAUDE.md` (PATCH-Semantik),
   `PatchSemanticsTests` erzwingt je Schalter einen Fall. Kosten: zwei additive Felder.
2. **`BoxIntervalDays`/`StageSchedule`: eine leere Liste heißt „zurück auf die Methoden-Vorgabe“**, wie bei
   `TimeSlots` (`PlanPositionsController.cs:282`). Begründung: kein Formular schickt sie; ein Schalter ohne Oberfläche
   wäre Vorrat, eine leere Liste hat heute keinen Sinn (ein leerer Stufenplan wird ohnehin auf die Vorgabe
   zurückgeführt). Kosten: eine Bedeutung mehr für „leer“, in der DTO-Doku benannt.
3. Die DTO-Doku (`StudyPlanDtos.cs`) nennt danach vier Schalter bzw. Leer-Regeln statt „B-547“.

## Akzeptanzkriterien

1. PATCH `clearItemCount: true` bzw. `clearGoalThreshold: true` setzt das Feld auf `null`; mit Wert und Schalter
   ebenfalls `null`; ohne Schalter bleibt der Wert (`PatchSemanticsTests`, zwei Tabellenzeilen). Rot ohne die
   Controller-Zeilen.
2. PATCH `boxIntervalDays: []` bzw. `stageSchedule: []` setzt `null` (Integrationstest). Rot, wenn die leere Liste
   gespeichert wird.
3. `settingsToUpdateDto` schickt `clearItemCount`/`clearGoalThreshold` genau dann, wenn das Feld leer ist und vorher
   ein Wert gespeichert war; das Anlegen schickt sie nie (Vitest `PlanPositions.test.ts`).

## Schätzung

**Größe S**, `wo: beides`, `migration: nein`, `vertragsbruch: nein` (additive Felder mit Vorgabe `false`).
Risiko: `GoalThreshold`-Prüfung (`ThresholdProblem`) darf einen gleichzeitig gesendeten Wert weiter prüfen – erst
prüfen, dann Wert, dann Schalter. Angriffsplan: DTO + Controller + `PatchSemanticsTests`, Integrationstest AK 2,
Vertrag neu, `PositionSettings` merkt sich die gespeicherten Werte (`itemCountStored`/`goalThresholdStored` wie
`stageStored`), Vitest AK 3. Testweg: Integrationstests, Vitest; kein neuer E2E-Weg (Rollengang: Planbau-Specs grün).

## Verlauf

- **2026-11-14** — angelegt aus dem Backend-Review von Sprint 74.
- **2026-11-14** — ausformuliert, gegrillt (autonom, `Defekt`) und geschätzt (S) im Backlog-Lauf.
- **2026-11-15** — `in-arbeit` → **`abgenommen`** in Sprint 77 des Backlog-Laufs 2026-11-15, Code-Commit `627e081f`.
  Gebaut nach den Entscheidungen 1–3: `ClearItemCount`/`ClearGoalThreshold` (Wert, dann Schalter), leere Liste bei
  `boxIntervalDays`/`stageSchedule` = Methoden-Vorgabe – im PATCH **und beim Anlegen** (Review-Fund), Formular merkt sich
  die gespeicherten Werte. Review-Funde behoben: (1) die Zeile übernahm die Merkwerte nur beim ersten Rendern, ein zweites
  „zurück auf Standard“ in derselben Sitzung schickte keinen Schalter (Komponententest `PlanPositionsStufe.test.tsx`
  „zweimal bearbeiten“); (2) Anlegen speicherte `[]`.
  - AK 1 → `PatchSemanticsTests` (Zeilen `clearItemCount`, `clearGoalThreshold`), Mutanten B547-AK1a/1b → rot.
  - AK 2 → `PositionListenZuruecksetzenTests.Leere_Liste_Setzt_…`, `…Ohne_Angabe_Bleiben_…`, `…Leere_Listen_Beim_Anlegen_…`;
    Mutanten B547-AK2a/2b/2c, R2 (zwei) → rot.
  - AK 3 → `PlanPositions.test.ts` „schickt clearItemCount/clearGoalThreshold …“, „übernimmt den gespeicherten Stand“;
    Mutanten B547-AK3a/3b/3c → rot; Review-Fall → `PlanPositionsStufe.test.tsx`, Mutante F1 → rot.
  - Belege: volle Suite **1670/1670** (Release), Vitest **1093/1093**, `tsc -b` grün. Rollengang Vater als Playwright
    gegen frisch gestartete Server (Ports 5251/5191): `position-zurueck-auf-standard.spec.ts` (setzen, zweiter Durchgang
    leeren, Zeile ohne „Inhalte“) plus `lernkontrolle-sichtbar`, `assistent`, `assistent-klassenarbeit`,
    `planbau-bewertung`, `feldhilfe`, `ziel-vier-wochen` – **13 passed**. Reviewer: `pugling-reviewer`,
    `frontend-reviewer`.
