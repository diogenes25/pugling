---
tags: [typ/story, status/ausformuliert, bereich/qualitaet, bereich/tests]
aliases: [Testsuite-Sensitivität, Grenzfälle, ScoringService-Grenzen]
status: ausformuliert
prio: P2
art: Aufräumen
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

- **Der Befund ist abgearbeitet, nicht offen.** In [testplan.md](../testplan.md) ist jeder benannte Punkt
  geschlossen: Fehlerklasse (a) im zweiten Commit (`:585-594`), die Restliste (c) mit „Stand 2026-07-30:
  abgearbeitet" und Gegenprobe je Rang (`:416-426`), der `SpeedBonusTests`-Flake über die
  `TimeProvider`-Naht (`:510-518`), die vier Grenzfall-Regeln D01/D07/D11/D15 (`:487-490`). Die alte Fassung
  dieser Story war damit unerfüllbar: Wer sie annimmt, erhebt neu (ausgeschlossen) oder erfindet Tests.
- **Was der Testplan offen lässt**, steht in `:660-666`: Unit zu Integration ist **5 % zu 95 %**, „jeder
  Grenzfall kostet einen vollen Flow". Benannt sind zwei kombinatorische Ecken – `ScoringService` /
  `StageMechanics` und der `MediaSelector`.
- **`ScoringService` ist unit-fähig**, ohne Host und ohne DB: `ScoringService.cs:15` nimmt nur
  `IOptions<ScoringOptions>`, und die Klassendoku sagt es ausdrücklich („Completely stateless – a pure
  function […] That makes it testable without a host").
- Die Grenzen liegen offen: `MinSpeedSeconds = 1.0` (`ScoringService.cs:37`, „below this it counts as a
  double click/automation"), dazu aus der `ScoreConfig` (`:44`) `ComboThreshold` und
  `SpeedThresholdSeconds`; die Zeitfenster kommen aus `Scoring:TimeSlots` in `appsettings.json`.

## Die echte Lücke

Nicht „die Suite ist unsensibel" – das war die Messung von 2026-07-30, und ihre Punkte sind abgearbeitet. Die
Lücke ist der **Preis** eines Grenzfalls: Solange jede Grenze über einen vollen HTTP-Flow geprüft wird, prüft
man sie einmal und nicht viermal (darunter, genau darauf, knapp darüber, darüber). Genau die zweite Stelle –
*genau darauf* – war laut Befund die teuerste. `ScoringService` ist die Stelle, an der das nichts kostet.

Der `MediaSelector` bleibt bewusst draußen: seine vier Injektionen sind nachgearbeitet oder als „nicht
beobachtbar" umgestuft (`testplan.md:401-412`) – dort ist ohne neue Messung keine Aussage möglich, und neu
gemessen wird nicht.

## Akzeptanzkriterien

1. Eine `[Theory]`-Klasse gegen `ScoringService` **ohne Host**, die je Grenze vier Fälle fährt: darunter,
   **genau darauf**, knapp darüber, deutlich darüber.
2. Abgedeckt sind mindestens: `MinSpeedSeconds` exakt 1,0, `SpeedThresholdSeconds` exakt erreicht, die
   Combo-Schwelle exakt erreicht und um eins verfehlt, die Zeitfenster-Kante exakt zur vollen Stunde, die
   höchste Leitner-Stufe.
3. **Die Abnahme ist nicht die Zahl der Tests, sondern je Grenze eine Gegenprobe:** Vergleich im
   Produktivcode von `>=` auf `>` (bzw. umgekehrt) drehen → genau der Grenzfall-Test wird rot → zurücknehmen.
   Protokolliert, nicht behauptet – dasselbe Verfahren, das den zweiten Commit der Defektinjektion belegt hat.
4. Kein Produktivcode geändert; die Laufzeit des Haupttors steigt nicht messbar (die Klasse braucht keinen
   Host).

## Offene Punkte

1. **Gehört `StageMechanics` mit hinein?** Der Testplan nennt beide in einem Atemzug. **Empfehlung:** erst
   `ScoringService`, dann entscheiden – die Gegenprobe je Grenze ist der teure Teil, und eine Klasse davon
   zeigt, wie viel es kostet.

## Verlauf

- **2026-07-30** — geerntet (Befund liegt vor, Abarbeitung offen).
- **2026-08-01** — ausformuliert **und neu zugeschnitten**: Der Befund ist entgegen der Story
  **abgearbeitet** – jeder benannte Punkt in `testplan.md` ist geschlossen. Übrig bleibt die eine Aussage aus
  `:660` (Unit/Integration 5:95), daraus wird eine Theory-Klasse auf den Grenzen des `ScoringService`.
  Bleibt außerhalb des Testabdeckungs-Pakets.
