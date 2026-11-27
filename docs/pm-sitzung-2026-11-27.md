# PM-Sitzung: Stoffrahmen ohne Termin (B-735)

**Datum:** 2026-11-27  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Der Vater legt je Kind und Fach einen Stoff ohne erfundenes Enddatum an und trägt den Termin später nach.

## Runde 1 — Findung

Keine neue Anmerkung unter `docs/anmerkungen/` (Stand unverändert). Ausgangslage aus
[pm-sitzung-2026-11-26](pm-sitzung-2026-11-26.md): nächster Strang des Planers ist B-735 → B-680 → B-736; B-1033 bleibt Frage.
B-735 trägt die stellvertretende Entscheidung 17 aus B-644.

**Entscheidung des Nutzers (Runde 1):** Entscheidung 17 (`StudyPlan.EndDate` optional, Vertragsbruch, `Klassenarbeit.ScheduledDate`
bleibt Pflicht) ist bestätigt; Sprint 196 = B-735.

## Runde 2 — Rollengang vor dem Bauen (Kopie der echten DB)

Kopie-Server auf `:5201` (Stand `a324ffe8`, keine ausstehende Migration). Kein neuer Befund unter `docs/anmerkungen/`.

- **Creator — Regressionszeuge.** Kein `creator/*`-Pfad außer dem Medienbedarf-Leser im Diff; dessen Fall steht in `OffenerPlanTests`.
- **Vater (beobachtet an den Plänen der Kopie):** alle 15 Pläne tragen ein erfundenes Enddatum (10–42 Tage nach dem Start); der
  Klausurtermin steht bei keinem fest. „Ich kann keinen Stoff anlegen, ohne ein Datum zu erfinden.“ → B-735.
- **Sohn — Regressionszeuge.** Kein Sohn-Weg im Diff außer den Lesern des Plans.

## Sprint 196 — Ziel & Umfang

**Sprint-Ziel (Vatersicht):** Ich lege für mein Kind einen Stoff ohne Klausurtermin an, sehe „kein Termin“ statt eines erfundenen
Datums und trage den Termin später nach; der Sohn spielt den Plan wie jeden anderen. Widerlegt, wenn ein offener Plan irgendwo
als abgelaufen gilt, ein Platzhalterdatum trägt oder ein Bestandsplan sein Datum verliert.
**Umfang:** B-735.
**Entwickler-Brief:** *Quelle der Wahrheit* `StudyPlan.EndDate?`; „kein Ende“ heißt „läuft bis heute“ (Schleifen bis `min(Ende, heute)`,
`TotalDays` = gelaufene Tage). *Guards* PATCH: `null` ändert nichts, `ClearEndDate` löscht und gewinnt. *Migration* `OffenerPlan`
(`AlterColumn`, Tabellenneubau in SQLite; Probe an der Kopie). *Testweg* `OffenerPlanTests`, `OffenerPlanMigrationTests`, je ein
Fall in Planer und Auswertung, Mutanten je Lesestelle, Vitest, E2E.

## Iteration 196 — umgesetzt

Siehe `## Verlauf` in [B-735](backlog/B-735-stoffrahmen-ohne-termin.md): 19 Lesestellen, Vertrag und Client, Vater-Web. Suite 2677/2678
vor der Korrektur eines Migrationstests, danach die betroffenen 47 Fälle grün; Vitest, `tsc -b`, vier E2E grün.

## Runde 3 — Re-Review / Abnahme

Frische Kopie nach der letzten Änderung (Migration `OffenerPlan` beim Start eingespielt).

- **Vater — abgenommen.** „Alle 15 Pläne haben ihr Datum behalten. Plan 19 habe ich auf ‚kein Termin‘ gestellt: Antwort `endDate:
  null`, weiter spielbar; Termin wieder gesetzt, 20.12. steht da.“ Die Maske (Haken, Anlegen, nachtragen) fährt die E2E
  `stoffrahmen-ohne-termin`. **Benannt offen:** nichts.
- **Sohn — Regressionszeuge.** Friedrichs Tag lädt an der Kopie (44 Positionen, 8 Ziele), die Plan-Übersicht antwortet 200.
- **Creator — Regressionszeuge.** Suite grün, kein Creator-Weg außer dem Medienbedarf (eigener Fall).

Abgenommen: B-735.

## Retrospektive

**Nachschau:** B-734 (Sprint 195) nachgesehen: sein Planer liest `EndDate` an einer Stelle (`DailyPlanner:146`) – sie war schon in
der Liste von B-735, ein Fall steht jetzt dort; kein neuer Defekt. `nachgeschaut: 2026-11-27` gesetzt.

**Was die eigenen Tore durchließen und wer es fand:**

- **Mutantenprobe:** Das Lese-/Anlege-Paar aus dem ersten Wurf deckte 9 von 19 Lesestellen – zehn Mutanten blieben grün (Serie,
  vollständige Tage, Tage des Fortschritts, Medienbedarf, Turmspiel, Verlauf, Auswertung, Planer). Ohne die Probe wäre „jede Stelle
  ein Fall“ aus der Story als erfüllt durchgegangen.
- **Volle Suite:** `AufbauStufeMigrationTests` legte einen Plan ohne Enddatum an – das schrieb `NULL` in das alte NOT-NULL-Schema.
  Gefangen erst in der vollen Suite, nicht in den gezielten Läufen.
- **Reviewer:** ein veralteter Vertragstext („rejected“) aus einer früheren Fassung – der Lauf hat das Dokument neu erzeugt.
- **Werkzeug:** `taskkill` auf `Pugling.Api.exe` hätte den Heimnetz-Server der Söhne mit beendet; die Auto-Prüfung hat es gestoppt,
  beendet wurde gezielt der Prozess auf `:5201`.

**Der eine Mechanismus:** Regel in [backend/Pugling.Api/CLAUDE.md](../backend/Pugling.Api/CLAUDE.md) unter „Schema & Migrationen“
(Prosa, kein Tor): eine Spalte nullable machen heißt je Lesestelle ein Fall mit eigener Mutante, und ältere Migrationstests, die die
Entität ohne den Wert anlegen, kippen erst in der vollen Suite. Kein mechanischer Nachweis – „liest jede Stelle `EndDate`?“ ist
nicht ohne Roslyn-Analyse zu prüfen. Kein neues Tor, also kein Halt aus diesem Grund.

**Bewusst ungedeckt:** Die Create/PATCH-Zeilen von B-735 sind nicht mutiert; die Ende-Feld-Erklärung („leeren = offen“) fehlt.

## Offene Roadmap (Stand nach Sprint 196)

Siehe `docs/backlog/`. Weiter in dieser Reihenfolge: B-680 (Eingänge für den Planer, braucht jetzt den offenen Plan), B-736 (Pflicht
aus der Tagesaufgabe), B-737. B-1033 (Kür im Tagesplan) braucht vorher eine Antwort des Vaters.

## Runde 4 — Findung und Entscheidung (B-1033, Sprint 197)

Der Nutzer entscheidet B-1033: der Planer wählt nur Positionen mit Pflicht (Kür bleibt Kür); abgelegt in B-1033 (`verworfen`) und als
Entscheidung 5 in [B-736](backlog/B-736-pflicht-aus-der-tagesaufgabe.md). Danach gibt er B-680 als Sprint 197 frei.

## Sprint 197 — Ziel & Umfang

**Sprint-Ziel (Vatersicht):** Trage ich für mein Kind eine Klassenarbeit ein, sehe ich im Schattenplan ab dem nächsten Tag, dass der
Planer mehr neue Wörter auf den Termin legt, in der Begründung, wie viel Zeit bleibt, und eine Meldung, wenn der Deckel nicht reicht.
Widerlegt, wenn der eingefrorene heutige Plan sich ändert, ein abgesagter oder fremder Termin Druck macht oder eine Meldung ausbleibt.
**Umfang:** B-680 (Kriterien 2, 3, 4, 9, 10, 11, 12, 13 und die Begründung zu 8).
**Entwickler-Brief:** *Quelle der Wahrheit* `DailyPlanRules.Compose` (rein) – Eingänge sammelt `DailyPlanner.CandidatesAsync` (Klassenarbeiten,
Stundenplan, Auswertung); Senke der Rangfolge ist das eine `byPlan`. *Guards* kein Notenstufen-Dienst im Konstruktor, Tage nur über
`LearningDay`, kein Student-Pfad. *Migration* keine, kein Vertragsbruch. *Testweg* `PlanerTerminRegelnTests`, `TagesplanerTests`, Mutanten, E2E.

## Iteration 197 — umgesetzt

Siehe `## Verlauf` in [B-680](backlog/B-680-taeglicher-lehrplan-je-fach-just-in-time.md). Pflicht-Tempo je Position, gemeinsames Budget,
Meldung bei Überschreitung, Rang nach Termin, Stundenplan, Schwäche.

## Runde 5 — Re-Review / Abnahme

Frische Kopie der echten DB (keine ausstehende Migration; die echte DB hat noch keinen Tagesplan, der Planer ist dort aus).

- **Vater — abgenommen, mit einer Frage.** „Ich habe für Friedrich eine Klassenarbeit in drei Tagen eingetragen und den Planer an der
  Kopie eingeschaltet: Die Begründung sagt ‚Klassenarbeit am 04.12., noch 6 Lerntage‘, bei den Positionen mit der nächsten Arbeit
  steht das nähere Datum.“ **Benannt offen:** Eine Arbeit ohne Übungen setzt das ganze Fach unter Druck („120 neue Wörter pro Lerntag“) →
  Frage [B-1034](backlog/B-1034-klassenarbeit-ohne-uebungen-und-der-planer.md).
- **Sohn — Regressionszeuge.** Im Schattenmodus wirkt der Plan nirgends; kein Sohn-Pfad im Diff; E2E `tagesplan-schatten` zeigt seinen Tag unverändert.
- **Creator — Regressionszeuge.** Kein Creator-Pfad im Diff; Suite grün.

Abgenommen: B-680.

## Retrospektive (Sprint 197)

**Nachschau:** B-735 (Sprint 196, `bc8eb9bb`) nachgesehen: der Planer-Eingang liest `EndDate` bereits mit dem Fall „offen“, die Auswertung
ebenso; kein Defekt. `nachgeschaut: 2026-11-27` gesetzt. B-1033 (Frage) ohne Code.

**Was die eigenen Tore durchließen und wer es fand:**

- **Review, kein Tor:** die Zuteilung – die erste Position nahm das ganze Budget, das die Summe mehrerer Tempi war. Meine Regel-Tests
  hatten nur eine gedrückte Position. Die Mutantenprobe zeigte danach, dass auch die Auswahl der gedrückten Positionen ungedeckt war (S2 grün), bis
  der Fall „fällige Position braucht das ganze Budget“ dazukam.
- **Rollengang an den echten Daten:** B-1034 – 29 grüne Tests, aber alle mit Arbeiten, die einen Stoff nennen.
- **Test-Tor:** rot, weil sein Lauf gleichzeitig mit meiner Suite baute (MSB3883) – derselbe Befund wie in Sprint 195.
- **Tor Tage-nur-in-LearningDay:** fing `ToString("dd.MM.")` und `.Date` in meinem neuen Code – richtig, nachgezogen.

**Der eine Mechanismus (Prosa):** in [pm-loop](../.claude/skills/pm-loop/SKILL.md) Step 4: eine neue Regel über Daten, die der Vater einträgt, bekommt
vor ihren Tests einen Trockenlauf an den echten Zeilen der Kopie. Kein Tor – ob eine Fixture „realistisch“ ist, ist nicht mechanisch zu fassen.
Kein neues Tor, also kein Halt aus diesem Grund.

**Bewusst ungedeckt:** Die Schwäche eines Fachs aus einer einzigen Antwort (1 von 1 falsch = 100 %) ordnet den Rang; eine Mindestzahl
fehlt. Der Konstruktor-Test prüft nur `DailyPlanner`.

## Offene Roadmap (Stand nach Sprint 197)

Siehe `docs/backlog/`. Beim Vater liegt B-1034. Danach B-736 (Pflicht aus der Tagesaufgabe, mit Entscheidung 5 aus B-1033) und B-737.
