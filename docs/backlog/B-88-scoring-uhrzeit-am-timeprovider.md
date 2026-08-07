---
tags: [typ/story, status/abgenommen, bereich/punkte, bereich/tests, rolle/student]
aliases: [Scoring-Uhrzeit, DateTime.Now im Punkte-Pfad]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-10-zeitfenster-pro-kind.md
nachgeschaut: "2026-08-07"
---

# B-88 · Die Punkte-Uhrzeit kommt von der Wanduhr, nicht vom `TimeProvider`

`PositionPracticeController` übergibt dem `ScoringService` ein `DateTime.Now` — zwei Zeilen über der
Stelle, an der dieselbe Methode den injizierten `TimeProvider` für Zeitstempel, Combo und
Schnell-Antwort-Bonus benutzt. Damit hängt die **Zeitfenster-Entscheidung** an der echten Uhr des Servers
und lässt sich mit dem `TestClock` nicht einfrieren.

Sichtbar geworden ist das beim Bauen von [B-10](B-10-zeitfenster-pro-kind.md): weil die Uhrzeit nicht
steuerbar ist, braucht der End-to-End-Test einen **eigenen Host**, ein Ganztags-Fenster und zehn
neutralisierte globale Fenster, nur um vom Zeitpunkt des Testlaufs unabhängig zu werden.

Befund des `pugling-reviewer` beim Review zu B-10; die Zeile ist älter als B-10.

## User Story

Als Entwickler möchte ich, dass die Punkte-Uhrzeit über den injizierten `TimeProvider` läuft statt über
`DateTime.Now`, damit der Zeitfenster-Pfad mit dem `TestClock` einfrierbar und damit ohne Zufallsabhängigkeit
von der Wanduhr testbar ist.

## Ist-Stand am Code

- Die einzige `DateTime.Now`-Stelle im gesamten Backend ist
  [`PositionPracticeController.cs:400`](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs):
  `scoring.ScoreReview(cfg, preReviewCount, preBox, prog.Box, wasCorrect, combo, DateTime.Now, elapsedSeconds)`
  (per Grep über `backend/` bestätigt — kein zweites Vorkommen).
- Zwei Zeilen darüber, `PositionPracticeController.cs:352`, liest dieselbe Methode den injizierten
  `TimeProvider` bereits: `var now = time.GetUtcNow().UtcDateTime;` — für den `ReviewEvent`-Zeitstempel,
  `LastReviewedAt`, die Combo-Berechnung (Zeile 341) und die Grundlage des Schnell-Antwort-Bonus
  (`elapsedSeconds`, Zeile 353). `TimeProvider time` ist über den Primärkonstruktor injiziert
  (`PositionPracticeController.cs:27`).
- `ScoringService.ScoreReview(…, DateTime nowLocal, …)`
  ([ScoringService.cs:63-64](../../backend/Pugling.Api/Services/Shared/ScoringService.cs)) reicht `nowLocal`
  an `BasePoints` (`:70`) weiter, die es in `MultiplierAt(TimeOnly.FromDateTime(nowLocal), cfg.TimeSlots)`
  (`:116`) einsetzt — genau die Stelle, die den Zeitfenster-Faktor aus `ScoringOptions`/`PlanPosition.TimeSlots`
  bestimmt (B-10). Der Parametername `nowLocal` erwartet ausdrücklich eine **lokale** Uhrzeit, weil die
  globalen Fenster in `appsettings.json` (`Scoring:TimeSlots`, Zeilen 17-20: Vormittag 08-12, Nachmittag
  12-18, Abend 18-21) in Uhrzeiten der Wanduhr formuliert sind, nicht in UTC.
- `TestClock` ([TestClock.cs](../../backend/Pugling.Api.Tests/TestClock.cs)) überschreibt ausschließlich
  `GetUtcNow()` (`:38-41`); `LocalTimeZone` bleibt die geerbte `TimeProvider`-Vorgabe (`TimeZoneInfo.Local`
  der Maschine). `TimeProvider.GetLocalNow()` ist in der Basisklasse als `ConvertTime(GetUtcNow(),
  LocalTimeZone)` implementiert — ein eingefrorener UTC-Zeitpunkt liefert also **deterministisch** dieselbe
  lokale Uhrzeit, ohne dass `TestClock` etwas Zusätzliches bräuchte.
- `PuglingWebAppFactory` registriert `TestClock` bereits als `TimeProvider`-Singleton für **jeden** Host
  dieser Test-Suite (`PuglingWebAppFactory.cs:68`: `s.AddSingleton<TimeProvider>(Clock)`) — der
  Standard-Host schaltet zusätzlich `Scoring:TimeSlotsEnabled` auf `false` (`:151`), unabhängig von der Uhr,
  weil sonst die von `DocsCaptureTests` eingecheckte Doku am Zeitpunkt des Testlaufs hinge.
- `PositionTimeSlotScoringTests` läuft deshalb gegen einen eigenen Host `TimeSlotsOnFactory`
  ([PositionTimeSlotTests.cs:218-246](../../backend/Pugling.Api.Tests/PositionTimeSlotTests.cs)), der
  `Scoring:TimeSlotsEnabled=true` setzt und die drei globalen `appsettings.json`-Fenster über zehn
  `UseSetting`-Einträge neutralisiert (`:224/238-244`). Der eine Positions-Test
  (`Positions_Fenster_Verdoppelt_Die_Punkte_Der_Antwort`, `:177-194`) legt ein **Ganztags-Fenster**
  (`TimeOnly.MinValue`..`TimeOnly.MaxValue`) an, mit einem eigenen Kommentar zum Exclusive-Ende-Fallstrick um
  Mitternacht (`:183-184`) — beides existiert einzig, weil die Uhrzeit der `ScoreReview`-Bewertung heute die
  echte Wanduhr ist und nicht die (in diesem Host längst eingefrorene) `TestClock`.

## Die echte Lücke

Nicht „die ganze Testinfrastruktur um die Zeitfenster wird überflüssig" (das behauptet die rohe Idee), denn
**zwei der drei Bestandteile bleiben aus einem eigenen, von dieser Zeile unabhängigen Grund bestehen**:

- Der **eigene Host** `TimeSlotsOnFactory` bleibt nötig — der Standard-Host schaltet `TimeSlotsEnabled`
  wegen `DocsCaptureTests` ab, nicht weil die Uhr unsteuerbar wäre.
- Die **zehn neutralisierten globalen Fenster** bleiben die robuste Wahl — sie isolieren die
  Positions-Fenster-Assertion von den drei realen `appsettings.json`-Fenstern (08-21 Uhr), unabhängig davon,
  ob die Uhr eingefroren ist. Ein „einfach eine Uhrzeit außerhalb 08-21 Uhr einfrieren" wäre möglich, koppelt
  den Test aber an den heutigen Inhalt von `appsettings.json` und würde bei einer künftigen Erweiterung der
  globalen Fenster still falsch.

Was tatsächlich entfällt: der **Ganztags-Fenster-Trick** samt seinem Exclusive-Ende-Kommentar. Mit einer
eingefrorenen `TestClock` kann der Test ein **gewöhnliches, schmales** Fenster verwenden (z. B. das
13:00-15:00-Beispiel aus B-10 selbst) und die Uhr auf eine Uhrzeit **innerhalb** dieses Fensters einfrieren —
genau das Muster, das `SpeedBonusTests` für die Antwortzeit schon fährt (`_factory.Clock.FreezeNow()` +
`Advance(...)`). Das ist eine echte, aber kleinere Vereinfachung als in der Idee behauptet.

## Offene Punkte

- ~~Welche Ersetzung ist die richtige — `time.GetLocalNow().DateTime` oder `time.GetUtcNow().UtcDateTime`?~~
  → siehe Entscheidung 1.
- ~~Braucht `TestClock` eine Erweiterung, um `GetLocalNow()` einfrierbar zu machen?~~ → siehe Entscheidung 2.
- ~~Bestätigt sich die Behauptung der Idee, der gesamte Neutralisierungs-Apparat entfalle?~~ → siehe
  Entscheidung 3 (nein, nur der Ganztags-Fenster-Trick entfällt).
- ~~Gibt es weitere `DateTime.Now`-Stellen im Punkte-/Scoring-Pfad, die dieselbe Story mitnehmen sollte?~~ →
  siehe Entscheidung 4 (nein, per Grep verifiziert).

## Entscheidungen

1. **Ersetzung: `time.GetLocalNow().DateTime`, nicht `.UtcNow`.** Begründung: Der Parameter `nowLocal`
   speist `TimeOnly.FromDateTime(...)` gegen Fenster, die in `appsettings.json` und im
   Positions-Formular als Uhrzeiten der Wanduhr (08:00-21:00 usw.) gepflegt werden — eine UTC-Zeit würde bei
   jeder Zeitzone ungleich UTC einen falschen Faktor auswählen. Kosten: keine — Produktionsverhalten bleibt
   identisch, weil `TimeProvider.System.GetLocalNow()` genau das ist, was `DateTime.Now` heute liefert.
2. **`TestClock` bleibt unverändert.** Begründung: `GetLocalNow()` ist in der `TimeProvider`-Basisklasse
   bereits als `ConvertTime(GetUtcNow(), LocalTimeZone)` implementiert; `TestClock` überschreibt nur
   `GetUtcNow()`, und `FreezeNow()`/`Advance(...)` wirken damit automatisch auch auf `GetLocalNow()`. Kosten:
   keine — eine Erweiterung wäre doppelt gemoppelt gewesen.
3. **Die Story-Behauptung wird korrigiert, nicht übernommen.** Der „ganze Neutralisierungs-Apparat" entfällt
   **nicht**: eigener Host (`TimeSlotsOnFactory`) und die zehn neutralisierten globalen Fenster bleiben aus
   den in „Die echte Lücke" genannten, von dieser Zeile unabhängigen Gründen bestehen. Nur der
   Ganztags-Fenster-Trick (`TimeOnly.MinValue`..`MaxValue`) entfällt zugunsten eines gewöhnlichen, schmalen
   Fensters mit eingefrorener Uhr. Begründung: ehrlicher Ist-Stand statt einer beim Ernten geschätzten
   Vereinfachung, die sich beim Nachsehen als zu groß erweist. Kosten: das ursprünglich in der Idee versprochene
   „der ganze Apparat entfällt" ist als Akzeptanzkriterium nicht haltbar — das Kriterium unten formuliert den
   tatsächlichen, kleineren Umfang.
4. **Kein erweiterter Zuschnitt.** Ein Grep über `backend/` nach `DateTime.Now` findet ausschließlich die
   eine Stelle in `PositionPracticeController.cs:400`. Begründung: keine weitere Instanz im Scoring-/Punkte-Pfad
   verlangt dieselbe Korrektur. Kosten: keine.

## Akzeptanzkriterien

1. `PositionPracticeController.cs:400` übergibt `ScoreReview` den lokalen Zeitpunkt über
   `time.GetLocalNow().DateTime` statt `DateTime.Now`.
2. Ein neuer oder erweiterter Test friert den `TestClock` ein und zeigt, dass der Zeitfenster-Multiplikator
   einer Position an der eingefrorenen Uhr hängt, nicht an der Wanduhr: eine Position mit Fenster
   13:00-15:00 ×2,0, Uhr eingefroren auf 14:00 → Faktor greift; dieselbe Position mit Uhr auf 10:00 → Faktor
   greift nicht.
3. `PositionTimeSlotScoringTests.Positions_Fenster_Verdoppelt_Die_Punkte_Der_Antwort` verzichtet auf das
   Ganztags-Fenster (`TimeOnly.MinValue`/`MaxValue`) und den zugehörigen Exclusive-Ende-Kommentar zugunsten
   eines gewöhnlichen Fensters mit passend eingefrorener Uhr.
4. `TimeSlotsOnFactory` (eigener Host, `TimeSlotsEnabled=true`) und die zehn neutralisierten globalen
   Fenster bleiben unverändert bestehen — kein Versuch, sie im Zuge dieser Story ebenfalls zu entfernen.
5. Keine Verhaltensänderung im Produktivbetrieb: `dotnet test Pugling.sln -c Release` bleibt vollständig
   grün, keine bestehende Punktzahl in einem Test ändert sich.

## Schätzung

**Größe: XS** — eine Produktionszeile (Austausch `DateTime.Now` → `time.GetLocalNow().DateTime`) plus die
Anpassung eines bestehenden Tests in `PositionTimeSlotTests.cs`. Kleiner als der S-Anker B-01 (`childId` aus
dem Test-Pfad ziehen), vergleichbar mit dem XS-Anker B-02 (zwei Sätze plus der sie prüfende Test).

- **wo**: backend.
- **migration**: nein — keine Schemaänderung.
- **vertragsbruch**: nein — keine DTO-Signatur ändert sich, reine interne Verdrahtung.
- **Risiken**: minimal. Der bestehende `PositionTimeSlotScoringTests`-Test bemerkt die heutige Lücke nicht
  selbst, weil sein Ganztags-Fenster das Ergebnis ohnehin zeitunabhängig macht — erst der neue/angepasste
  Test aus Akzeptanzkriterium 2 schließt das und beweist, dass die Korrektur wirkt (ohne ihn wäre ein
  versehentliches Zurückrudern auf `DateTime.Now` unbemerkt grün).
- **Angriffsplan**, Backend zuerst und einzig:
  1. `DateTime.Now` → `time.GetLocalNow().DateTime` in `PositionPracticeController.cs:400`.
  2. `PositionTimeSlotScoringTests`: `Positions_Fenster_Verdoppelt_Die_Punkte_Der_Antwort` auf ein
     schmales Fenster + `_factory.Clock.FreezeNow()`/`Advance(...)` umstellen (Muster `SpeedBonusTests`),
     Exclusive-Ende-Kommentar entfernen, weil er sich auf den entfallenden Ganztags-Fall bezog.
  3. Einen zweiten Fall ergänzen, der dieselbe Position mit einer außerhalb des Fensters eingefrorenen Uhr
     gegenprüft (Akzeptanzkriterium 2, zweite Hälfte).
- **Testweg**: `PositionTimeSlotTests.cs` (`PositionTimeSlotScoringTests`), danach
  `dotnet test Pugling.sln -c Release` für die volle Regression. Kein `/smoke-test` nötig — die Änderung hat
  keinen im laufenden Betrieb sichtbaren Effekt (Produktionsverhalten ist vor und nach der Korrektur
  identisch, `TimeProvider.System.GetLocalNow()` entspricht `DateTime.Now`).

## Verlauf

- **2026-08-04** — aus dem B-10-Review aufgenommen (ungeprüft: der genaue Umfang der Testvereinfachung
  ist geschätzt, nicht gemessen).
- **2026-08-04** — ausformuliert gegen den Code: Fundstelle bestätigt
  (`PositionPracticeController.cs:400`, einzige `DateTime.Now`-Stelle im Backend), `TestClock` braucht keine
  Erweiterung (`GetLocalNow()` ist Basisklassen-Funktionalität über `GetUtcNow()`), und die Behauptung „der
  ganze Neutralisierungs-Apparat entfällt" widerlegt: eigener Host und die zehn neutralisierten globalen
  Fenster bleiben aus einem unabhängigen Grund (`DocsCaptureTests`-Determinismus,
  `PuglingWebAppFactory.cs:151`) bestehen — nur der Ganztags-Fenster-Trick entfällt.
- **2026-08-04** — gegrillt: alle vier offenen Punkte in nummerierte Entscheidungen überführt, davon die
  Kernbehauptung der Idee (Entscheidung 3) korrigiert statt übernommen (autonom getroffen, Nutzerauftrag).
- **2026-08-04** — geschätzt: Größe XS, `wo: backend`, `migration: nein`, `vertragsbruch: nein`,
  Angriffsplan eine Produktionszeile plus Testumbau in `PositionTimeSlotTests.cs`, Testweg
  `PositionTimeSlotScoringTests` + volle Regression (autonom getroffen, Nutzerauftrag).
- **2026-08-06** — gebaut (Nachtlauf 2, Sprint 2 „Testsuite-Qualität & Determinismus"):
  `PositionPracticeController.cs:413` auf `time.GetLocalNow().DateTime` umgestellt.
  `Positions_Fenster_Verdoppelt_Die_Punkte_Der_Antwort` auf ein schmales 13:00–15:00-Fenster mit
  eingefrorener `TestClock` umgebaut (statt Ganztags-Fenster), neuer Gegenfall
  `Positions_Fenster_Ausserhalb_Laesst_Punkte_Unveraendert` (Uhr auf 10:00, Faktor bleibt aus).
  **Rote Probe:** `DateTime.Now` testweise zurückgesetzt → `Positions_Fenster_Verdoppelt_…` sofort rot
  (`Expected: 20, Actual: 10`, an der echten Wanduhr ~00:21 außerhalb des Fensters), zurückgenommen.
  `dotnet test Pugling.sln -c Release` → **746/746 grün**, `dotnet format --verify-no-changes` clean.
  `pugling-reviewer` lief gegen den gesamten Sprint-2-Diff, kein Blocker.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `PositionPracticeController` `ScoreReview` weiterhin
  über `time.GetLocalNow().DateTime` statt `DateTime.Now` speist — hält
  (`PositionPracticeController.cs:413`). Kein Fund.
