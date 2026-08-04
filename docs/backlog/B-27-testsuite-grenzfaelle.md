---
tags: [typ/story, status/geschaetzt, bereich/qualitaet, bereich/tests]
aliases: [Testsuite-Sensitivität, Grenzfälle, ScoringService-Grenzen]
status: geschaetzt
prio: P2
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/testplan.md
---

# B-27 · Die Grenzen des `ScoringService` als Tabelle statt als Flow

**Neu zugeschnitten am 2026-08-01** beim Grillen des Testabdeckungs-Pakets. Der alte Titel („die
Grenzfall-Lücke schließen") behauptet ein Arbeitsblatt, das es nicht mehr gibt – und die Story bleibt
**außerhalb** des Pakets ([testabdeckung-plan.md](../testabdeckung-plan.md), Entscheidung 7): das hier ist
Testtiefe, nicht Abdeckung.

## User Story

Als **Entwickler**, der an der Punkteberechnung eine Schwelle verschiebt, möchte ich, dass ein Test **genau
auf** der Grenze steht, damit ein `>=` statt `>` sofort rot wird – und nicht erst auffällt, wenn ein Kind
seinen Bonus nicht bekommt.

## Ist-Stand am Code

**Nachgeprüft am 2026-08-04** – der parallel laufende Zeitfenster-Umbau (`PlanPosition.TimeSlots`,
`ScoringOptions`) hat den Code seit dem Ausformulieren tatsächlich verschoben, siehe Korrekturen unten.

- **Der Befund ist abgearbeitet, nicht offen.** In [testplan.md](../testplan.md) ist jeder benannte Punkt
  geschlossen: Fehlerklasse (a) im zweiten Commit (`:585-594`), die Restliste (c) mit „Stand 2026-07-30:
  abgearbeitet" und Gegenprobe je Rang (`:416-426`), die vier Grenzfall-Regeln D01/D07/D11/D15 (`:487-490`).
  **Korrektur:** keine dieser vier betrifft `ScoringService` – D01 ist die Bestehensgrenze in
  `PositionTestsController.cs:282`, D07 der `MediaSelector`-Tiebreak, D11 der Nachtrag-Tag, D15 das
  `CreatorProfile`-Matching. Sie belegen nur, dass das Testplan-Programm insgesamt abgearbeitet ist – nicht,
  dass `ScoringService` selbst schon an seinen Grenzen geprüft ist.
- **Was der Testplan offen lässt**, steht in `:660-666`: Unit zu Integration ist **5 % zu 95 %**, „jeder
  Grenzfall kostet einen vollen Flow". Benannt sind zwei kombinatorische Ecken – `ScoringService` /
  `StageMechanics` und der `MediaSelector`.
- **`ScoringService` ist unit-fähig**, ohne Host und ohne DB: `ScoringService.cs:15` nimmt nur
  `IOptions<ScoringOptions>`, und die Klassendoku sagt es ausdrücklich („Completely stateless – a pure
  function […] That makes it testable without a host"). Zeile unverändert seit dem Ausformulieren.
- Die Grenzen liegen offen: `MinSpeedSeconds = 1.0` (`ScoringService.cs:37`, unverändert, „below this it
  counts as a double click/automation"), dazu aus der `ScoreConfig` – **Korrektur:** der Record steht jetzt
  bei `ScoringService.cs:53-54` (nicht mehr `:44`, verschoben durch den Zeitfenster-Umbau) – `ComboThreshold`
  und `SpeedThresholdSeconds`. Die Zeitfenster kommen **seit dem Ausformulieren zusätzlich** aus
  `PlanPosition.TimeSlots` (`PlanPositionEntities.cs:113`), nicht mehr nur aus `Scoring:TimeSlots` in
  `appsettings.json` – beide Listen werden in `ScoringService.MultiplierAt` (`:139-150`) vereinigt, die
  engste gewinnt.
- **Neu seit dem Ausformulieren, deckt einen Teil der Lücke bereits ab:** Der Zeitfenster-Umbau hat
  `ScoringTimeSlotTests.cs` mitgebracht – bereits **ohne Host**, gegen `ScoringService` direkt. Er prüft
  Innerhalb/Außerhalb, überlappende Fenster (engstes gewinnt, Träger-neutral) und den Kill-Switch – aber
  **nicht** die Kante selbst (`time == Start` bzw. `time == End`).
- **Zwei der vier ursprünglich vermuteten Lücken sind bereits – teuer – über den vollen Flow gepinnt:**
  `SpeedBonusTests.AntiCheatUntergrenze_GiltGenauAbEinerSekunde` (`:81-93`, Theory 900 ms → kein Bonus /
  1000 ms → Bonus, als Nebenfund der `TimeProvider`-Nahtreparatur, `testplan.md:510-518`) und
  `ComboTests.ComboBonus_LautPositionsEinstellung_BeiSchwelleErreicht` (`:29-42`, Combo 1 → kein Bonus,
  Combo 2 → Bonus). Beide laufen über den vollen HTTP-Host – genau der Preis, den diese Story vermeiden will.
- **Drei Kanten sind an keiner Stelle geprüft:** `SpeedThresholdSeconds` **exakt an der oberen** Schwelle
  (`ZuLangsameAntwort_UeberDerSchwelle_BringtKeinenBonus` prüft nur 11 s gegen eine 10-s-Schwelle, nie genau
  10 s), die Zeitfenster-Kante exakt auf `Start`/`End`, und die Boden-Schwelle der Wiederholungspunkte
  (`ScoringService.cs:112-114`, `Math.Max(2, 8 - box)` – der Boden 2 wird ab `box == 6` erreicht, `box == 7`
  hält ihn über den Clamp).

## Die echte Lücke

Nicht „die Suite ist unsensibel" – das war die Messung von 2026-07-30, und ihre Punkte sind abgearbeitet. Die
Lücke ist der **Preis** eines Grenzfalls: Solange jede Grenze über einen vollen HTTP-Flow geprüft wird, prüft
man sie einmal und nicht viermal (darunter, genau darauf, knapp darüber, darüber). Genau die zweite Stelle –
*genau darauf* – war laut Befund die teuerste. `ScoringService` ist die Stelle, an der das nichts kostet.

Die Nachprüfung bestätigt das Muster, verschiebt aber den Umfang: Zwei Grenzen (`MinSpeedSeconds`,
`ComboThreshold`) sind schon geprüft, aber **auf die teure Art** über den vollen Flow. Drei weitere
(`SpeedThresholdSeconds` oben, die Zeitfenster-Kante, der Punkte-Boden) sind an **keiner** Stelle geprüft,
weder billig noch teuer. Beides gehört in dieselbe Theory-Klasse.

Der `MediaSelector` bleibt bewusst draußen: seine vier Injektionen sind nachgearbeitet oder als „nicht
beobachtbar" umgestuft (`testplan.md:401-412`) – dort ist ohne neue Messung keine Aussage möglich, und neu
gemessen wird nicht.

## Entscheidungen

1. **`StageMechanics` bleibt draußen** (löst den einzigen offenen Punkt des Ausformulierens). `IsTyped(...)`
   ist ein Pattern-Match über Enum-Werte (`TestStage`/`ClozeStage`) ohne numerische Schwelle – es gibt keine
   Grenze, auf der eine Theory „genau darauf" stehen könnte; `Normalize` ist reine Stringtransformation ohne
   Zahlenschwelle. Der ganze Gewinn dieser Story (billige Gegenprobe an einer Zahl-Grenze) trägt hier nicht.
   Kosten: keine – der Scope bleibt exakt auf `ScoringService` begrenzt, wie ursprünglich empfohlen.
2. **Die neue Theory-Klasse deckt fünf Grenzen, nicht vier** – trotz (bzw. wegen) der Nachprüfung:
   `MinSpeedSeconds` unten, `SpeedThresholdSeconds` oben, `ComboThreshold` exakt erreicht/um eins verfehlt,
   die Zeitfenster-Kante exakt auf `Start`/`End`, und der Punkte-Boden ab `box == 6`. Begründung: Zwei der
   fünf sind zwar schon über den vollen Flow gepinnt, aber genau **das** ist der Preis, den diese Story
   abschaffen soll – sie werden zusätzlich billig auf `ScoringService`-Ebene gespiegelt, statt sie als
   „schon erledigt" auszulassen. Kosten: eine Fallgruppe mehr als ursprünglich angenommen, aber jede bleibt
   Host-frei und kostet damit praktisch nichts extra.
3. **Die bestehenden HTTP-Flow-Tests (`SpeedBonusTests`, `ComboTests`) werden nicht zurückgebaut.** Sie
   prüfen eine andere Sorge als die neue Theory-Klasse: dass die Positions-**Einstellung** (`ComboThreshold`
   etc.) überhaupt bis zum `ScoringService` durchdringt, nicht nur, dass die Formel stimmt. Kosten: eine
   kleine, akzeptierte Redundanz zwischen Unit- und Flow-Ebene – genau die Art von Doppelung, die diese
   Story für **neue** Grenzen vermeiden will, aber für bestehende, funktionierende Tests kein Grund zum
   Löschen ist.

## Akzeptanzkriterien

1. Eine `[Theory]`-Klasse gegen `ScoringService` **ohne Host** (Vorschlag: `ScoringServiceBoundaryTests`),
   die je Grenze mehrere Fälle fährt: darunter, **genau darauf**, knapp darüber, deutlich darüber – so viele
   wie zur jeweiligen Grenze passen (Schwellen brauchen vier Punkte, das Zeitfenster zwei Kanten, der
   Punkte-Boden drei Box-Werte).
2. Abgedeckt sind mindestens die fünf in Entscheidung 2 benannten Grenzen: `MinSpeedSeconds` exakt 1,0,
   `SpeedThresholdSeconds` exakt erreicht, die Combo-Schwelle exakt erreicht und um eins verfehlt, die
   Zeitfenster-Kante exakt auf `Start` (innerhalb) und `End` (außerhalb, halboffenes Intervall), und der
   Punkte-Boden bei `box == 5` (noch darüber), `box == 6` (Boden erreicht) und `box == 7` (Clamp greift).
3. **Die Abnahme ist nicht die Zahl der Tests, sondern je Grenze eine Gegenprobe:** Vergleich im
   Produktivcode von `>=` auf `>` (bzw. umgekehrt, bzw. `Math.Max(2, …)` kurz entfernt) drehen → genau der
   Grenzfall-Test wird rot → zurücknehmen. Protokolliert, nicht behauptet – dasselbe Verfahren, das den
   zweiten Commit der Defektinjektion belegt hat.
4. Kein Produktivcode geändert; die Laufzeit des Haupttors steigt nicht messbar (die Klasse braucht keinen
   Host).

## Schätzung

- **Größe:** S – eine neue, Host-freie Testdatei mit fünf Grenzfall-Gruppen plus die dazugehörigen fünf
  Gegenproben (Operator kurz kippen, Testlauf, zurücknehmen); vergleichbar mit dem `childId`-Zuschnitt aus
  B-01, kein Eingriff in Produktivcode, kein neuer Endpunkt.
- **Wo:** `backend` – reine Testarbeit in `Pugling.Api.Tests`, keine Frontend-Berührung.
- **Migration:** nein – keine Schemaänderung, keine neue Spalte, kein neues Modell.
- **Vertragsbruch:** nein – `Pugling.Contracts` bleibt unberührt, es handelt sich um eine interne
  Testklasse gegen einen bestehenden Service.
- **Risiken:**
  1. Die Gegenprobe ist Handarbeit (fünf Operator-Kippungen, je ein gezielter Testlauf) – Zeitkosten, kein
     technisches Risiko.
  2. Der Punkte-Boden (`Math.Max(2, 8 - box)`) ist **unabhängig** vom konfigurierbaren `MaxBox` der Position
     (Default 5) – reine Funktion des `box`-Parameters. Verwechslungsgefahr beim Schreiben der Testfälle;
     im Testkommentar klarstellen, sonst wirkt `box == 6` willkürlich gewählt.
  3. Redundanz zu den bestehenden HTTP-Flow-Tests ist gewollt (Entscheidung 3), nicht versehentlich –
     im Klassenkommentar der neuen Datei kurz begründen, damit ein späterer Leser sie nicht als Duplikat
     streicht.
- **Angriffsplan** (Backend ist die einzige Fläche):
  1. Neue Testklasse `ScoringServiceBoundaryTests` in `backend/Pugling.Api.Tests/` anlegen, analog zu
     `ScoringTimeSlotTests.cs` (kein Host, `Options.Create(new ScoringOptions {...})`).
  2. Grün fahren (`dotnet test`), noch ohne Gegenprobe.
  3. Je Grenze die Gegenprobe fahren: Operator in `ScoringService.cs` kurz kippen, gezielt die neue Klasse
     laufen lassen, den einen erwarteten Fall rot sehen, zurücknehmen. Ergebnis in der PR-Beschreibung
     protokollieren (AC 3 verlangt das Protokoll, nicht nur die Behauptung).
  4. Keine Berührung von Frontend, Contracts oder Client.
- **Testweg:** gezielt `dotnet test backend/Pugling.Api.Tests --filter FullyQualifiedName~ScoringServiceBoundaryTests`
  während der Gegenproben-Runde; zur Gesamtabnahme das reguläre Haupttor
  `dotnet test Pugling.sln -c Release` (Stop-Hook/CI). Kein E2E, kein `/smoke-test` nötig – reine
  Unit-Ebene ohne Host.

## Offene Punkte

1. ~~Gehört `StageMechanics` mit hinein?~~ → siehe Entscheidung 1.

## Verlauf

- **2026-07-30** — geerntet (Befund liegt vor, Abarbeitung offen).
- **2026-08-01** — ausformuliert **und neu zugeschnitten**: Der Befund ist entgegen der Story
  **abgearbeitet** – jeder benannte Punkt in `testplan.md` ist geschlossen. Übrig bleibt die eine Aussage aus
  `:660` (Unit/Integration 5:95), daraus wird eine Theory-Klasse auf den Grenzen des `ScoringService`.
  Bleibt außerhalb des Testabdeckungs-Pakets.
- **2026-08-04** — gegrillt (autonom getroffen, Nutzerauftrag): Ist-Stand gegen den Code nachgeprüft – der
  parallele Zeitfenster-Umbau hat die Zeile der `ScoreConfig` verschoben (`:44` → `:53-54`) und brachte
  bereits eine Host-freie `ScoringTimeSlotTests`-Klasse sowie zwei über den vollen HTTP-Flow gepinnte
  Grenzen (`MinSpeedSeconds`, `ComboThreshold`) mit, die die Kante selbst aber teuer statt billig prüfen.
  Der einzige offene Punkt (`StageMechanics`) ist als Entscheidung 1 beantwortet (nein, bleibt draußen);
  zwei weitere Entscheidungen legen den Umfang der neuen Theory-Klasse (fünf statt vier Grenzen) und den
  Umgang mit der bestehenden Flow-Redundanz fest (bleibt stehen).
- **2026-08-04** — geschätzt (autonom getroffen, Nutzerauftrag): `groesse: S`, `wo: backend`,
  `migration: nein`, `vertragsbruch: nein`. Angriffsplan und Testweg stehen in `## Schätzung`; kein XL-Split
  nötig, der Zuschnitt bleibt eine einzelne, Host-freie Testdatei.
