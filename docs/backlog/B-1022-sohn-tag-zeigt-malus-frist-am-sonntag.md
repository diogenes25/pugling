---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student]
aliases: []
status: abgenommen
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
nachgeschaut: ""
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
- **2026-11-26** — Sprint 192 (`docs/pm-sitzung-2026-11-26.md`): `pugling-reviewer` fand eine Abweichung von der
  Vertragsdoku: `OwesDutyOn` fehlt das Erst-Spiel-Tor der Abrechnung (B-535) – ein nie geöffneter Plan droht „−N Münzen“ an,
  die nie gebucht werden. Gebaut und **zurückgenommen**: mit dem Tor zeigte `e2e/pfad.spec.ts` auf jedem frischen Plan keine
  Frist mehr – das ist eine Produktentscheidung, keine Reparatur → Frage **B-1024**. Ausnahme jetzt in `OwesDutyOn` und in
  der Vertragsdoku (`ProgressDtos.cs`) benannt. Neu belegt: `PlanDueForPeriod` in `owesDuty` →
  `LerntageTests.Mitten_In_Der_Woche_Wieder_Eingeschaltet_Droht_Die_Woche_Nichts_An`, Mutante M2 → rot.
- **2026-11-26** — `in-arbeit` → `abgenommen` (Sprint 192, `docs/pm-sitzung-2026-11-26.md`). Verifikation am Endstand `4e70c583`: Backend-Suite 2609/2609 (Release), `vitest` 1514/1514, `tsc -b` grün, E2E
  `lerntage-und-pause` + `pfad` 2 passed (Ports 5244/5284). Reviews: `pugling-reviewer` (zweimal, zuletzt ohne Fund),
  `frontend-reviewer` (zwei Funde, behoben). Rollengang an einer Kopie der echten DB (`kopie-server.sh`, `:5201`, Server nach
  der letzten Änderung gestartet): `today` an der Kopie: Pausentag → `owesDuty: false` für alle Pflicht-Positionen, sonst `true`; Arcade per E2E `pfad` (Frist am Pflichttag). Die Drohung vor dem ersten Spiel ist bewusst offen (B-1024). Commits `f4df2984`, `4e70c583`.
