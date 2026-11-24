---
tags: [typ/story, status/abgenommen, bereich/punkte, rolle/student, rolle/supervisor]
aliases: [Malus nennt den verpassten Tag]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-24.md#runde-139--vorstellung--feedback
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-951 · Ein Malus nennt nicht den Tag, den das Kind verpasst hat

Der Malus wird lazy bei der nächsten Anmeldung gebucht. Seine Ledger-Zeile heißt nur „Tagesziel gerissen“ bzw.
„Wochenziel gerissen“, ohne den Tag oder die Woche. Gesehen in Runde 139 an der Kopie der echten DB: Theodor meldet sich
am Dienstag um 16:02 an und bekommt fünfmal −2 Münzen für den **Montag**. Im Sohn-Konto steht „Pflicht gerissen (Malus) ·
[…] Tagesziel gerissen“ ohne Datum, im Vater-Konto mit dem Buchungsdatum **24.11.** An dem Dienstag ist der Tag noch offen.
Beide lesen also „heute verpasst“. Dazu kommt das Wochenziel der Vorwoche: Es steht als „Wochenziel gerissen“ im Verlauf,
und dieselbe Position zeigt auf „Heute“ zugleich „Wochenziel offen“.

## User Story

Als Sohn möchte ich an einem Malus sehen, für welchen Tag bzw. welche Woche er abgezogen wurde. So weiß ich, dass mein
heutiger Tag noch zu retten ist, und halte den Abzug nicht für einen Fehler der App.
Als Vater möchte ich im Konto des Kindes sehen, welcher Tag verpasst wurde, nicht nur, wann die App das gemerkt hat.

## Ist-Stand am Code

- Einzige Schreibstelle eines `GoalPenalty`: `backend/Pugling.Api/Services/Shared/PositionProgressService.cs:884`. Dort ist
  `Reason = $"[{plan.Title} · {pos.Exercise?.Title}] {Tagesziel|Wochenziel} gerissen"`. Die Periode `(from, to)` liegt in der
  Schleife (`:852`) und wird nur in die `PositionGoalPenalty`-Zeile geschrieben (`PeriodStart`/`Day`, `:870`), nicht in den
  Ledger-Text.
- `CreatedAt = now` (`:885`) ist der Zeitpunkt der Abrechnung, also der nächsten Anmeldung bzw. des nächsten Shop-Kaufs
  (`SettleClosedPeriodsAsync`, `:818`). Es gibt keinen Scheduler.
- Wochen: `ClosedPeriods` (`:749`) liefert nur abgeschlossene Wochen, Montag bis Sonntag.
- Sohn-Konto `frontend/src/sohn/SohnKonto.tsx:36` zeigt die Art und `reason`, **kein Datum**.
- Vater-Konto `frontend/src/vater/VaterKonto.tsx:72` zeigt `formatDate(e.createdAt)`, also den Abrechnungstag.
- Die Belohnung `:630` („Tagesziel erreicht“) entsteht im selben Moment wie die Leistung. Ihr fehlt der Tag nicht.

## Die echte Lücke

Die Periode ist bekannt, aber an der einen Senke, die beide Oberflächen lesen (der Ledger-Text), geht sie verloren. Das
Buchungsdatum ist nicht falsch, beantwortet aber eine andere Frage („wann gemerkt“) als die, die das Kind stellt
(„wann verpasst“).

## Offene Punkte

1. ~~**Wo steht die Periode?** Im Ledger-Text (eine Senke, beide Oberflächen und alte Clients bekommen sie sofort, kein
   Vertrag) oder als neues Feld am Eintrag (`periodStart`/`cadence`, formatiert in der Oberfläche, braucht Migration oder
   Join)? *Empfehlung:* Text. Der Ledger-Text ist schon Produktinhalt auf Deutsch, und die Belohnung trägt ihre Angabe
   ebenso dort.~~ → Entscheidung 1
2. ~~**Form.** *Empfehlung:* „Tagesziel vom Mo 23.11. gerissen“ bzw. „Wochenziel der Woche 16.–22.11. gerissen“.~~ → Entscheidung 2
3. ~~**Altbestand.** Bereits gebuchte Zeilen umschreiben (Migration über `PositionGoalPenalty` → Ledger, ohne FK nur über
   Kind + Zeitpunkt zuzuordnen) oder stehen lassen? *Empfehlung:* stehen lassen. Das Ledger wird nicht rückwirkend
   umgeschrieben (Grundsatz aus B-432), und die Zuordnung wäre geraten.~~ → Entscheidung 3
4. **Zurückgestellt: Datum im Sohn-Konto.** Soll die Zeile zusätzlich das Buchungsdatum zeigen? *Empfehlung:* nein, nicht in dieser Story.
   Mit der Periode im Text ist die Frage des Kindes beantwortet.

## Entscheidungen

Im Dialog mit dem Nutzer am 2026-11-24 (Runde 139); Punkt 4 ist nicht gefragt und darum zurückgestellt.

1. **Die Periode steht im Ledger-Text.** Es gibt nur eine Senke (`PositionProgressService.cs:884`). Sohn- und Vater-Konto
   zeigen den Text ohnehin, also braucht es weder Vertrag noch Migration. *Kosten:* Die Oberfläche kann nicht nach der
   Periode filtern oder sortieren.
2. **Form „Tagesziel vom Mo 23.11. gerissen“ bzw. „Wochenziel der Woche 16.–22.11. gerissen“.** Eine Woche über eine
   Monatsgrenze heißt „30.11.–6.12.“. Die Wochentage kommen aus einer festen Liste (So … Sa), nicht aus der Kultur: Mit ICU
   schreibt de-DE „Mo.“, ohne ICU „Mo“, und lokal läuft Windows, in CI Linux. *Kosten:* keine Übersetzung. Der Ledger-Text
   ist schon heute deutscher Produktinhalt.
3. **Altbestand bleibt.** Das Ledger wird nicht rückwirkend umgeschrieben (B-432). *Kosten:* Die bisherigen Malus-Zeilen
   bleiben ohne Tag.

## Akzeptanzkriterien

1. Ein Tagesziel-Malus nennt den **verpassten** Tag mit Wochentag („Tagesziel vom Mo 23.11. gerissen“), nicht den Tag der
   Abrechnung. Werden zwei Tage auf einmal abgerechnet, trägt jede Zeile ihren eigenen Tag.
2. Ein Wochenziel-Malus nennt die verpasste Woche von Montag bis Sonntag („Wochenziel der Woche 16.–22.11. gerissen“). Über
   eine Monatsgrenze stehen beide Monate („30.11.–6.12.“).
3. Bereits gebuchte Zeilen bleiben unverändert (keine Migration).

## Schätzung

**Größe: S**: eine Zeile an der Senke, dazu ein Formatierer. Kein Vertrag, keine Migration, kein Frontend.
**Risiken:** Der Wochentag hängt an `DayOfWeek` (Sonntag = 0). Ein Index-Versatz träfe jeden Tag, darum braucht der Test
Tage mit verschiedenen Wochentagen und einen Sonntag.
**Angriffsplan:** `internal static string MissedPeriodLabel(GoalCadence, DateOnly from, DateOnly to)` in
`PositionProgressService`, aufgerufen an `:884`.
**Testweg:** `PflichtMalusTests.MalusBuchungstext_…` mit exakter Erwartung je Tag und Woche (Integration). Dazu ein
Formatierer-Test mit festen Daten (Sonntag, einstelliger Tag, Woche über die Monats- und die Jahresgrenze). Rollengang:
Theodors Anmeldung an einer frischen Kopie der echten DB, die Zeilen lesen.

## Verlauf

- **2026-11-24** — angelegt und ausformuliert (Quelle: Rollengang Runde 139 an der Kopie der echten DB, Theodors Anmeldung
  am Dienstag 16:02 mit fünf Malus-Buchungen für Montag).
- **2026-11-24** — gegrillt (Entscheidungen 1–3 im Dialog, Punkt 4 zurückgestellt) und geschätzt (S, backend).
- **2026-11-24** — in Arbeit: `MissedPeriodLabel` in `PositionProgressService` (Senke: einzige `GoalPenalty`-Schreibstelle).
- AK 1, Tag statt Abrechnungstag → `PflichtMalusTests.MalusBuchungstext_BenenntTagesZielUndWochenziel_JeweilsRichtig`
  (exakte Texte für heute−2 und heute−1), Mutante B951-1 → rot.
- AK 1, zwei Tage tragen je ihren Tag → derselbe Test, B951-2 → rot.
- AK 1, Wochentag (So = Index 0) → `MissedPeriodLabel_NenntTagOderWoche` (sieben Wochentage, einstelliger Tag), B951-3 → rot.
- AK 1/2, Takt → beide Tests, B951-4 → rot.
- AK 2, Monatsgrenze → Theorie `30.11.–6.12.`, B951-5 → rot; Wochenende → Theorie und Integration, B951-6 → rot.
- AK 3, keine Migration → `git show --stat 04827f89` berührt nur Service und Test.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | B951-1 Abrechnungstag statt Periode | PositionProgressService.cs:900 | red | red |
  | B951-2 jeder Tag gleich | PositionProgressService.cs:900 | red | red |
  | B951-3 Wochentag versetzt | PositionProgressService.cs:60 | red | red |
  | B951-4 Takt vertauscht | PositionProgressService.cs:59 | red | red |
  | B951-5 Monatsgrenze ohne Monat | PositionProgressService.cs:61 | red | red |
  | B951-6 Wochenende fehlt | PositionProgressService.cs:62 | red | red |

  Gemessen mit `mutant-probe.py` an `567b3e61 + Arbeitsbaum`. Danach wurden nur Kommentare geändert (Review).
- **2026-11-24** — `pugling-reviewer`: kein Korrektheitsfund. Zwei Aufräumpunkte übernommen (Begründung im Kommentar,
  Test-Summary); die übrigen sind optional und bewusst offen gelassen. Suite 2334/2334 (Release).
- **2026-11-24** — abgenommen, Commit `04827f89`. Rollengang an einer frischen Kopie der echten DB: Theodors Anmeldung am
  Dienstag bucht fünfmal −2, Text „Tagesziel vom Mo 23.11. gerissen“, die Nacharbeit „Wochenziel der Woche 16.–22.11.
  gerissen“. Sohn- und Vater-Konto zeigen `reason` unverändert an, darum gibt es keinen eigenen UI-Gang.
