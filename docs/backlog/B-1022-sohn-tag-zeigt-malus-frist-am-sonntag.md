---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: []
status: idee
prio: P1
art: Defekt
unverifiziert: true
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

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
