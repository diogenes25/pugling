---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Laufende Nacharbeit zur Kür]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf B 2026-11-23, Review von B-659 (pugling-reviewer, 🟢4)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-820 · Stellt der Vater eine laufende Nacharbeit auf Kür, fallen ihre erfüllten Tage aus der Serie

B-659 hält die erfüllten Tage einer Pflicht, die der Vater später auf Kür stellt – aber nur für Tages- und
Wochenpflichten (Entscheidung 2 dort: die Schließ-Regel läuft nur beim Übergang Daily/Weekly → None). Eine **laufende**
Nacharbeit „täglich, bis erledigt“ (`UntilDone`, `CompletedOn` leer), die der Vater auf Kür stellt, schreibt keine Spanne
(`PositionProgressService.CloseDutyAsync`, früher Ausstieg). Ihre Tage lesen danach die heutige Taktung `None`, und
waren sie die einzige Pflicht, fällt die Serie rückwirkend – dieselbe Fehlerklasse wie B-659.

Gelesen am Code (Review 2026-11-22), nicht nachgestellt. Abzugrenzen: eine **beendete** Nacharbeit endet über
`CompletedOn` (B-636, B-669) und ist davon nicht betroffen.

Offen für die Ausformulierung: ob die Schließ-Regel `UntilDone` mit aufnimmt (die Periode ist ein Tag; „erfüllt“ heißt
dort „eine Runde durch alle Aufgaben“, `RemediationDayMetAsync`) und ob `CadenceUntil = UntilDone` dann mit `CompletedOn`
zusammen gelesen werden muss.

## Verlauf

- **2026-11-22** — angelegt (Nachtlauf B, Review von B-659). Bewusst nicht in B-659 gebaut: das Sprint-Ziel von B2 hält
  ohne sie, und die Entscheidung 2 von B-659 schließt `UntilDone` ausdrücklich aus.
