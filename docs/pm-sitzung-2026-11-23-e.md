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
