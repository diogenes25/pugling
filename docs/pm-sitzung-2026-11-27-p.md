---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf P – die Pflicht kommt aus der Tagesaufgabe

**Datum:** 2026-11-27  ·  **Moderation:** PM (unbeaufsichtigter Nachtlauf nach [nachtlauf.md](nachtlauf.md))
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die nächste Etappe des Tagesplaners nach Sprint 197: die scharfe Schaltung (B-736), soweit sie ohne neue Entscheidung baubar ist.
**Arbeitsort:** Worktree `.claude/worktrees/nachtlauf-p`, Branch `nachtlauf/2026-11-27-p` von `main` (`8c4395de`). Der Haupt-Checkout steht auf
`turmspiel-gdscript` mit offenen Skripten des Nutzers und bleibt unberührt.

## Runde 1 — Findung

Die Roadmap nach Sprint 197 nennt B-736, dann B-737. Offene Defekte liegen nur als `idee` P3 vor (B-860, B-971, B-994, B-1029). B-736 ist ein
`geschaetzt`er Wunsch und damit nach Freigabe 8 baubar.

Beim Lesen des Codes reißt B-736 die L-Marke, wie ihre Schätzung es als Möglichkeit nennt: fünf Leser der Pflicht (Tagessicht,
Pflichtzustand, Serie, Zeitachse, Zielpunkte), eine Dosis, deren 0 „keine Dosis“ heißt, und eine Abrechnung mit Migration. **Geteilt** nach
README → „Teilen und Zusammenlegen“: [B-1036](backlog/B-1036-pflicht-quelle-tagesaufgabe.md) Pflichtquelle,
[B-1037](backlog/B-1037-malus-je-tagesaufgabe-und-rueckstand.md) Abrechnung und Rückstand, [B-1038](backlog/B-1038-dosis-aus-der-tagesaufgabe.md)
Dosis. Kriterium 6 (gestrichene Aufgabe) setzt das Streichen voraus und liegt bei B-738.

**Entscheidungslage (Freigabe 8):** B-680 Entscheidung 6 beantwortet die Zielpunkt-Frage („Zielpunkte, Truhe und Serie bleiben an der
erfüllten Pflicht“). Was B-1036 darüber hinaus annimmt, steht dort als fünf Schätz-Annahmen – Folgerungen aus geerbten Entscheidungen, keine
neue Produktrichtung. **Zur Bestätigung am Morgen:** Annahme 1 (wann ein Tag scharf ist), 2 (Wochenziel-Position als Tagesaufgabe am Tag
gemessen), 3 (Zielpunkte nur für Positionen mit Aufgabe).

**Offen beim Vater (neu, nicht entschieden):** B-1038 – darf eine Kür-Position an einem scharfen Tag neue Wörter bringen (Umgehung des
Wörter-Deckels)?

**Rollengang vor dem Bauen:** entfällt – die echte DB hat keinen Planer an (Stand Sprint 197), es gibt dort keine Zeile, die die neue Regel liest.

## Sprint 198 — Ziel & Umfang

**Sprint-Ziel (Vatersicht):** Schalte ich ein Kind (an der Kopie, per DB) scharf, sehen Sohn-App und Vater-Web als Pflicht des Tages genau die
Aufgaben des Tagesplans: Serie, Truhe und Zielpunkte folgen ihnen, ein Tag ohne Plan ist wie eine Pause. Widerlegt, wenn eine Position ohne
Aufgabe Pflicht wird, der alte Malus einen scharfen Tag bucht oder sich im Schattenmodus etwas ändert.
**Umfang:** B-1036.
**Entwickler-Brief:** *Quelle der Wahrheit* `ChildDutyDays` (trägt die Aufgaben der scharfen Tage) und ein Helfer
`PositionProgressService.CadenceOn(pos, day, dutyDays)` – die Senke, durch die jede Frage „Pflicht heute?“ läuft. *Guards* Planer und
Tagesauswertung lesen weiter den reinen Kalender; kein Student-Schreibpfad; Wallet unberührt (kein neuer Abbuchungspfad). *Migration* keine
(Enum als Text). *Testweg* `TagespflichtScharfTests`, Regressionsnetz, Mutanten je Leser, E2E `tagesplan-schatten`.

## Iteration 198 — umgesetzt

Siehe `## Verlauf` in [B-1036](backlog/B-1036-pflicht-quelle-tagesaufgabe.md).

- **Backend:** `PlannerMode.Enforced` (Vertrag, additiv), über `PATCH children/{id}` noch nicht setzbar. `DutyCalendar` lädt die Aufgaben der
  scharfen Pläne in `ChildDutyDays` (`TaskPositionsOn`, `JudgedByTasks`, `DayOwesDuty`). `PositionProgressService.CadenceOn` ist die Senke für
  „Pflicht heute?“: Tagessicht, Pflichtzustand, Serie, erfüllte Tage, Zielpunkte lesen sie; der alte Malus überspringt scharfe Tage; die
  Missions-Serie (`MetricsService`) überspringt Tage ohne Aufgabe. Der Planer nimmt nur Positionen mit Pflicht.
- **Frontend:** zwei Zeilen in `Tagesplaene.tsx` (`planWirkung`, `planerStatus`), erzwungen von den erschöpfenden `switch`.
- **Verifikation:** Suite Release 2715/2715; `TagespflichtScharfTests` 15/15; Mutanten 20/20 rot am benannten Test; Vitest `Tagesplaene` 6/6,
  `tsc -b` grün; E2E 5 passed (Ausweichports). Review `pugling-reviewer`: kein 🔴, drei Funde behoben (Zähler 3 von 5), zwei als Frage
  [B-1039](backlog/B-1039-umschalten-scharf-umschalttag-und-wochenziel.md) abgelegt.

## Runde 2 — Re-Review / Abnahme (Sprint 198)

Kopie der echten DB (`kopie-server.sh`, :5201, gestartet nach der letzten Code-Änderung; keine ausstehende Migration), Vater-Sitz, Friedrich:

- **Vater — abgenommen.** „Ohne Planer hat Friedrich heute 8 Pflichten. Schattenmodus eingeschaltet: der Tagesplan nennt die Positionen 133,
  134, 202 und 218 – und Friedrichs Tag zeigt weiter dieselben 8 Pflichten. ‚Scharf‘ lässt die Oberfläche nicht zu (‚not available yet‘).
  Von Hand an der Kopie scharf gestellt: genau die 4 Aufgaben sind Pflicht, die Wochenposition 218 als Tagesziel, kein Malus-Preis.“
  **Benannt offen:** B-1039 (Umschalttag, angebrochene Woche) vor B-737; der Plan meldet „99 neue Wörter pro Lerntag“ → B-1040.
- **Sohn — Regressionszeuge.** Kein Kind ist scharf, der Modus ist über die API nicht setzbar; E2E `tagesplan-schatten`, `belohnung-*`,
  `nacharbeit*` grün; Friedrichs Tag im Schattenmodus unverändert.
- **Creator — Regressionszeuge.** Kein Creator-Pfad im Diff; Suite grün.

Abgenommen: B-1036.

## Retrospektive (Sprint 198)

**Nachschau:** B-680 (Sprint 197, `229589a9`, `2dfa44e2`) nachgesehen: Termin-Druck an der Kopie nachgerechnet (Friedrich, Klassenarbeit
04.12.: „99 neue Wörter pro Lerntag“) – zwei Positionen derselben Übung zählen ihre Wörter doppelt → **Defekt
[B-1040](backlog/B-1040-termin-druck-zaehlt-woerter-doppelt.md)**, `entgangen_bei: B-680`. Ohne Befund geprüft: abgesagter/fremder Termin
(`Status == Planned`, `ChildId`), `ExamPresses` nur bei benannter Übung. `nachgeschaut: 2026-11-27` an B-680.

**Was die eigenen Tore durchließen und wer es fand:**

- **Mutantenprobe, kein Review:** mein Schatten-Fall setzte kein `PlannerSince` und konnte den Modus-Filter des Kindes nicht unterscheiden (S1 grün).
- **Review:** Zeitachse (AK 3) ohne Fall; die Momentaufnahme nach dem Zurückschalten ohne Fall; zwei Umschaltfälle (B-1039).
- **Rollengang an echten Daten:** B-1040 – die Planer-Tests von Sprint 197 legen jede Position auf eine eigene Übung; Friedrichs echter Plan führt
  Tages- und Wochenposition auf derselben Übung.

**Vorschlag (Freigabe 3: nur vorgeschlagen, nicht gelandet):** ein Skript `planer-trockenlauf` neben `kopie-server.sh`, das an der Kopie für
jedes echte Kind den heutigen Plan im Schattenmodus komponiert und Aufgaben, Budget und Warnungen druckt – der Trockenlauf aus pm-loop Step 4
(seit Sprint 197 Prosa) als ein Befehl. Hätte B-1040 schon in Sprint 197 gezeigt; ein Tor ist es nicht (ob eine Warnung plausibel ist,
bleibt ein Urteil).

**Bewusst ungedeckt:** `OpenGoalProgressAsync` mit Aufgaben-Taktung (nur bei Typen ohne Prüfmodus sichtbar).

**Zähler Sprint 198:** 3 (S1-Fall, Zeitachse, Momentaufnahme) – unter 5, kein Halt. Neues Tor: keines.
