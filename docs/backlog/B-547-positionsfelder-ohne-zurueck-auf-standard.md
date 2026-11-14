---
tags: [typ/story, status/idee, bereich/api, bereich/frontend, rolle/supervisor]
aliases: [ClearItemCount, Positionsfelder zurücksetzen]
status: idee
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
---

# B-547 · Vier Positionsfelder kommen nach dem Setzen nicht mehr auf „Standard“ zurück

`PlanPosition.ItemCount` (`null` = alle Inhalte), `GoalThreshold` (`null` = 80 %), `BoxIntervalDays` und
`StageSchedule` (`null` = Methoden-Vorgabe) speichern „Standard“ als `null`. `UpdatePositionDto` kennt dafür keinen
Schalter (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs`, PATCH-`null` heißt „bleibt“): setzt der Vater einmal
`itemCount: 5`, kommt er nie wieder auf „alle“ zurück – das Formular meldet „Gespeichert“, der alte Wert bleibt. B-16
hat das nur für `Stage` gelöst (`ClearStage`); die DTO-Doku behauptete bis zum Review, die übrigen Felder trügen
„inherit“ als eigenen Wert. Bewusst nicht in Sprint 74 mitgenommen: das Sprint-Ziel (Stufe, Inhalte, Tempo im Formular)
ist ohne diese vier erfüllt, und je Feld braucht es einen Schalter nach dem Muster `ClearStage` samt Formularlogik.

## Verlauf

- **2026-11-14** — angelegt aus dem Backend-Review von Sprint 74.
