---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf E – Aufstieg ab dem nächsten Lerntag

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den Aufbau-Weg weitertragen: nach B-848 (die Position spielt ihre Stufe) jetzt der Aufstieg (B-849).

Auftrag: „starte nachtlauf“ – gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–11.
Ausgangsstand `main` `c46f1de5`, sauber. Story-Ids dieses Laufs: ab B-880.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/` (jüngster vom 2026-08-02). Der rote Faden des Vorlaufs ist der Aufbau-Weg; er
nennt B-849 als Nächstes. B-849 ist `geschaetzt` mit vom Nutzer bestätigten Entscheidungen (E3–E6 am 2026-11-19, E8 am
2026-11-22) – baubar nach Freigabe 8. B-702 hängt an B-849 (und widerspricht ihr in einem Punkt, siehe unten), B-841 ist
Oberfläche über beides.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 122 | B-849 | Aufstieg nach bestandenem Test, gespielt ab dem nächsten Lerntag |

## Sprint 122 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn besteht den Test seiner Vorstufe, spielt den Rest des Tages weiter die alte Stufe und bekommt ab
dem nächsten Lerntag (deutsche Zeit) die nächste Stufe – ohne dass der Aufstieg Münzen bucht oder ein Vorstufen-Test die
Pflicht erfüllt.
**Umfang:** B-849 (M, backend, Migration).
**Entwickler-Brief:** Quelle der Wahrheit bleibt die Naht `PositionPlayService.PlayedExercise` – sie bekommt den Tag
(`PlayedExercise(pos, day)`), und `PlayedBuildLevel(pos, day)` ist die eine Stelle für „welche Stufe an diesem Tag“: vor
`BuildLevelEffectiveOn` die Stufe darunter auf dem Weg. Der Tag einer Runde ist der Tag, an dem sie begann
(`session.Day`, `attempt.Day`), nicht „jetzt“ – eine Runde über Mitternacht bleibt auf ihrer Übung. Aufstieg nur im
Abschluss des Versuchs (`Submit`), aus der **geprüften** Stufe (`TestAttempt.BuildLevel`, `PromotedBuildLevel`), nicht aus
der Stufe der Position – das macht ihn idempotent. Ziel-Filter in `IsGoalMetAsync`. Migration `BuildLevelPromotion`.
Testweg: `AufbauStufeAufstiegTests` (Berlin-Host, eingefrorene Uhr), `AufbauStufeAufstiegMigrationTests`.

## Iteration 122 — umgesetzt

**Backend (API-First):** Die Naht ist der Umbau: `PlayedExercise`, `PlayedExerciseId`, `BuildPathOf`, `LearningCheckOf`,
`TestTargetStage` und die Instanz-Helfer (`ItemsOfAsync`, `PoolSize`, `DailyDose`, `IntroducedCardsAsync`,
`ProgressForAsync`, `RemediationRoundAsync`) nehmen den Tag ausdrücklich – der Compiler hat alle 35 Aufrufer gelistet,
jeder ist einzeln entschieden (Übung: `session.Day`; Test: Start-`day`, danach `attempt.Day`; Fortschritt/Abrechnung:
der ausgewertete Tag; Vater-Sichten, Bericht, Rückmeldung: heute). Neu: `PlayedBuildLevel`, `PathLevels`,
`ExaminedBuildLevel`, `PromotedBuildLevel`, `AttemptsOnLevel`. Aufstieg in `PositionTestsController.Submit` im selben
`SaveChanges` wie der Abschluss. Migration `BuildLevelPromotion` (zwei nullable Spalten, Bestand der Versuche gestufter
Positionen gefüllt). Kein Vertragsbruch, kein Frontend.

**Verifikation:** Build der Solution grün; Suite 2170/2170 (Release, vorher 2149); neue Klassen
`AufbauStufeAufstiegTests` (21) und `AufbauStufeAufstiegMigrationTests` (1); Mutanten-Probe nach der letzten Änderung 22/22
rot am benannten Test (Tabelle im Verlauf von B-849); E2E `aufbau-weg-editor` + `test-durchgefallen-ueben` 2 passed
(Ausweichports 5201/5174, :5200 ist der Heimnetz-Server); Migration an einer Kopie der echten DB geprobt: 118 Versuche,
208 Positionen, neue Felder leer. `dotnet format --verify-no-changes` meldet nur `PositionPlayService.cs:617–640` – Zeilen,
die dieser Diff nicht berührt (CRLF/LF-Artefakt, siehe Gedächtnis).

**Review:** `pugling-reviewer` – keine 🔴, drei 🟡, alle behoben mit Test (Fehlerzähler **3**): überholter Versuch nach
Stufenwechsel (→ eigene Story B-880, entgangen bei B-848), Aufstieg durch ungewerteten Test unter „nur getippt“, Bericht
verlor Ziel-Tests von vor dem Weg. 🟢 ohne Zählung: Migrations-Kommentar präzisiert; zurückgestellt nach B-702 (ein
Schritt Historie), B-841 (wartender Aufstieg für den Vater unsichtbar), im Verlauf von B-849 (Rückmeldung löst „heute“
auf, `UsedBy` liest die rohe Stufe).

## Runde 122 — Abnahme

- **Sohn — abgenommen (maschinell):** „Ich bestehe den Test der leichten Stufe und spiele heute noch sie zu Ende – morgen
  kommt die nächste. Eine Runde, die ich um zehn vor zwölf anfange, bleibt, wie sie war.“ Gegangen über die HTTP-Pipeline
  mit Kind-Login auf dem Berlin-Host (eingefrorene Uhr: 23:30, 23:45, 00:10, 00:30, nächster Tag). Eine UI-Änderung gibt es
  nicht; die Sohn-App liest Titel und Typ aus `PositionStatus`, das ab dem Wirktag die neue Stufe trägt.
- **Vater — abgenommen (maschinell, API):** „Meine Stufe gilt sofort und schlägt einen wartenden Aufstieg; mein
  Probe-Test hebt nichts; der Bericht zählt Treffer der Stufe, die mein Kind spielt.“ Nicht sichtbar für ihn: dass ein
  Aufstieg auf morgen wartet – mit B-841.
- **Creator — Regressionszeuge:** E2E `aufbau-weg-editor` grün; kein Pfad des Creators im Diff außer der Lösch-Sperre
  (`UsedBy`, unverändert).
- **Offen für einen Menschen:** Migration `BuildLevelPromotion` beim nächsten Heimnetz-Start (Startskript sichert vorher).

## Retrospektive Sprint 122

**Nachschau:** B-848 (Sprint 121), Prüfpunkte im Verlauf dort: (a) jeder Loader einer Position, der in die Naht läuft,
lädt `Exercise.Prerequisites` – per Compiler-Liste der 35 Aufrufer und 2170 Tests ohne Wurf; (b) die Sperre
`InPlayTodayAsync` gegen offene Versuche und Runden früherer Tage – **Fund**: beide bleiben beantwortbar und werten nach
einem Stufenwechsel gegen die neue Übung. Als eigene Defekte abgelegt: B-880 (Versuch, im Sprint behoben, `entgangen_bei:
[B-848]`) und B-881 (Runde, offen). `nachgeschaut` an B-848 gesetzt. Index: Nachgeschaut 554 von 587.

**Was die eigenen Tore durchgelassen haben:** Alle drei 🟡 haben eine Form: eine **zweite Lesart derselben Frage**, die
neben einer schon vorhandenen entstand, statt sie zu teilen. Der Aufstieg las „bestanden“ ohne die Zulassung, die das Ziel
für denselben Versuch schon hat (`!RequireTypedTest || Graded`); der Bericht bekam einen eigenen Stufen-Filter neben dem der
Rückmeldung; der überholte Versuch ist dieselbe Frage „gehört der Versuch noch zur Übung?“, die B-848 nur für heute stellt.
Die Mutanten-Probe konnte keinen davon finden – sie prüft, ob jede gebaute Regel trägt, nicht, ob eine Regel fehlt.

**Vorschlag (nicht gelandet, Freigabe 3):** (DC) In `pm-loop` Step 4 neben „Put a rule at the sink“: *Eine neue Wirkung
eines Testergebnisses (Aufstieg, Abstieg, Prämie) liest dieselbe Zulassung wie das Ziel, über dasselbe Prädikat –
`grep` auf `t.Passed` und je Treffer die Frage, welche Bedingung dort mitläuft.* Ein Tor wäre ein Wächter, der jede
Abfrage auf `TestAttempt.Passed` außerhalb eines gemeinsamen Prädikats rot macht; das zu messen kostet einen eigenen
Schritt.

**Bewusst ungedeckt:** B-881 (Übungsrunde früherer Tage), ein Schritt Historie in `PlayedBuildLevel` (wird mit B-702
relevant).

**Zur Entscheidung (Produkt):** B-702 Entscheidung 2 („Test der aktuellen Stufe erfüllt das Ziel“, stellvertretend) und
B-849 E6 („Vorstufen-Test zählt nicht“, vom Nutzer bestätigt) widersprechen sich; gebaut ist E6. Dazu: Setzt ein Aufstieg
eine auf der Vorstufe beendete Nacharbeit (`CompletedOn`) zurück – also wird „bis erledigt“ auf der neuen Stufe wieder
Pflicht mit Malus? Beides in B-702 notiert. B-702 ist bis dahin nicht baubar.

## Runde 2 — Auswahl nach Sprint 122

Kein Halt-Grund: Fehlerzähler 3, die Retro hat nur vorgeschlagen. Im Faden baubar bleiben: B-881 (Defekt aus der
Nachschau, autonom grillbar), B-841 (Oberfläche, `geschaetzt`). B-702 ist gesperrt, bis der Widerspruch zu B-849 E6
entschieden ist. B-881 zuerst: dieselbe Frage wie B-880, solange das Prädikat frisch ist.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 123 | B-881 | Überholte Übungsrunde nach Stufenwechsel |

**Refinement:** B-881 am Code belegt (fünf Spielpfade ohne Prüfung, `InPlayTodayAsync` nur für heute, keine
Fortsetzung alter Runden in der Sohn-App → P3), autonom gegrillt (zwei Entscheidungen: Stempel wie beim Versuch über ein
gemeinsames Prädikat; `End`/`Get` bleiben offen), geschätzt S.

## Sprint 123 — Ziel & Umfang

**Sprint-Ziel:** Eine Übungsrunde, die eine Stufenänderung des Vaters überholt hat, spielt und wertet keine Karte mehr –
der Lernstand der neuen Stufe bleibt unberührt.
**Umfang:** B-881 (S, backend, Migration).
**Entwickler-Brief:** Quelle der Wahrheit ist `PositionPlayService.IsOvertaken(pos, stamped, day)` – eine Frage für
Runde und Versuch (B-880 wird darauf umgestellt). Stempel `PracticeSession.BuildLevel` im `Start`; Prüfung in `Cards`,
`ReshuffleImage`, `Next`, `Easier`, `Review`. Migration `PracticeSessionBuildLevel` hinter `BuildLevelPromotion`.

## Iteration 123 — umgesetzt

**Backend:** `PracticeSession.BuildLevel` (Stempel im `Start`), `PositionPlayService.IsOvertaken` als eine Frage für Runde
und Versuch (B-880 umgestellt), Prüfung in `Cards`, `ReshuffleImage`, `Next`, `Easier`, `Review`; die Nacharbeit setzt
keine überholte Runde fort. Migration `PracticeSessionBuildLevel`. Kein Vertragsbruch, kein Frontend.

**Verifikation:** Suite 2173/2173 (Release); Mutanten-Probe nach der letzten Änderung 10/10 rot am benannten Test (Tabelle
in B-881); E2E 6 passed (`nacharbeit`, `nacharbeit-bis-erledigt`, `ueben-ohne-leitner`, `uebung-abbruch`,
`aufbau-weg-editor`, `test-durchgefallen-ueben`; Ausweichports); Migrationen `BuildLevelPromotion` +
`PracticeSessionBuildLevel` an einer Kopie der echten DB: 226 Runden, 118 Versuche, alle ohne Stempel.

**Funde (Fehlerzähler 2):** eigener Fund der Probe – die erste Zusicherung „404“ auf `ReshuffleImage` war schon ohne
Prüfung erfüllt (R2 grün), jetzt verlangt der Test den Text der überholten Runde; Review 🟡 – die Fortsetzung der offenen
Nacharbeitsrunde gab eine überholte Runde zurück und sperrte das Kind für den Tag aus, behoben mit Test. 🟢 ohne Zählung:
Füllung der Migration für Runden am Tag eines wartenden Aufstiegs als hingenommen kommentiert.

## Runde 123 — Abnahme

- **Sohn — abgenommen (maschinell):** „Hat Papa meine Stufe umgestellt, spielt die alte Runde nicht weiter – ich fange neu
  an und lande auf der neuen Stufe, auch in der Nacharbeit.“ HTTP-Pipeline mit Kind-Login.
- **Vater — Regressionszeuge mit Auslöserrolle:** setzt die Stufe über die API; sein Pfad ist unverändert.
- **Creator — Regressionszeuge:** E2E `aufbau-weg-editor` grün.
- **Offen für einen Menschen:** Migrationen `BuildLevelPromotion` und `PracticeSessionBuildLevel` beim nächsten
  Heimnetz-Start (zusammen an einer Kopie geprobt).

## Retrospektive Sprint 123

**Nachschau:** B-849 und B-880 (Sprint 122), Prüfpunkte im Verlauf je Story: jeder Leser von `TestAttempt.Passed` per grep
(Ziel gefiltert, Aufstieg mit Ziel-Zulassung, `MetricsService` zählt Vorstufen-Tests für Missionen – nicht Pflichtziel, kein
Befund); `PromotedBuildLevel` bei fehlender Vorstufe nachgerechnet (kein Aufstieg, kein Wurf); alle Aktionen mit
Versuchs-Id im `PositionTestsController` (drei spielende prüfen, `Get` darf zeigen). Kein Befund, `nachgeschaut` gesetzt.
Index: Nachgeschaut 556 von 588.

**Was die eigenen Tore durchgelassen haben:** (1) Eine Zusicherung, die der Ausgangszustand schon erfüllt – genau die Regel
aus der Root-`CLAUDE.md`; diesmal hat die Probe sie gefunden, weil je Pfad eine eigene Mutante lief (bei einer Mutante für
alle fünf wäre sie durchgerutscht). (2) Ein Pfad, der das abgewiesene Objekt **wieder herausgibt**, statt es zu spielen:
die Fortsetzung im `Start`. Ich hatte alle Pfade gesucht, die Karten einer Runde lesen – nicht die, die eine Runde
zurückgeben.

**Vorschlag (nicht gelandet, Freigabe 3):** (DD) In `pm-loop` Step 5 zur Mutanten-Regel: *Eine Prüfung, die an mehreren
Pfaden steht, bekommt je Pfad eine Mutante* (eine Sammel-Mutante beweist nur, dass irgendein Pfad trägt). Und zu Step 4:
*Wer ein Objekt neu abweist, sucht auch jeden Pfad, der es wieder herausgibt (fortsetzen, „offene Runde“, Liste),
nicht nur die, die es benutzen.*

**Bewusst ungedeckt:** `Heartbeat` und `Shown` auf einer überholten Runde (zählen Sekunden bzw. setzen einen Zeitstempel,
geben nichts heraus – vom Reviewer als unschädlich bestätigt).

## Runde 3 — Auswahl nach Sprint 123

Kein Halt-Grund (Fehlerzähler 2, Retro nur Vorschlag). Im Faden baubar: B-841 (`geschaetzt`, Wunsch, Entscheidungen 1, 3,
4 aus B-704 vom Nutzer bestätigt; `wartet_auf` B-849 ist erfüllt). Was B-704 nicht entscheidet – einen auf morgen wartenden
Aufstieg anzeigen, die Stufe im Bearbeiten ändern – bleibt draußen (B-882, Idee).

| Sprint | Stories | Thema |
| --- | --- | --- |
| 124 | B-841 | Aufbau-Weg in der Oberfläche: Start-Stufe, Stufenband, Stufe in der Auswertung |

## Sprint 124 — Ziel & Umfang

**Sprint-Ziel:** Der Vater legt im Positionsformular eine Position mit Start-Stufe an, ohne über Stufen-Zahlen oder einen
abgelehnten Rhythmus zu stolpern, und der Sohn sieht im Kopf seiner Runde „Stufe n von m“.
**Umfang:** B-841 (M, frontend).
**Entwickler-Brief:** Quelle der Wahrheit ist der Vertrag (`CreatePositionDto.currentBuildLevel`, `PositionResponse.build`,
`PositionStatus.build`, `Report.build`), kein Backend-Umbau. Feld nur bei Vorstufen (Unit-Liste, `buildTargetExerciseId`);
Rhythmus-Regel des Servers (kein Tages-/Wochenziel auf dem Weg) im Formular gespiegelt. Testweg: Vitest für Regeln und
Band, E2E `aufbau-stufen.spec.ts` als Rollengang.

## Iteration 124 — umgesetzt

**Frontend:** `StartLevelField` + `startLevelOptions`, `cadencesFor(…, staged)`, `withStaged` (verdrängtes Ziel kommt
zurück), `aufbauStufeText` in Zeile und Report, `useRundenKopf` (Buchbezug + Stufenband) in Üben und Test, i18n
`pfad.buildLevel`, Feldhilfe `startBuildLevel`. Kein Backend.

**Verifikation:** Vitest 1383/1383, `tsc -b` grün, Produktions-Build grün; Mutanten-Probe nach der letzten Änderung 15/15 rot
(10 über Vitest, 5 über die E2E-Spec; Tabelle in B-841); E2E 13 passed über 8 Specs (Ausweichports).

**Review:** `frontend-reviewer` – keine 🔴, zwei 🟡 behoben (Fehlerzähler **2**): verdrängtes Ziel blieb „frei“ nach Wegfall des
Wegs (still pflichtfreie Position); zwei Bedeutungen von „Stufe“ auf einem Bildschirm (Katalog-Stufe im Feld, Platz auf dem
Weg in Zeile und Band). 🟢 notiert in B-841 bzw. B-882.

## Runde 124 — Abnahme

- **Vater — abgenommen (E2E):** „Das Feld kommt nur, wo es Vorstufen gibt; es spricht dieselbe Sprache wie die Zeile
  (‚Stufe 1 von 2‘); und wenn ich den Weg wieder wegnehme, steht mein Wochenziel wieder da.“ Offen für ihn: ein wartender
  Aufstieg und die Stufe im Bearbeiten (B-882).
- **Sohn — abgenommen (E2E), Gerätecheck offen:** „Oben steht, auf welcher Stufe ich bin.“ Ob die Pille neben Buchbezug und
  Tempo am Handy (360 px) sauber sitzt, sieht nur ein Mensch am Gerät.
- **Creator — Regressionszeuge:** kein Pfad des Creators im Diff; E2E `aufbau-weg-editor` lief in Sprint 122/123 grün.

## Retrospektive Sprint 124

**Nachschau:** B-881 (Sprint 123), Prüfpunkte im Verlauf dort: die 10 Aufrufer von `GetSession(` je Pfad eingeordnet (5
prüfen, 1 eigene Prüfung, 4 geben nichts heraus); `new PracticeSession` genau einmal (stempelt). Kein Befund,
`nachgeschaut` gesetzt. Index: Nachgeschaut: 557 von 589.

**Was die eigenen Tore durchgelassen haben:** Beide 🟡 waren Zustand, den das Formular **selbst** erzwang und dann vergaß
bzw. nicht erklärte: ein Rhythmus, den die Regel verschoben hatte, und eine Zahl, die zwei Skalen bedeutete. Die Probe konnte
keinen von beiden sehen – sie misst gebaute Regeln, und die Rückgabe des verdrängten Ziels gab es noch nicht.

**Vorschlag (nicht gelandet, Freigabe 3):** (DE) In `frontend/CLAUDE.md` ein Satz bei den Formular-Regeln: *Setzt ein Feld
den Wert eines anderen zwangsweise (eine Regel des Servers im Formular gespiegelt), nimmt es die Änderung zurück, wenn die
Bedingung wegfällt – sonst bleibt eine Wahl stehen, die niemand getroffen hat.*

**Bewusst ungedeckt:** der Ladehinweis des Stufenfelds (ein schneller Vater legt eine Position ohne Weg an); der Gerätecheck
des Bands.

## Runde 4 — Auswahl nach Sprint 124

Der Aufbau-Faden ist ausgeschöpft: B-702 wartet auf eine Entscheidung (Widerspruch zu B-849 E6, Nacharbeit nach dem
Aufstieg), B-882 ist ein ungegrillter Wunsch. Autonom grillbar bleiben B-863 (Aufräumen) und B-860/B-649. B-863 als
eigener kleiner Sprint; B-860 und B-649 siehe Abschluss.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 125 | B-863 | Code-Doku auf Englisch im Testprojekt |

## Sprint 125 — Ziel & Umfang

**Sprint-Ziel:** Ein Leser des Testprojekts trifft keine deutsche Kommentar-Prosa mehr an – Zitate deutscher Produkttexte
ausgenommen –, und kein Test verhält sich anders.
**Umfang:** B-863 (S, backend, nur Kommentare). Refinement: nachgezählt, zwei Entscheidungen (alles übersetzen, kein Tor).

## Iteration 125 — umgesetzt

Zwölf Dateien in `Pugling.Api.Tests` übersetzt, nur Kommentare (gegen HEAD ohne Kommentare verglichen). Suite 2173/2173
(Release); nach den Nachträgen aus dem Review die betroffenen Klassen 199/199. Review `pugling-reviewer`: sinngetreu; fünf
übersehene Stellen und ein Glossar-Verstoß („curriculum“) nachgezogen – ohne Zählung (nur Kommentar, Freigabe 11).

## Runde 125 — Abnahme

Kein Rollenpfad berührt: alle drei Rollen sind Regressionszeugen über die grüne Suite. B-863 abgenommen (`617f50fd`).

## Retrospektive Sprint 125

**Nachschau:** B-841 (Sprint 124), Prüfpunkte im Verlauf dort: jeder Leser von `currentBuildLevel`/`build` im Frontend;
`staged` im Bearbeiten gegen die Server-Regel. Kein Befund, `nachgeschaut` gesetzt. Index: Nachgeschaut 558 von 590.

**Was die eigenen Tore durchgelassen haben:** Mein Inventar war eine Suche nach deutschen Funktionswörtern; kurze Zeilen
ohne solche Wörter („Basis 7 × Meilenstein 1“) fielen durch, der Reviewer fand fünf. Bei einer Aufräum-Story ist das
Inventar der eigentliche Beleg.

**Kein neuer Mechanismus, begründet:** Ein Tor über Kommentarsprache träfe die erlaubten Zitate und bräuchte eine
Ausnahmeliste je Zitat (Entscheidung 2 in B-863); der Reviewer-Blick hat die Lücke in einem Durchgang geschlossen.

## Abschluss des Laufs

**Halt nach Sprint 125, begründet:** kein weiteres Thema autonom baubar. B-702 braucht eine Produktentscheidung (siehe
Retro Sprint 122); B-882 ist ein ungegrillter `Wunsch`. B-860 (`Defekt`) hat der Lauf C bewusst dem Dialog überlassen (Wahl
zwischen Schreibsperre und DB-Regel), und B-649 (`Aufräumen`) entfernt bzw. verschiebt Einträge der echten DB und ändert den
Vertrag der Übersetzungsübung – das entscheidet hier niemand nachts. Fehlerzähler je Sprint: 3, 2, 2, 0 – keine Halt-Grenze
erreicht.

**Ergebnis:** abgenommen B-849, B-880 (Sprint 122), B-881 (123), B-841 (124), B-863 (125); neu B-882 (Idee). Commits auf
`main`, **ungepusht**: `2bf20d88`, `8f2c549a`, `e5c2430c`, `e618f94a`, `34d6becc`, `a728884d`, `617f50fd` und dieser.

**Zum Umstoßen am Morgen:**

1. Ein Probe-Test des Vaters (`BySupervisor`) hebt die Stufe nicht (B-849, Abweichung b).
2. Ein Aufstieg zählt nur, wo er das Ziel auch erfüllen könnte: unter „nur getippte Tests“ hebt ein selbst eingeschätzter
   Test nicht.
3. Eine von einer Stufenänderung überholte Runde bzw. ein überholter Versuch antwortet 404 („belongs to a level the position
   no longer plays“) statt weiterzulaufen (B-880/B-881).
4. Die Start-Stufe im Formular ist nach dem Platz auf dem Weg beschriftet („Stufe 1 von 2 · erkennen“).

**Zur Entscheidung (Produkt):** B-702 Entscheidung 2 gegen B-849 E6; Nacharbeit (`CompletedOn`) nach einem Aufstieg; B-882
(wartenden Aufstieg zeigen, Stufe im Bearbeiten). Retro-Vorschläge (DC), (DD), (DE).

**Menschlicher Check offen:** Migrationen `BuildLevelPromotion` und `PracticeSessionBuildLevel` beim nächsten
Heimnetz-Start (zusammen an einer Kopie der echten DB geprobt, Startskript sichert vorher); das Stufenband am Handy.

## Offene Roadmap (priorisiert)

Durable Liste: [docs/backlog/](backlog/INDEX.md). Als Nächstes im Dialog: B-702 (nach der Entscheidung baubar, hebt die
Kadenz-Sperre gestufter Positionen), dann B-882 und B-703; B-860 und B-649 brauchen je eine Wahl.
