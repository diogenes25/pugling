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
