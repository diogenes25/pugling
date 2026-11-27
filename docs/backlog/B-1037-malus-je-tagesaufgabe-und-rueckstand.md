---
tags: [typ/story, status/in-arbeit, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Malus je Tagesaufgabe, Abrechnung je Aufgabe, Rückstand]
status: in-arbeit
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-736
grund: ""
ersetzt_durch: []
wartet_auf: "Nutzer: Rollengang und Abnahme; Nachtlauf P endete am Fünfer-Zähler (B-1041)"
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
- **2026-11-27** — **gebaut** (Nachtlauf P, Sprint 199). Migration `TagesaufgabeMalus` (`DailyTask.DoneAt`, Tabelle `DailyTaskPenalties`,
  eindeutig je Aufgabe, Erstattungsmarke wie am Positions-Malus). Abrechnung `AddMissedTaskPenaltiesAsync` in `SettleClosedPeriodsAsync`
  (Stamp-Bump dort), Stempel `StampDoneTasksAsync` nach `EvaluateAndAwardAsync`, Preis = Betrag des Kindes, Erstattung in
  `FeedbackCorrectionService`, Zeitgeber rechnet scharfe Kinder ab 6 Uhr ab, Rückstand `MissedYesterday` im Planer (Stempel fehlt **und** Regel
  nicht erfüllt). Abweichung von B-736 Schätz-Annahme 2 (bewusst, zugunsten des Kindes): die Abrechnung prüft bei fehlendem Stempel die Regel
  erneut. Fälle:
  - AK 3, offen kostet einmal den Betrag, Text, erledigte nicht → `TagespflichtScharfTests.Offene_Aufgabe_Kostet_Einmal_Den_Betrag_Des_Kindes`
    (T5, T14, T15, T17); Stempel gelesen → `Erledigt_Gestempelte_Aufgabe_Kostet_Nichts` (T9); Stempel gesetzt → `Zielpfad_Stempelt_Die_Erledigte_Aufgabe` (T2–T4).
  - AK 3, eindeutiger Index: die Vorprüfung entfernt bleibt grün (T11, erwartet grün) – der Index ist die Sicherung.
  - AK 4 → `Aufgaben_Malus_Bumpt_Den_Stamp` (T6). Kein Wettlauf-Fall (B-1041).
  - AK 5 → `Tag_Ohne_Plan_Pause_Und_Heute_Kosten_Nichts` (T8, T12; Teilfall „ohne Plan“ trägt nichts → B-1041);
    `Abgeschalteter_Plan_Und_Geloeschte_Position_Kosten_Nichts` (T13); Theorie `Ohne_Betrag_Im_Schatten_Und_Zu_Alt_Kostet_Nichts` (T7, T10, T16).
  - AK 7 → `PlanerRueckstandRegelnTests` (T19–T21) und `Offene_Aufgabe_Kommt_Am_Naechsten_Lerntag_Wieder` (T22–T24).
  - Preis → `Aufgabe_Zeigt_Den_Betrag_Des_Kindes` (T1); vergangener Tag nach Betragsänderung ungedeckt (B-1041).
  - Zeitgeber → Theorie `Zeitgeber_Rechnet_Verpasste_Aufgaben_Ab` (12 Uhr bucht, 0:30 nicht; T18, T25). Erstattung →
    `MeldungBuchtNachTests.Aufgaben_Malus_wird_durch_berechtigte_Meldung_zurueckgenommen` (R1).
  - AK 11 → `SchemaGuardTests` grün mit beiden neuen Beziehungen.
- **2026-11-27** — Mutantenprobe nach der letzten Code-Änderung (`c9e00d7d + Arbeitsbaum`, 23:32, Debug): 25 von 25 rot am benannten Test,
  T11 erwartet grün und grün; Grundlinie nach dem Zurückspielen grün. Erste Messung 22:5x: T7 und T10 grün (Fälle maßen nicht) → Fälle
  geschärft.
- **2026-11-27** — Review `pugling-reviewer`: kein 🔴; Zähler des Sprints lief über 5 (Liste in `docs/pm-sitzung-2026-11-27-p.md`, „Halt des
  Nachtlaufs“). Behoben: „verpasst“ liest im Planer Stempel **und** Regel; der Zeitgeber bucht erst ab 6 Uhr. Offen: [B-1041](B-1041-restpunkte-review-b-1037.md).
  **Bleibt `in-arbeit`:** Rollengang und Abnahme stehen aus; wartet auf den Nutzer.
