---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Vater-Antworten im Lernstand]
status: geschaetzt
prio: P2
art: Defekt
groesse: M
wo: backend
migration: "ja – Spalte ItemReviewEvents.Day, aus At rückgefüllt"
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-26.md#iteration-193--umgesetzt
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1028 · Antworten des Vaters (Vorschau, Nachtrag) landen im Lernstand und in der Auswertung des Kindes

> **Verengt (Nutzerentscheidung 2026-11-26):** B-542 bleibt – eine Vater-Runde ist der Nachtrag und zählt. Gebaut wird nur:
> ein zurückdatierter Nachtrag erscheint in der Auswertung an **seinem** Tag, nicht heute.

Fund des `pugling-reviewer` zu B-733 (2026-11-26), bewusst aus Sprint 193 herausgelassen: das Sprint-Ziel hält ohne ihn, und die
Ursache liegt auf der Schreibseite, nicht in der Auswertung.

Ein Vater, der den Test einer Position probehalber macht (`TestAttempt.BySupervisor`) oder für das Kind übt, schreibt über
`ItemProgressService.RecordAsync(..., countsForMastery: true)` (`PositionTestsController.cs:480`, `PositionPracticeController.cs:756`)
eine `ItemReviewEvent`-Zeile auf das Kind und bewegt dessen `ItemProgress` (Box, Beherrschung, `IntroducedAt`). Die Zeile trägt
keinen Hinweis, wer geantwortet hat. Seit B-733 erscheinen diese Antworten zusätzlich als Antworten, Fehlerarten und Problemwörter
des Kindes und bleiben in einem abgeschlossenen Tag für immer stehen. Beim Nachtrag eines vergangenen Tags steht `At` auf jetzt,
die Sitzung auf dem alten Tag.

Zu klären: nichts schreiben, wenn kein Student antwortet, oder die Zeile kennzeichnen (Spalte, Migration) und filtern.

## User Story

Als Vater möchte ich, dass ein Nachtrag für Montag in der Auswertung am Montag steht – Antworten, Lernzeit und Pflicht –, damit
der Verlauf zeigt, wann mein Kind gelernt hat, und nicht, wann ich es eingetragen habe.

## Ist-Stand

- `ItemProgressService.RecordAsync` bekommt den Tag der Antwort (`today` = `session.Day` / `attempt.Day`), schreibt aber nur
  `At = jetzt` (`ItemProgressService.cs:44-56`).
- Die Auswertung ordnet Antworten nach `At` (`DailyEvaluationService.cs:135-136`, `:184-185`), Lernzeit nach `PracticeSession.Day`.
- Ein abgeschlossener Tag wird einmal gerechnet und nie wieder (`EvaluateMissingDaysAsync`); ein Nachtrag für einen schon
  gespeicherten Tag erscheint dort also nie – weder Antworten noch Lernzeit noch die erfüllte Pflicht.

## Entscheidungen

1. **Nutzer:** B-542 bleibt; kein Rollenfilter um `RecordAsync`. Kosten: ein Probetest des Vaters bewegt weiter die Kästen des
   Kindes – Ausprobieren läuft über die Vorschau.
2. **Jede Antwortzeile trägt ihren Lerntag** (`ItemReviewEvent.Day`, gesetzt in `RecordAsync` aus dem übergebenen Tag); die
   Auswertung (Tageszeile und Problemwörter) zählt nach `Day`. Rückfüllung in SQL: April bis Oktober `+2 hours`, sonst
   `+1 hours` – falsch nur in der letzten März- und Oktoberwoche für eine Antwort zwischen 0 und 1 Uhr Ortszeit. Kosten: eine
   Migration. Verworfen: `At` auf den alten Tag fälschen (die Uhrzeit wäre erfunden).
3. **Ein gespeicherter Tag wird neu gerechnet, wenn nach dem Speichern Belege für ihn eingingen** – eine Antwortzeile mit diesem
   `Day` und späterem `At`, eine später beantwortete Karte einer Runde dieses Tags (`ReviewEvent`, Nicht-Vokabel-Typen), eine
   Übungsrunde dieses Tags, die danach begann oder endete, ein danach abgegebener Test dieses Tags.
   Geprüft an der Lesestelle (`EvaluateMissingDaysAsync`), damit jeder Schreibweg erfasst ist. Kosten: zwei, drei Abfragen je
   Aufruf; ein neu gerechneter Tag liest die Pläne von jetzt (bekannte Grenze, schon dokumentiert). Verworfen: an jedem
   Schreibweg wieder öffnen (Regel je Pfad).

## Akzeptanzkriterien

1. Ein Nachtrag des Vaters für gestern erscheint mit seinen Antworten in der gestrigen Zeile, nicht in der heutigen.
2. War gestern schon gespeichert, wird die Zeile beim nächsten Lesen neu gerechnet und enthält den Nachtrag (Antworten und
   Lernzeit); ohne neue Belege bleibt eine gespeicherte Zeile unverändert.
3. Problemwörter eines Zeitraums ordnen eine Antwort nach ihrem Lerntag.
4. Die Migration füllt `Day` für Bestandszeilen; die echte DB startet (Kopie-Probe).

## Angriffsplan · Testweg

Spalte + Migration → `RecordAsync` setzt `Day` → Auswertung auf `Day` → Neurechnen in `EvaluateMissingDaysAsync`.
Tests in `TagesauswertungTests` (Nachtrag über die echte API mit `day`), Mutanten je AK, Migrationsprobe am Kopie-Server.

## Verlauf

- **2026-11-26** — Gegen Code und Kopie geprüft (PM-Sitzung Runde 5): Ein Probetest des Vaters (Position 119, Plan 15)
  ohne Antworten warf Friedrichs „Vive … !“ von Box 5 auf Box 1. Das ist jedoch **die abgenommene Entscheidung B-542**
  („eine Vater-Runde in der Position des Kindes ist der Nachtrag und zählt“, `PositionPracticeController.cs:681-687`,
  `VaterRundeZaehltFuersKindTests`); diese Story nannte sie nicht. Unstrittig defekt bleibt nur: ein **zurückdatierter**
  Nachtrag schreibt `At = jetzt`, die Auswertung ordnet nach `At` (`DailyEvaluationService.cs:135-136`, `:184-185`) und
  zeigt die Antworten am heutigen Tag. Reichweite: Vater-Web schickt nie `day` (`frontend/src/lib/api.ts:753`), an der
  echten DB null Vater-Testversuche. Als Frage an den Nutzer: B-542 umstoßen oder B-1028 auf die Tageszuordnung verengen.
- **2026-11-26** — Gebaut (Sprint 194): Spalte `ItemReviewEvent.Day` + Migration `ReviewEventDay` (Rückfüllung, Index
  `ChildId, Day`), `RecordAsync` setzt `Day` aus dem Tag der Runde/des Tests, `DailyEvaluationService` zählt nach `Day` und
  rechnet gespeicherte Tage mit späten Belegen neu (`ReopenedDaysAsync`, gerechnet außerhalb der Schreibtransaktion).
  `CreatedAt` jetzt aus dem `TimeProvider`.
- **2026-11-26** — Belege je AK-Fall (Tests in `TagesauswertungTests.cs`, `ReviewEventDayMigrationTests.cs`):
  - AK 1, Nachtrag über die echte API landet am Sonntag, heute unverändert → `Nachtrag_des_Vaters_fuer_gestern…`; M1 (`Day` aus der
    Uhr) rot, M2 (Tageszeile nach `At`) rot.
  - AK 2, gespeicherter Tag wird neu gerechnet → derselbe Test; M4 (nie neu rechnen) rot.
  - AK 2, je Belegart allein: späte Vokabelantwort → `Spaete_Antwort…` (M5 rot); späte Karte einer Runde (Nicht-Vokabel) →
    `Spaete_Karte_einer_Runde…` (M11 rot); später begonnene Runde → `Spaet_begonnene_Runde…` (M6 rot); später beendete Runde →
    `Spaet_beendete_Runde…` (M7 rot); später abgegebener Test, Pflicht verpasst→erfüllt → `Spaet_abgegebener_Test…` (M8 rot).
  - AK 2, ohne neue Belege bleibt die Zeile → `Abgeschlossene_Tage_werden_einmal_gespeichert…` (B-733); M9 (jede Zeile des Tags
    gilt als neu) rot.
  - AK 3, Problemwörter nach Lerntag → `Problemwoerter_ordnen_einen_Nachtrag…`; M3 (Zeitraum nach `At`) rot.
  - AK 4, Rückfüllung (Winter, Sommer, 7-stellige Sekundenbruchteile) → `Bestandsantworten_bekommen_den_deutschen_Tag…`; M10
    (UTC-Datum) rot.
  - `mutant-probe.py` nach der letzten Änderung, 2026-11-26 18:29: 11/11 rot, Grundlinie danach grün.
  - Ungemessen (kein Verhalten, nur Last): Rechnen außerhalb der Transaktion (Review-Fund 1).
- **2026-11-26** — `pugling-reviewer`: eingearbeitet Fund 1 (Schreibsperre über 28 Tage Rechnen → Rechnen vorgezogen),
  Fund 2 (Karten anderer Übungstypen als Beleg → `ReviewEvents`), BOMs entfernt, zwei veraltete `summary`-Sätze, Rollback mit
  `CancellationToken.None`. Nebenwirkung, gewollt: Antworten einer über Mitternacht fortgesetzten Runde zählen jetzt zum Tag der
  Runde, wo auch ihre Lernzeit steht.
- **2026-11-26** — Verifikation: Suite 2631/2631 (Release, Exit 0), `dotnet format --verify-no-changes` sauber. Rollengang an
  frischer Kopie (Migration eingespielt, 3169 Zeilen gefüllt): Nachtrag des Vaters für Friedrichs schon gespeicherten 25.11.
  erscheint am 25. (Lernzeit 60 s), heute unverändert. Sohn und Creator Regressionszeugen (kein Pfad im Diff).
