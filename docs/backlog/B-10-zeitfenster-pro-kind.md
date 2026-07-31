---
tags: [typ/story, status/geschaetzt, bereich/punkte, bereich/gamification, rolle/supervisor]
aliases: [Zeitfenster pro Pflicht, Zeitfenster pro Kind, Hausaufgaben-Faktor]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: memory/bonus-gamification-system.md
---

# B-10 · Zeitfenster (Punkte-Faktor) je Pflicht statt global

> **Der Titel hat sich beim Grillen geändert:** aus „pro Kind" wurde „je Pflicht" (Entscheidung 1). Der
> Dateiname behält den alten Slug — er ist die Spur zum ursprünglichen Wunsch, und die Id ist ohnehin die
> stabile Referenz.

## User Story

Als Vater möchte ich das Punkte-Zeitfenster **je Pflicht** festlegen — etwa einen
„13–15-Uhr-Hausaufgaben-Faktor" an der Hausaufgaben-Position —, damit der Anreiz zur Tageszeit passt, zu der
*diese* Aufgabe erledigt werden soll.

## Ist-Stand am Code (geprüft am 2026-07-30)

Die Notiz beschrieb einen Weg, den es nicht mehr gibt. **Etappe E12 des DB-Umbaus hat `TimeSlotRule`
aufgelöst** (Commit `b08d691`): Entity und `DbSet` sind weg, die Fenster stehen in der Konfiguration.

- `ScoringOptions` ([Services/Shared/ScoringOptions.cs](../../backend/Pugling.Api/Services/Shared/ScoringOptions.cs)):
  Abschnitt `Scoring` mit `TimeSlotsEnabled` (bool) und `List<ScoringTimeSlot>`; ein Fenster trägt `Name`,
  `Start`, `End`, `Multiplier`.
- `ScoringService.MultiplierAt(TimeOnly)`
  ([ScoringService.cs:119-130](../../backend/Pugling.Api/Services/Shared/ScoringService.cs)): liest
  `IOptions<ScoringOptions>` und nimmt **nur eine Uhrzeit** — kein Kind, kein Plan, keine Position.
  Überlappung ist erlaubt, die Auswahl aber deterministisch: das am spätesten beginnende (engste) Fenster
  gewinnt, bei Gleichstand das früher endende.
- `TimeSlotsEnabled` existiert **für die Test-Suite**: mit Fenstern hängt die Punktzahl derselben richtigen
  Antwort an der Uhrzeit des Laufs, und für die von `DocsCaptureTests` eingecheckte Doku ist das
  Diff-Rauschen. Gleiche Bauart wie `RateLimiting:LoginEnabled`.
- Aufrufkette: `BasePoints(cfg, reviewCount, box, nowLocal)` → `MultiplierAt` (`ScoringService.cs:101-107`).
  Das `cfg` ist eine `ScoreConfig`, gebaut in
  [PositionPracticeController.cs:322](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs)
  **aus der `PlanPosition`** (`pos.NewContentPoints`, Combo-Schwelle …). Die per-Position-Punktewerte kommen
  also längst aus Daten — nur der Zeitfaktor nicht.

## Die echte Lücke

Nicht „`MultiplierAt` fehlt ein `childId`-Parameter" (das ist eine Zeile), sondern: **die Fenster sind jetzt
Konfiguration, und ein Kind ist keine Konfiguration.** Ein kind-abhängiger Faktor braucht ein
**Daten**-Zuhause — und genau eine Tabelle dafür hat E12 aus guten Gründen entfernt (keine API, kein
Schreibpfad außer dem Seed, keine Überlappungsprüfung, und die Test-Suite musste ihre Zeilen *löschen*, um
deterministische Punktzahlen zu bekommen).

## Entscheidungen

1. **Zuhause ist die `PlanPosition`**, als JSON-Spalte. Sie trägt schon jede Punkte-Einstellung
   (`NewContentPoints`, `ComboThreshold`, `PenaltyCoins`) und wird in die `ScoreConfig` eingelesen; das Kind
   ist über den Plan eindeutig. **Folge — und sie ist gewollt:** Damit wird es „je Pflicht" statt „je Kind".
   Fachlich genauer, denn „Hausaufgaben zwischen 13 und 15" ist eine Aussage über die Hausaufgabe, nicht über
   das Kind rund um die Uhr; abendliches Vokabelüben bleibt unberührt. **Kosten:** eine Migration (Spalte),
   ein `ValueComparer`, ein Feld im Formular. **Verworfen:** JSON am `Child` (wirkt pauschal auf alles, was
   das Kind lernt, und das Kind ist im Scoring-Pfad nicht geladen); Mission statt Multiplikator (Verstärkung
   statt Gewichtung — einmalig statt dauerhaft pro Antwort); neue Tabelle (E12 war zwei Stunden alt).
2. **Positions- und globale Fenster landen in EINER Liste**, und die *bestehende* Ordnung entscheidet
   (engstes gewinnt). **Kosten: null neue Semantik** — `MultiplierAt` bekommt eine zusammengesetzte Liste
   statt `cfg.TimeSlots`. Beispiel: global `08–12 ×1,5` + `20–23 ×0,8`, Position `13–15 ×2,0` → 09:00 ×1,5,
   14:00 ×2,0, 21:00 ×0,8. **Verworfen:** „Positions-Fenster ersetzt die globalen" (bräuchte eine neue Regel
   und nähme still den Abend-Malus mit) und „Faktoren multiplizieren" (Punktzahlen werden unvorhersehbar,
   und Vorhersehbarkeit *ist* der Wert des server-autoritativen Punktesystems).
3. **Ein Fenster, eingeklappt** im Positions-Formular: aufklappbarer Block „Zeitfenster" mit von / bis /
   Faktor. Gespeichert wird eine **einelementige Liste** — die Ablage bleibt listenfähig, eine spätere
   Erweiterung auf mehrere Fenster kostet dann nur UI und **keine** Migration. **Kosten:** ein zwölftes Feld
   wird vermieden, dafür eine Aufklapp-Mechanik. **Verworfen:** Repeater (Frontend-Arbeit für einen Fall, der
   nicht genannt wurde) und „erst API, UI später" (verletzt die stehende Bedingung „ohne API-Gefummel").
4. **`TimeSlotsEnabled: false` schaltet auch Positions-Fenster ab.** *Nicht verhandelt — von den Fakten
   erzwungen:* `MultiplierAt` kehrt bei `false` früh mit 1,0 zurück, vor dem Zusammenwerfen. Sonst hinge die
   von `DocsCaptureTests` eingecheckte Doku wieder an der Uhrzeit des Laufs. **Kosten:** ein test-motivierter
   Schalter regiert jetzt auch ein Vater-Feature — das gehört in seine XML-Doku.
5. **Keine Überlappungsprüfung beim Schreiben.** *Ebenfalls von den Fakten erzwungen:* Überlappung ist heute
   ausdrücklich erlaubt und die Auswahl trotzdem deterministisch. Ein Verbot wäre ein neuer Validierungsfall
   an einer Stelle, die bewusst keinen hat. **Kosten:** der Vater kann sich widersprüchliche Fenster bauen —
   das Ergebnis ist aber definiert und dokumentiert.

## Akzeptanzkriterien

1. Eine `PlanPosition` kann ein Zeitfenster (von / bis / Faktor) tragen; ohne Eintrag gilt unverändert die
   globale Konfiguration.
2. Beim Punkten wird die Vereinigung aus Positions- und globalen Fenstern betrachtet; es gewinnt das engste,
   bei Gleichstand das früher endende — **unverändert dieselbe Regel wie heute**.
3. `TimeSlotsEnabled: false` liefert Faktor 1,0, auch wenn die Position ein Fenster trägt.
4. Der Vater stellt das Fenster im Positions-Formular ein, in einem eingeklappten Block, mit Feld-Erklärung.
5. Die neue Spalte trägt einen `ValueComparer` und steht in `UnlimitedByDesign`.
6. Kein Rückbau von E12: **keine** neue Tabelle für Zeitfenster.

## Schätzung

**Größe: M** — vergleichbar mit B-03 (neuer Batch-Pfad im `MediaSelector`): eine Spalte samt Migration, eine
Signatur, drei DTOs und ein Formularblock.

- **`migration: ja`** — neue JSON-Spalte an `PlanPosition`; die Kette wird **neu gefaltet**, nicht verlängert
  (`SchemaGuardTests` hält Länge 1).
- **`vertragsbruch: nein`** — die Vertrags-Records bekommen das Feld **additiv** (Create/Update/Response der
  Position). Kein bestehendes Feld fällt weg, `Pugling.Client` und Frontend-Typen ziehen ohne Bruch nach.
- **Angriffsplan, Backend zuerst:**
  1. `ScoringTimeSlot` wiederverwenden (nicht duplizieren) und als JSON-Liste an `PlanPosition` hängen.
  2. `MultiplierAt(TimeOnly, IReadOnlyList<ScoringTimeSlot> extra)` — Vereinigung bilden, Ordnung unverändert.
  3. `ScoreConfig` um das Feld erweitern, in `PositionPracticeController:322` aus `pos` füllen.
  4. DTOs additiv, dann `Pugling.Client`, dann Formular + `fieldHelp`-Eintrag.
- **Drei Fallstricke, alle namentlich bekannt:**
  1. **`ValueComparer`** für die neue JSON-Spalte (`Data/JsonValueComparer.cs`) — ohne ihn gehen Änderungen
     still verloren, solange niemand die Liste neu zuweist.
  2. **`UnlimitedByDesign`** in `PuglingDbContext.cs:965` — seit E11 kappt eine Konventionsschleife jede
     String-Spalte auf 200 Zeichen, wenn sie nicht mit Begründung dort steht.
  3. **`TimeOnly` in JSON** — Serialisierung und Rundreise prüfen, nicht annehmen.
- **Testweg:** Integrationstest im Muster von `ScoringTimeSlotTests` (setzt die Fenster per `UseSetting`):
  Position mit `13–15 ×2,0` gegen globales `20–23 ×0,8` → 14:00 doppelt, 21:00 mit 0,8, `TimeSlotsEnabled:
  false` → 1,0 in beiden Fällen. Dazu `PatchClearFieldTests` beachten: ein löschbares Feld braucht einen
  ausdrücklichen `Clear…`-Schalter im Update-DTO, sonst meldet das Formular „Gespeichert" und der alte Wert
  steht weiter da.
- **Reihenfolge im Bereich:** unabhängig von E13/E14 — berührt weder `LearnGoal` noch `Objective`.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft). Beim Ernten die Kollision mit E12 vorhergesagt.
- **2026-07-30** — ausformuliert gegen den Code. Die Vorhersage traf ein: E12 wurde am selben Tag committet
  (`b08d691`), `TimeSlotRule` existiert nicht mehr. Der Weg der Notiz („`ChildId` an `TimeSlotRule` hängen")
  ist gegenstandslos.
- **2026-07-30** — gegrillt: fünf offene Punkte aufgelöst, drei im Dialog entschieden, zwei von den Fakten
  erzwungen. Der Zuschnitt wanderte dabei von „pro Kind" zu „je Pflicht".
- **2026-07-30** — geschätzt: **M**, `migration: ja`, `vertragsbruch: nein`. Nicht umgesetzt.
