---
tags: [typ/story, status/in-arbeit, bereich/training, rolle/student]
aliases: []
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: "beides"
migration: nein
vertragsbruch: "additiv: PositionStatus.owesDuty (Pflichtfeld)"
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-1022 · Die Arcade zeigt am Sonntag und in einer Pause „noch N Std. · −X Münzen“, obwohl der Server nichts abbucht

Fund des `pugling-reviewer` im Sprint 191 (B-731), nicht mehr behoben (Fehlerzähler). `TodayResponse`
(`ChildTodayController`, `ComputeChildDayAsync`) trägt kein Signal „heute ist kein Pflichttag“; die Arcade
(`frontend/src/sohn/pfad.ts:92`, `SohnHome.tsx`) zeigt darum an einem Sonntag oder Pausentag Frist und Malus jeder offenen
Tagespflicht – den Malus bucht der Server seit B-731 nie. Widerspricht Entscheidung 4 von B-731 und deren Testweg („Sohn-Tag
zeigt keinen offenen Malus“). Reparaturweg laut Review: `DutyDay` an `TodayResponse` aus `ChildDutyDays.IsDutyDay(today)`, die
Arcade blendet Frist und Malus dann aus; `frontend/e2e/pfad.spec.ts:81` braucht dabei eine feste Uhr oder `SundayFree=false`
im E2E-Server, sonst wird sie sonntags rot.

## Entscheidungen

1. Das Signal sitzt je Position (`PositionStatus.OwesDuty`), nicht als `DutyDay` am `TodayResponse`: eine Wochenpflicht in
   einer von einer Pause berührten Woche schuldet nichts, auch wenn heute ein Pflichttag ist – das kann nur die Position
   sagen. Gerechnet mit denselben Toren wie die Abrechnung (`PlanDueForPeriod`, `ChildDutyDays.OwesDuty`, `DutyUntil`).
2. Die Arcade blendet die **ganze** Frist aus, nicht nur den Malus: „noch 3 Std.“ an einem Tag ohne Pflicht ist dieselbe
   falsche Drohung ohne Zahl.
3. Der E2E-Server läuft mit `LearningDay:SundayFree=false` (wie der In-Process-TestServer), damit `pfad.spec.ts` nicht
   sonntags rot wird.

## Akzeptanzkriterien

1. `GET student/children/{id}/today` meldet `owesDuty: false` für eine Tagespflicht am Sonntag und am Pausentag und für eine
   Wochenpflicht in einer Pausenwoche, sonst `true`.
2. Die Arcade zeigt bei `owesDuty: false` weder Frist noch Malus.

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
- **2026-11-26** — `idee` → `in-arbeit` im Merge-Review von M/N/O (Branch `merge-1125`): der Nutzer hat die Behebung der
  Review-Funde angeordnet, der Reparaturweg des Reviews trägt die Entscheidungen (Abweichung 1 oben begründet). Belege:
  - AK 1 → `LerntageTests.Heute_Sagt_Ob_Die_Pflicht_Etwas_Kostet` (5 Zeilen). Rote Probe `OwesDutyOn` liefert immer `true`
    → die drei `false`-Zeilen rot; Probe „nur `IsDutyDay(day)` statt Wochenregel“ → genau die Pausenwochen-Zeile rot.
  - AK 2 → `frontend/src/sohn/pfad.test.ts` „gibt es nicht, wo die Periode nichts schuldet“. Rote Probe `!pos.owesDuty` aus
    `frist()` gestrichen → rot.
  - Commit: `f4df2984`. Offen für `abgenommen`: `pugling-reviewer`/`frontend-reviewer` über den Fix.
  - Verifikation nach der letzten Code-Änderung: Backend-Suite 2607/2607 (Release), `vitest` 1512/1512, `tsc -b` grün, E2E
    `lerntage-und-pause`, `sparziel`, `lob`, `gleichung-loesen`, `pfad`, `bonus-deckel`(+`-voll`), `uebungstypen` 8 passed
    (Ports 5244/5284, Wegwerf-DB).
