---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Abstieg ohne Malus auf dem Aufbau-Weg

**Datum:** 2026-11-23  ·  **Moderation:** PM (im Dialog mit dem Nutzer)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den Aufbau-Weg schließen: Wer an einer Stufe zweimal deutlich scheitert, steigt ab – ohne dass der Stick
dafür kassiert.

Auftrag: `/pm-loop` ohne genanntes Ziel. Ausgangsstand `main` `a782b4b2`, sauber.

## Runde 1 — Vorstellung & Feedback

Kein neuer Befund unter `docs/anmerkungen/` (jüngster Export 2026-08-14, beide Einträge erledigt). Die Rollenstimmen
kommen aus dem beobachteten Befund, aus dem der Faden stammt (B-535/B-536: Friedrich hing an zu schwerem Stoff, die
Pflicht war unerreichbar), und aus dem Abschluss von Nachtlauf E; ein neuer Rollengang vor der Auswahl fand nicht statt.

### Feedback Sohn (O-Ton, aus B-535/B-536)

- Nervt: „Ich schaffe die Stufe nicht, und es wird nie leichter.“ Der Aufstieg ist seit Sprint 122 gebaut, der Weg
  zurück nicht.
- Top-3: (1) leichter, wenn ich zweimal richtig daneben liege; (2) dafür nicht bestraft werden; (3) sehen, auf welcher
  Stufe ich bin (B-841, abgenommen).

### Feedback Vater (O-Ton)

- Stört: „Ein Kind, das an der Stufe scheitert, bleibt dort, bis ich es merke.“
- Top-3: (1) Abstieg automatisch, aber nicht auf Zuruf – gemessen; (2) der Stick bleibt, wo das Kind nur nicht übt;
  (3) sehen, seit wann es auf der Stufe steht (Entscheidung 3).

### Feedback Creator (O-Ton)

- Kein eigener Schmerz in diesem Faden; B-703 (Vorstufen per KI) steht als Nächstes für ihn.

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Warum jetzt |
| --- | --- | --- | --- | --- |
| P1 | B-702 Abstieg ohne Malus | S | backend | seit heute Abend durch zwei Nutzerentscheidungen baubar; schließt den Weg |
| P1 | B-703 Vorstufen per KI | M | backend | danach; Creator-Enabler |
| P2 | B-882 wartenden Aufstieg zeigen | — | frontend | `idee`, braucht Grillen |

**Roter Faden:** Der Aufbau-Weg ist erst fair, wenn er in beide Richtungen geht.

## Sprint 126 — Ziel & Umfang

**Sprint-Ziel:** Ein Kind, das zwei Tests seiner Stufe hintereinander unter 50 % schreibt, spielt ab sofort die
nächstleichtere Stufe – ohne dass der Abstieg Münzen bucht oder abzieht, und ohne dass ein knapp verfehlter Test, ein
Probe-Test des Vaters oder ein Test einer alten Stufe dazu beiträgt.
**Umfang:** B-702 (S, backend, Migration).
**Entwickler-Brief:** Quelle der Wahrheit ist der Abschluss des Versuchs (`PositionTestsController.Submit`), dieselbe
Stelle wie der Aufstieg; der Abstieg liest dieselbe Zulassung (Kind, `!RequireTypedTest || Graded`) über ein gemeinsames
Prädikat und die **geprüfte** Stufe (`TestAttempt.BuildLevel`), die der aktuellen gleichen muss. Neue Helfer
`PositionPlayService.DemotedBuildLevel` neben `PromotedBuildLevel`. Zähler `PlanPosition.FailedBuildTestsInRow` und
`CurrentBuildLevelSince`; jeder Stufenwechsel (Anlegen, Vater, Aufstieg, Abstieg) setzt beides. Wirkung sofort
(`BuildLevelEffectiveOn` = null, Nutzerentscheidung). Migration `BuildLevelDemotion` hinter `PracticeSessionBuildLevel`.
Testweg: `AufbauStufeAbstiegTests`, `AufbauStufeAbstiegMigrationTests`.

**Lesart im Dialog entschieden:** Der Abstieg gilt **sofort** wie die Stufe des Vaters, nicht ab dem nächsten Lerntag
wie der Aufstieg (Nutzer, 2026-11-23). Eine offene Runde der alten Stufe ist damit überholt (B-881).

## Iteration 126 — umgesetzt

**Backend (API-First):** `PositionPlayService.SetBuildLevel` ist der einzige Schreiber der Stufe (Anlegen, Vater-PATCH,
Aufstieg, Abstieg) und setzt Zählung und `CurrentBuildLevelSince` mit. `MoveBuildPath` trägt Aufstieg und Abstieg,
`MovesBuildPath` ist die gemeinsame Zulassung. `Submit` schließt einen Versuch über ein bedingtes Update in einer
Transaktion ab, damit ein doppelt abgegebener Versuch nicht zweimal zählt. Vertrag: trailing optional `BuildPath.Since`.
Migration `BuildLevelDemotion` (zwei nullable Spalten, keine Füllung). Kein Frontend.

**Verifikation:** Build der Solution grün; Suite 2189/2189 (Release, vorher 2173); neue Klassen `AufbauStufeAbstiegTests`
(13) und `AufbauStufeAbstiegMigrationTests` (1). Mutanten-Probe nach der letzten Änderung 20/20 rot am benannten Test
(Tabelle im Verlauf von B-702). E2E `aufbau-stufen`, `aufbau-weg-editor`, `test-durchgefallen-ueben` 3 passed (Ausweichports
5203/5176, frisch gestarteter Server). Migration an einer Kopie der echten DB: 208 Positionen, keine gestuft, neue Felder
leer. `dotnet format` meldet nur `PositionPlayService.cs:754–760`, Zeilen außerhalb des Diffs, in `HEAD` genauso
(CRLF/LF-Artefakt).

**Review:** `pugling-reviewer` – keine 🔴, keine 🟡 (Fehlerzähler **0**). 🟢 eingearbeitet: Test „Aufstieg setzt die
Zählung zurück“, AK 10 an `null` (= 0) angeglichen. 🟢 bewusst offen: ein Fehlversuch kann bei paralleler Abgabe zweier
Versuche verschiedener Tage verloren gehen (nur zugunsten des Kindes); `Since` bei Vater-Rückstellung während eines
wartenden Aufstiegs → Hinweis in B-882.

**Eigener Fund der Probe:** Die erste Fassung von AK 9 (zwei echte parallele Abgaben) war ohne die Sperre grün – der
TestServer erreicht das Rennfenster nie. Ersetzt durch einen Interceptor, der den Gewinner auf der Verbindung der Anfrage
spielt; danach rot ohne Sperre.

## Runde 126 — Abnahme

- **Sohn — abgenommen (maschinell):** „Wenn ich zweimal richtig danebenliege, komme ich sofort auf die leichtere Stufe,
  und es kostet nichts. Ein knapp verpasster Test zählt nicht dagegen.“ Gegangen über die HTTP-Pipeline mit Kind-Login auf
  dem Berlin-Host (eingefrorene Uhr, über zwei Lerntage); die Sohn-App liest Titel und Stufenband aus `PositionStatus`, das
  nach dem Abstieg sofort die leichtere Stufe trägt (E2E `aufbau-stufen` grün). Das Gefühl („fühlt sich das fair an?“)
  sieht nur ein Mensch – kein Gerätecheck nötig, es gibt keine neue Oberfläche.
- **Vater — abgenommen (maschinell, API):** „Meine Probe-Tests verschieben nichts, der Stick bleibt, wo das Kind nicht
  übt, und ich lese im Vertrag, seit wann es auf der Stufe steht.“ Nicht sichtbar für ihn in der Oberfläche: `Since`
  (gehört zu B-882).
- **Creator — Regressionszeuge:** E2E `aufbau-weg-editor` grün; kein Pfad des Creators im Diff.
- **Offen für einen Menschen:** Migration `BuildLevelDemotion` beim nächsten Heimnetz-Start (an einer Kopie geprobt,
  Startskript sichert vorher).

## Retrospektive

**Nachschau:** B-863 (Sprint 125): `grep` auf deutsche Funktionswörter in Kommentaren des Testprojekts – übrig sind nur
erlaubte Zitate von Produkttexten. Kein Befund, `nachgeschaut` gesetzt. Index: Nachgeschaut 559 von 591.

**Was die eigenen Tore durchgelassen haben:** Nichts bis in die Abnahme. Die Probe fand eine Zusicherung, die der
Ausgangszustand erfüllte (AK 9) – genau die Regel aus der Root-`CLAUDE.md`. Sie hielt, weil jede Bedingung ihre eigene
Mutante hatte. Von außerhalb der Schleife kam kein Fund.

**Kein neuer Mechanismus, begründet:** Das Tor hat gehalten. Zwei Werkzeug-Stolpersteine ohne Folgen: Ein `|` im
`--filter` des Testbefehls geht unter `cmd.exe` als Pipe durch (vor dem ersten Mutanten bemerkt und abgebrochen). Der
Abbruch hinterließ eine verwaiste Sperrdatei; das Skript benannte sie und den Ausweg. Beides hätte keine falsche Tabelle
erzeugt: Die Grundlinie wäre rot gewesen bzw. die Probe hätte gar nicht begonnen.

**Bewusst ungedeckt:** der verlorene Fehlversuch bei paralleler Abgabe zweier Versuche verschiedener Tage (siehe Review).

## Offene Roadmap (priorisiert)

Durable Liste: [docs/backlog/](backlog/INDEX.md). Der Aufbau-Weg geht jetzt in beide Richtungen. Als Nächstes: B-703 (der
KI-Creator erzeugt Vorstufen, Creator-Enabler, `geschaetzt`), dann B-882 (wartenden Aufstieg und `Since` zeigen, braucht
Grillen). B-860 und B-649 brauchen je eine Wahl.

## Konkreter Änderungsstand (für Review)

`PositionPlayService` (`SetBuildLevel`, `MoveBuildPath`, `MovesBuildPath`, `DemotedBuildLevel`, `BuildPathOf`),
`PositionTestsController.Submit`, `PlanPositionsController` (Anlegen, PATCH), `PlanPosition` (+2 Felder), `BuildPath.Since`,
Migration `20261123211614_BuildLevelDemotion`, Tests `AufbauStufeAbstiegTests`, `AufbauStufeAbstiegMigrationTests`,
`docs/openapi/v1.json`.
