---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Verlängerter Plan kostet Malus für abgelaufene Tage]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Review von Sprint 102 (B-672, pugling-reviewer) im Nachtlauf 2026-11-21, docs/pm-sitzung-2026-11-21.md"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-672]
---

# B-783 · Ein verlängerter, schon abgelaufener Plan holt die Tage dazwischen als Pflicht zurück

Das Gegenstück zu B-672 am anderen Ende des Plans. Läuft ein Plan von T bis T+5 und setzt der Vater am Tag T+10 das
Ende auf T+30, waren die Tage T+6 bis T+9 nicht spielbar (`PositionPlayService.PlanPlayableForChild` verlangt
`today <= EndDate`). `ActivatedAt` bleibt aber bei T, also `<= StartDate`, und in `PlanDueForPeriod`
(`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:677-679`) greift die reine Überlappungsregel: Die
Abrechnung bucht Malus für T+6 bis T+9, und `ChildStreakAsync` (`:493`) zählt diese Tage als offene Pflicht und bricht
die Serie.

Am Code nachgelesen (Reviewer und PM), nicht per Test nachgestellt. Schreibstelle ist `StudyPlansController.Update`
(`if (dto.EndDate is not null) plan.EndDate = …`).

Möglicher Ansatz (zu entscheiden): Wird `EndDate` über einen Tag hinaus verlängert, an dem der Plan schon abgelaufen
war, hebt der Controller `ActivatedAt` auf `min(alter Endtag + 1, heute)` an, mit demselben Gate wie B-672
(`plan.Active`). Rote Probe: Plan T..T+5 mit Tagespflicht und Malus, Uhr auf T+10, PATCH `endDate` T+30, Abrechnung –
erwartet kein Malus für T+6..T+9.

## Verlauf

- **2026-11-21** — angelegt aus dem Review von Sprint 102 (🟡, außerhalb des Sprint-Diffs, darum eigene Story und ohne
  Zählung; `entgangen_bei: [B-672]`).
