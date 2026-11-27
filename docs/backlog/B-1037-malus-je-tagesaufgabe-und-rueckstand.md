---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Malus je Tagesaufgabe, Abrechnung je Aufgabe, Rückstand]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-736
grund: ""
ersetzt_durch: []
---

# B-1037 · Malus je verpasster Tagesaufgabe und Rückstand am nächsten Lerntag

Zweiter Teil aus der Teilung von [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) (Nachtlauf P, 2026-11-27): die **Abrechnung**. Baut auf
[B-1036](B-1036-pflicht-quelle-tagesaufgabe.md) (scharfe Tage, Taktung eines Tages) auf und muss vor [B-737](B-737-reset-zum-stichtag.md)
stehen, das den Modus über die API freigibt.

## User Story

Als *Vater* möchte ich, dass eine verpasste Pflichtaufgabe genau einmal den Malus-Betrag meines Kindes kostet und eine erledigte nichts.
Als *Sohn* möchte ich nur für das Münzen verlieren, was ich an dem Tag sehen konnte, und Verpasstes am nächsten Lerntag nachholen können,
ohne dass der Tag überläuft.

## Ist-Stand am Code

Wie in B-736 (`SettleClosedPeriodsAsync`, `PositionGoalPenalty`, Wallet-Invariante `PositionProgressService.cs:827`). Seit B-1036 rechnet
die alte Abrechnung scharfe Tage nicht mehr ab; eine neue gibt es noch nicht, und der angezeigte Malus einer Aufgabe ist 0.

## Entscheidungen (geerbt)

B-736 Entscheidungen 1, 2, 3 und Schätz-Annahmen 2 bis 4 (dort mit Herkunft und Begründung): Malus je verpasster Aufgabe mit dem Betrag des
Kindes (`Child.PenaltyCoinsPerMissedTask`), genau einmal, nur für einen gesehenen Plan; `DoneAt` an der Aufgabe, gesetzt vom Pfad, der das
Ziel bucht; träge Abrechnung (Login, `MeController`, Zeitgeber), höchstens 14 Tage zurück; Rückstand als Planer-Regel innerhalb des Deckels.

## Akzeptanzkriterien

B-736 Kriterien 3, 4, 5 (Malus-Teil), 7 und 11, unverändert; dazu: der angezeigte Malus einer Aufgabe ist der Betrag des Kindes (die
Schätz-Annahme 5 aus B-1036 fällt). *Rot, wenn* Anzeige und Buchung verschiedene Beträge kennen.

## Schätzung

**Größe: M** — Migration (`DailyTask.DoneAt`, Tabelle `DailyTaskPenalty` mit eindeutiger Aufgabe), eine Abrechnung nach dem Muster von
`SettleClosedPeriodsAsync` mit Wallet-Bump und Index-Verlierer, `DoneAt` im Zielpfad, eine Rang-Regel in `DailyPlanRules` für den Rückstand.
Testweg wie B-736 (`TagespflichtTests`, `TagespflichtWalletTests`, Mutanten), Messung an einer Kopie der echten DB.

## Verlauf

- **2026-11-27** — angelegt durch Teilung von B-736 (Nachtlauf P), `geschaetzt` mit den geerbten Entscheidungen.
