---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf C – Nacharbeit aus der Nachschau von B-730 und B-660

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die sieben offenen Defekte aus den Nachschauen der letzten Nacht schließen, Sprint für Sprint.

Auftrag: „starte einen nachtlauf“ – gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10.
Ausgangsstand `main` `f41d035d`, sauber; keine parallele Pugling-Sitzung aktiv (geprüft per Sitzungsliste). Story-Ids
dieses Laufs: ab B-860.

## Runde 1 — Auswahl

Kein Befund unter `docs/anmerkungen/` neu. Der Backlog trägt 101 offene Stories; autonom grillbar sind nur `Defekt`/
`Aufräumen`, und offen auf `idee` liegen genau sieben Defekte, alle aus den Nachschauen vom 2026-11-22: B-842/843/844
(B-730, Lerntag), B-845/846/847 (B-660, „Noch einmal“) und B-820 (B-659, Kür). Die geschätzten `Wunsch`-Stories der
B-644-Kette (B-731 ff.) bleiben liegen: B-731 baut auf fünf unbestätigten Entscheidungen von B-730 (Gedächtnis-Notiz
Lauf A), B-701/B-703 tragen offene Fragen aus Lauf C vom 2026-11-22.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 117 | B-842, B-843, B-844 | Lerntag: was B-730 versprach, auch an den Rändern |
| 118 | B-845, B-846, B-847 | „Noch einmal“: Text und Belege von B-660 |
| 119 | B-820 | Laufende Nacharbeit auf Kür hält die Serie |

## Sprint 117 — Ziel & Umfang

**Sprint-Ziel:** Jede Zeitangabe, die Sohn oder Vater rund um Mitternacht und in einer Umstellungswoche sehen, folgt dem
deutschen Lerntag – und das Tor fängt jede Form, die es verspricht.
**Umfang:** B-842, B-843, B-844 (alle XS–S, backend, keine Migration).
**Entwickler-Brief:** Quelle der Wahrheit bleibt `LearningDay` (neu: `SameTimeDaysEarlier`). Tests zuerst in
`TagesbegriffTests` (Berlin-Fabrik), Tor in `ConventionGuardTests` auf das ganze API-Projekt mit Selbsttest. Reihenfolge:
Tor gegen den Ausgangsstand (muss B-844 melden) → B-844 → B-842.

## Iteration 117 — umgesetzt

- **Backend:** `LearningDay.SameTimeDaysEarlier` (Wanduhr minus Kalendertage, übersprungene Frühjahrs-Uhrzeit +1 h);
  `WalletService.WeekCoinsAsync` schneidet die Vorwoche damit; der Titel der Schwachwort-Wiederholung trägt
  `learningDay.DayOf(now)`.
- **Tor:** `Tage_Entstehen_Nur_Im_Lerntag` über das ganze API-Projekt (Filter auf dem relativen Pfad), neue Formen
  (`.Date`, `new DateOnly(Y,M,D)`, `FromDayNumber`, `ToDateTime(default)`, Datumsformat auf Zeitpunkten, `DayOfWeek` auf
  UTC-Uhren), Ausnahmen als `(Datei, Fragment)`; Selbsttest `Lerntag_Tor_Faengt_Jede_Form` (18 Fälle).
- **Verifikation:** rote Proben 3/3 (Tagesbegriff) und 1/1 (Tor gegen den Stand vor B-844); Mutanten M1–M13 alle rot;
  Backend 2126/2126 (Release, nach der letzten Änderung). Commit `e3f51ee7`.
- **Review** (`pugling-reviewer`): kein 🔴, ein 🟡 (Ausnahme am zweiten Doppelpunkt abgeschnitten → Tupel; **Zähler
  1/5**), sieben 🟢 – sechs behoben, einer (eine Umstellung um eine Stunde angenommen) im Kommentar benannt; alle ohne Zählung.

## Runde 117 — Abnahme

- **Sohn — abgenommen:** „In der Woche mit Zeitumstellung vergleiche ich mich wirklich mit Sonntagmittag, nicht mit 11 Uhr.“
  Gegangen maschinell (Integrationstests über die HTTP-Pipeline mit eingefrorener Uhr in der Umstellungswoche – ein
  laufender Server hat diese Uhr nicht).
- **Vater — abgenommen:** „Die Wiederholung von 0:30 Uhr heißt nach dem neuen Tag.“ Ebenso maschinell
  (`Schwachwort_Wiederholung_Traegt_Den_Lerntag_Im_Titel`).
- **Creator — Regressionszeuge:** volle Suite grün; keine Creator-Fläche im Diff.

## Retrospektive Sprint 117

**Nachschau:** B-700 und B-840 (Lauf C vom 2026-11-22), `pugling-reviewer`, nur gelesen. B-700: Prüfung vor jedem
Schreibweg, kein Umweg, Migration nur nullable Spalten – zwei 🟢 → [B-860](backlog/B-860-aufbau-weg-wettlauf-baut-kette.md)
(Wettlauf baut Kette/Kreis, `entgangen_bei: B-700`) und [B-861](backlog/B-861-deutscher-kommentar-im-uebungs-loeschen.md)
(deutscher `/* */`-Kommentar). B-840: kein Befund, Prüfpunkte im Verlauf. `nachgeschaut` an beiden gesetzt.

**Was die eigenen Tore durchgelassen haben:** Der Selbsttest des Tors prüft das Muster, nicht die Ausnahmeliste – ein
Eintrag mit Doppelpunkt wurde still verkürzt, und kein Test sah es. Gefunden hat es nur der Reviewer.

**Vorschlag (nicht gelandet, Freigabe 3):** (CY) Die Ausnahmelisten aller reflexiven Tore als `(Datei, Fragment)`-Tupel
plus ein Test, dass jede Ausnahme **genau eine** heutige Zeile trifft – fängt Verkürzung und verwaiste Ausnahmen. Der
zweite Fall mit `Split(':')` steht noch in `ConventionGuardTests.cs:654` (heute ohne Doppelpunkt im Fragment).

**Bewusst ungedeckt:** `.DayOfWeek` auf einem Zeitpunkt in einer Variablen; Standard-Datumsformate (`{x:d}`).

## Sprint 118 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn bekommt im leeren Üben nur versprochen, was das Bonus-Training wirklich zahlt, und der Weg
dorthin führt nachweislich in genau seine Übung.
**Umfang:** B-845 (frontend), B-846 (E2E), B-847 (Backend-Test) – alle XS, keine Migration.
**Entwickler-Brief:** Quelle der Wahrheit ist `ExtraTrainingOverview` (`dailyCoinCap`, `exercises`); die Sohn-App spiegelt
`capZero` aus `SohnBonus.tsx`. Die Spec bekommt eine zweite Pflicht als erste Position und die Rechen-Übung vorn im Vorrat;
der Backend-Test eine Theorie über die drei Formen. Reihenfolge beliebig (disjunkte Dateien), E2E auf Ausweichports
(5200 trägt den Heimnetz-Server).

## Iteration 118 — umgesetzt

- **Frontend:** `NochEinmal.tsx` wählt bei `dailyCoinCap === 0` die neuen Sätze `againLocked(Week)NoCoins` (de/en);
  `nochmal-nach-pflicht.spec.ts` mit `Farben` als erster Position und der Rechen-Übung vorn im Vorrat.
- **Backend-Test:** `Bonus_Runde_Laesst_Den_Kasten_Der_Position_Unberuehrt` als Theorie über drei Formen, voller
  Schnappschuss (alle Spalten, ungestempelte Tage, plan-weiter Lernstand).
- **Verifikation:** rote Probe B-845 2/2; Mutanten F1–F3, E1–E2, K1–K10 rot (K10 zunächst grün, weil der Aufbau keine
  plan-weiten Zeilen hatte – nach dem Nachtrag rot). Backend 2130/2130, Vitest 1361/1361, tsc sauber. Commit `08a421cb`.
- **Review:** `frontend-reviewer` – im Diff nur ein 🟡 am Story-Text (B-846, Text nachgezogen, ohne Zählung), drei Stellen
  außerhalb → [B-862](backlog/B-862-bonus-versprechen-bei-deckel-null-ausserhalb-noch-einmal.md); `pugling-reviewer` – ein 🟡
  (`??=`-Stempel unsichtbar, **gezählt, 1/5**), zwei 🟢 behoben.

## Runde 118 — Abnahme

- **Sohn — abgenommen:** „Wenn Papa den Bonus auf 0 stellt, verspricht mir die App vorher keine Münzen mehr, und der Knopf
  führt in meine Tiere, nicht in Farben oder Rechnen.“ Gegangen per E2E (7 Specs, frisch gestartete Server).
- **Vater — Regressionszeuge:** `bonus-deckel` (Deckel setzen, zurücksetzen) grün.
- **Creator — Regressionszeuge:** Suite grün, keine Creator-Fläche im Diff.

## Retrospektive Sprint 118

**Nachschau:** B-842, B-843, B-844 (Sprint 117), Selbst-Check – Ausnahmen des Tors gezählt, Aufrufer des neuen Helfers
gezählt, Grenzen nachgerechnet; kein Befund, `nachgeschaut` gesetzt.

**Was die eigenen Tore durchgelassen haben:** Zweimal ein Test, der einen Zustand prüfte, den der Aufbau gar nicht
herstellte – vorgefüllte Tage verdeckten `??=` (Reviewer), fehlende plan-weite Zeilen machten K10 grün (Mutanten-Probe).
Die Probe fing den zweiten Fall, weil die Mutante eine Wirkung auf **vorhandene** Zeilen hatte; die Leitlinie in
`CLAUDE.md` („eine Zusicherung, die den Ausgangszustand prüft …“) war bekannt und half beim Schreiben nicht.

**Vorschlag (nicht gelandet):** (CZ) In `pm-loop` Step 5 ein Satz: „Für einen Unverändert-Schnappschuss je Spalte eine
Mutante, die sie **erstmals** setzt (`??=`) und eine, die sie **ändert** – der Aufbau muss beide Lagen enthalten.“ Prosa,
weil kein Tor die Zielspalten eines Schnappschusses kennt.

**Bewusst ungedeckt:** Plural `_other` mit Deckel 0 (die Schlüssel existieren, i18n-Parität prüft sie).

## Sprint 119 — Ziel & Umfang

**Sprint-Ziel:** Stellt der Vater eine Nacharbeit „täglich, bis erledigt“ auf Kür, behält der Sohn jeden Tag, an dem er
sie erfüllt hat, in seiner Serie – und keinen Tag mehr.
**Umfang:** B-820 (XS→S, backend, keine Migration).
**Entwickler-Brief:** Quelle der Wahrheit ist die Spanne `DutyUntil`/`CadenceUntil` (B-659) neben `CompletedOn` (B-636);
der Schreiber ist `CloseDutyAsync`, die Leser `DutyCadenceOn`/`HasDutyOn` und die Tagesansicht.

## Iteration 119 — umgesetzt

- **Backend:** `CloseDutyAsync` schließt auch `UntilDone`; die Spanne endet spätestens an `CompletedOn`.
- **Verifikation:** rote Proben 2/2 und 1/1 (Begrenzung); Mutanten R1–R3 rot; Backend 2131/2131 (Release, nach der letzten
  Änderung). Commit `f294e804`.
- **Review** (`pugling-reviewer`): ein 🟡 – eine **beendete** Nacharbeit kam beim Freigeben als erfüllte Pflicht in die
  Tagesansicht zurück und hätte Truhe und Bonus-Training ohne Spielen geöffnet → Begrenzung (**gezählt, 1/5**); zwei 🟢
  (Story-Text, eine Serien-Zeile ohne Beweiskraft) nachgezogen.

## Runde 119 — Abnahme

- **Sohn — abgenommen:** „Papa hat meine Nacharbeit freigegeben, und mein Tag von heute bleibt in der Serie.“ Maschinell
  (Integrationstests mit Wechsel über die Vater-API) und E2E `nacharbeit`, `nacharbeit-bis-erledigt` (2 passed).
- **Vater — abgenommen:** „Freigeben verschenkt nichts: eine beendete Nacharbeit öffnet keine Truhe.“
  (`Beendete_Nacharbeit_Auf_Kuer_Endet_Am_Tag_Ihres_Endes`).
- **Creator — Regressionszeuge:** Suite grün.
- **Zum Umstoßen:** Entscheidung 1 hebt B-659 Entscheidung 2 („nur Daily/Weekly“) für die Nacharbeit auf.

## Retrospektive Sprint 119

**Nachschau:** B-845, B-846, B-847 (Sprint 118), Selbst-Check mit benannten Prüfpunkten (Bedingung gegen `capZero`,
Spec-Kopf gegen gemessene Mutanten, Schnappschuss gegen alle Spalten der Entität); kein Befund, `nachgeschaut` gesetzt.

**Was die eigenen Tore durchgelassen haben:** Der erste Fix war im Kleinen richtig und öffnete einen Nachbarweg – die
Tagesansicht liest die Spanne, nicht `CompletedOn`. Mein Brief nannte die Leser, ich habe sie aber nicht einzeln gegen
den neuen Wert gelesen; der Reviewer hat es getan. Genau das verlangt `pm-loop` Step 4 („grep its callers and re-read
each“) – die Regel stand da, sie wurde nicht befolgt.

**Vorschlag (nicht gelandet):** keiner neu – die Regel existiert; (CY)/(CZ) bleiben offen. Ehrlich: kein Mechanismus hätte
das gefangen, außer der gelesenen Aufruferliste.

## Sprint 120 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn liest nirgends im Bonus-Training ein Münzversprechen, das der Deckel heute nicht hält – weder
auf der Startseite noch in der Liste, über der Karte oder auf dem Ergebnisblatt.
**Umfang:** B-862 (S, beides, additiv) und B-861 (XS, Aufräumen) – beide aus Funden dieser Nacht (Review B-845,
Nachschau B-700). B-860 bleibt liegen (siehe Offene Roadmap).
**Entwickler-Brief:** Quelle der Wahrheit ist der Deckel des Servers: `ExtraRunResponse.CoinsLeftToday` für die Runde,
`ExtraTrainingOverview` für Liste und Teaser. Vorhandene Sätze (`capZero`, `capReached`, `vocabNoWorth`, `hintNoWorth`)
statt neuer.

## Iteration 120 — umgesetzt

- **Backend:** `CoinsLeftToday` am Start/Fortsetzen; sechs deutsche `/* */`-Blöcke übersetzt.
- **Frontend:** Hinweis, Listenzeile, Ergebnis-Vorschau (nur Formname am vollen Deckel, neue Schlüssel `formName*`) und
  Teaser (lädt die Übersicht, im Ladezustand ohne Satz).
- **Verifikation:** rote Probe Backend 2/2; Mutanten G1–G9, B1–B3 rot nach der letzten Änderung. Backend 2134/2134,
  Vitest 1371/1371, tsc sauber. Commit `635b68b4`.
- **Review:** drei 🟡 im Diff (Teaser-Blitzen, Gegenfall im Ladezustand, aufgebrauchter Deckel im Backend-Test) – alle
  behoben, **Zähler 3/5**; ein 🟡 außerhalb (Ergebnis-Vorschau) in B-862 genommen; deutsche `//`-Absätze in den Tests →
  [B-863](backlog/B-863-deutsche-kommentare-in-den-tests.md).

## Runde 120 — Abnahme

- **Sohn — abgenommen:** „Bei Deckel 0 sagt mir die Startseite gleich, dass es keine Bonus-Münzen gibt, und keine Karte
  verspricht mir 3 🪙.“ Gegangen per E2E (6 Specs, frische Server) für den Weg; der Deckel-0-Fall auf der Startseite nur
  auf Komponentenebene – **ein Mensch klickt das am Gerät nach** (Vater stellt Deckel 0, Sohn öffnet die Startseite).
- **Vater — Regressionszeuge:** `bonus-deckel` grün.
- **Creator — Regressionszeuge:** Suite grün.

## Retrospektive Sprint 120

**Nachschau:** B-820 (Sprint 119), Selbst-Check – alle Leser der Spanne gezählt, Filter der Tagesansicht gegen die
Begrenzung gelesen; kein Befund, `nachgeschaut` gesetzt.

**Was die eigenen Tore durchgelassen haben:** Zwei Tests, die einen Zwischenzustand statt des Ziels prüften (Teaser vor
der Antwort; Deckel ohne verdiente Münzen) – dieselbe Klasse wie in Sprint 118. Gefunden von Reviewern, nicht von der
Mutanten-Probe, weil ich die passenden Mutanten (falsch nur im geladenen Zweig; Deckel statt Rest) nicht geschrieben hatte.

**Vorschlag (nicht gelandet):** (DA) Für `useAsync`-Zweige: je Komponente ein Fall, der den **Ladezustand** zusichert, und
die anderen Fälle erst nach `await act(…)` – als Satz in `frontend/CLAUDE.md` neben der B-284-Regel. Stützt (CZ).

## Abschluss des Laufs

**Halt nach Sprint 120, begründet:** Kein Halt-Tor hat gegriffen (keine Retro hat ein Tor gelandet, kein Zähler über 5).
Offen und autonom grillbar sind noch B-860 (Defekt) und B-863 (Aufräumen). B-860 lasse ich bewusst liegen: der Fix
verlangt eine Wahl zwischen Schreibsperre um Prüfen+Speichern und einer Datenbank-Regel gegen Ketten – beides berührt jeden
Übungs-Schreibweg und ist eher eine Architektur- als eine „richtig herstellen“-Entscheidung. B-863 ist reine
Übersetzungsarbeit für einen nächsten Lauf.

**Ergebnis:** 9 Stories abgenommen – B-842, B-843, B-844 (117), B-845, B-846, B-847 (118), B-820 (119), B-861, B-862 (120);
4 neu (B-860, B-861, B-862, B-863 – davon B-861/B-862 im selben Lauf erledigt). Fehlerzähler je Sprint: 1, 1, 1, 3.
Commits auf `main`, **ungepusht**: `e3f51ee7`, `54f354b7`, `08a421cb`, `15921baf`, `f294e804`, `de974cf0`, `635b68b4` und
der Abschluss-Commit.

**Zum Umstoßen am Morgen:**
1. B-820 Entscheidung 1 hebt B-659 Entscheidung 2 („nur Daily/Weekly“) für die Nacharbeit auf.
2. B-862 Entscheidung 3: der Startseiten-Teaser lädt die Bonus-Übersicht selbst (ein Abruf mehr) statt eines Feldes an
   der Tagesübersicht.
3. B-843: `.Date` ist jetzt überall im API-Projekt verboten (Ausnahmen benannt) – ein künftiges DTO-Feld `Date` braucht
   eine Zeile in der Ausnahmeliste.

**Retro-Vorschläge zur Entscheidung:** (CY) Ausnahmelisten als Tupel + „jede Ausnahme trifft genau eine Zeile“; (CZ)
Schnappschuss-Tests mit Mutanten „erstmals setzen“ und „ändern“; (DA) Ladezustand je `useAsync`-Komponente.

**Menschlicher Check offen:** Deckel 0 → Sohn-Startseite am Gerät (B-862).

## Offene Roadmap (priorisiert)

Durable Liste: [docs/backlog/](backlog/INDEX.md). Für den nächsten Lauf: B-863 (Aufräumen, mechanisch), danach B-860 im
Dialog (Wahl Sperre vs. Datenbank-Regel). Die B-644-Kette (B-731 ff.) wartet weiter auf die Bestätigung der fünf
B-730-Entscheidungen.
