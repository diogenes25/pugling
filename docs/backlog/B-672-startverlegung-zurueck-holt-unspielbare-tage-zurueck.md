---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Start zurückverlegt bricht Serie, Ferienpause kostet Malus]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von Sprint 87 (fe13531c, pugling-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-613]
---

# B-672 · Ein zurückverlegter Planstart holt Tage zurück, an denen der Plan nicht spielbar war

Am Code bestätigt (Arbeitsstand 2026-11-18): Beim Verlegen des Starts eines aktiven Plans wird `ActivatedAt` nur
**gesenkt** (`if (playableFrom < plan.ActivatedAt)`, `backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:225-229`),
nie angehoben. Der Kommentar verspricht „days before it were not playable then, so they stay in the child's favour“ –
das hält nur, wenn `ActivatedAt` jünger ist als der neue Start.

- **Serie:** Plan mit Start 1.12., am 18.11. vorzeitig eingeschaltet (`ActivatedAt` 18.11.); am 25.11. Start auf 24.11.
  zurück ⇒ `ActivatedAt` bleibt 18.11., die Tage ab 18.11. zählen als offene Pflicht, die Serie bricht rückwirkend.
- **Malus:** Plan seit 1.11. (gespielt); am 5.11. Start auf 15.11. verlegt (Ferien, nicht spielbar); am 20.11. zurück auf
  1.11. ⇒ `PlanDueForPeriod` nimmt die Überlappungsregel (`ActivatedAt <= StartDate`), die Abrechnung bucht Malus für
  6.–14.11.

Der einzige Test zur Startverlegung ist `RueckwirkendePflichtTests.Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet`
(Zukunftsstart auf heute vorgezogen).

## Naheliegend (Reviewer)

Vor dem bestehenden Absenken: lag der alte Start nach `ActivatedAt`, waren die Tage bis `min(alter Start, heute)` nicht
spielbar ⇒ `ActivatedAt = max(ActivatedAt, min(alter Start, heute))`. Zwei Fälle (Serie, Malus), beide heute rot.
