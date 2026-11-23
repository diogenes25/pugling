---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Laufende Nacharbeit zur Kür]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachtlauf B 2026-11-23, Review von B-659 (pugling-reviewer, 🟢4)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
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

## User Story

Als Sohn möchte ich, dass meine erfüllten Nacharbeits-Tage in der Serie bleiben, wenn der Vater die laufende Nacharbeit
auf Kür stellt – wie bei jeder anderen Pflicht seit B-659.

## Ist-Stand am Code

- `PositionProgressService.CloseDutyAsync` (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:230`): früher
  Ausstieg für alles außer `Daily`/`Weekly`.
- `DutyCadenceOn` (`:66`) liest ohne Spanne die heutige Taktung; `HasDutyOn` (`:53`) beendet `UntilDone` nach
  `CompletedOn`; `IsGoalMetAsync` (`:208`) prüft `UntilDone` über `RemediationDayMetAsync` (eine ganze Runde, oder
  `CompletedOn` erreicht); `PeriodRange` (`:35`) gibt für `UntilDone` den Tag selbst.

## Die echte Lücke

Die Schließ-Regel braucht für `UntilDone` nichts Neues: die Periode ist ein Tag, „erfüllt?“ beantwortet `IsGoalMetAsync`
schon richtig, und `HasDutyOn` liest `CompletedOn` zusätzlich zur Spanne. Es fehlt nur der Fall im Wächter.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **`UntilDone` in die Schließ-Regel aufnehmen** (`Daily or Weekly or UntilDone`). Kosten: eine Zeile; die
   Entscheidung 2 von B-659 („nur Daily/Weekly“) wird für `UntilDone` aufgehoben – sie war eine Abgrenzung des damaligen
   Umfangs, kein Produktwunsch (B-659 nennt die Nacharbeit nicht als gewollt anders).
2. **Die Spanne endet spätestens am Ende der Nacharbeit** (`CompletedOn`): ab dem Ende gilt „erfüllt“ per Definition,
   und eine Spanne bis heute brächte die beendete Pflicht in die Tagesansicht zurück, die die Spanne liest und nicht
   `CompletedOn` (Review-Fund, siehe Verlauf). Nebenwirkung, gewollt: auch eine **beendete** Nacharbeit, die der Vater
   später freigibt, behält jetzt ihre Tage bis zum Ende in der Serie – vorher fielen sie wie bei B-659 heraus.
3. **Malus unverändert** (Entscheidung 5 von B-659): das Freigeben erlässt offene Perioden.

## Akzeptanzkriterien

1. Kind mit einer laufenden Nacharbeit „täglich, bis erledigt“ als einziger Pflicht, heute eine Runde ganz gespielt
   (Serie 1); der Vater stellt auf Kür → Serie bleibt 1, `DutyUntil` = heute, `CadenceUntil` = `UntilDone`. Rot ohne die
   Taktung im Wächter.
2. Dieselbe Lage ohne gespielte Runde → `DutyUntil` = gestern (offene Periode zählt nicht). Rot, wenn die Regel für
   `UntilDone` immer heute schreibt.
3. Eine vor zwei Tagen beendete Nacharbeit wird heute freigegeben → `DutyUntil` = Tag des Endes, heute kein erfülltes
   Ziel, Belohnungen bleiben zu. Rot ohne die Begrenzung.

## Schätzung

XS, backend, keine Migration, kein Vertragsbruch. Testweg: Integrationstest in `RemediationPositionTests` (Runde als
Sitzung geseedet, Wechsel über die Vater-API, Serie über `ChildStreakAsync`).

## Verlauf

- **2026-11-22** — angelegt (Nachtlauf B, Review von B-659). Bewusst nicht in B-659 gebaut: das Sprint-Ziel von B2 hält
  ohne sie, und die Entscheidung 2 von B-659 schließt `UntilDone` ausdrücklich aus.
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 119). Rote Probe vor dem Fix: `Laufende_Nacharbeit_Auf_Kuer_Behaelt_Ihren_Erfuellten_Tag`
  erwartet rot in beiden Fällen, gemessen rot (2/2: gespielt Serie 0 statt 1, nicht gespielt `DutyUntil` null statt gestern).
  - AK 1 → Fall `playedToday: true`, R1 (`UntilDone` aus dem Wächter) → rot.
  - AK 2 → Fall `playedToday: false`, R2 (für `UntilDone` immer heute) → rot.
  Review (`pugling-reviewer`): 🟡 eine **beendete** Nacharbeit bekam beim Freigeben eine Spanne bis heute, die
  Tagesansicht (`ComputeDayAsync` filtert die rohe Taktung) zeigte sie als erfüllte Pflicht und öffnete Truhe und
  Bonus-Training ohne Spielen → Spanne auf `CompletedOn` begrenzt (**gezählt**, 1/5); 🟢 Story nannte die beendete
  Nacharbeit „nicht betroffen“ → Entscheidung 2 und AK 3 nachgezogen; 🟢 die Serien-Zeile im ungespielten Fall kann nicht
  fallen → im Test als Kontext beschriftet (beide ohne Zählung).
  - AK 3 → `RemediationPositionTests.Beendete_Nacharbeit_Auf_Kuer_Endet_Am_Tag_Ihres_Endes`, rote Probe vor der Begrenzung
    erwartet rot, gemessen rot (1/1, `DutyUntil` 23.11. statt 21.11.); R3 (Begrenzung aus) → rot. R1/R2 nach der letzten
    Änderung erneut rot.
- **2026-11-23** — abgenommen (Sprint 119, Commit siehe Protokoll). Verifikation nach der letzten Änderung: Backend
  2131/2131 (Release); Rollengang Vater/Sohn maschinell über die Integrationstests (Wechsel über die Vater-API, Serie und
  Tagesansicht über den Dienst) plus E2E `nacharbeit` und `nacharbeit-bis-erledigt` gegen frisch gestartete Server
  (5211/5184) – 2 passed. Creator Regressionszeuge (Suite grün).
