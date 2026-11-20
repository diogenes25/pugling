---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/gamification, bereich/api, rolle/supervisor]
aliases: [Zusatzpflicht bei Terminverschiebung, Absage und Schreiben schließen die Vorbereitung]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-248
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-758 · Die Vorbereitungs-Pflicht folgt der Klassenarbeit: verschoben, abgesagt, geschrieben

Teil (c), Folgen, von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md), geteilt beim Schätzen am 2026-11-20. Baut auf
[B-752](B-752-vorbereitungs-pflicht-im-fenster.md) (Abgleich) und [B-659](B-659-pflicht-wird-kuer-bricht-die-serie.md) (Schließ-Regel) auf: der Abgleich
läuft auch **synchron**, wenn der Vater die Klassenarbeit ändert.

## User Story

Als *Vater* möchte ich, dass die Vorbereitungs-Pflicht meiner Änderung folgt – Termin verschoben, Arbeit abgesagt oder geschrieben –, damit das Kind
nie für eine Arbeit büßt, die es nicht mehr gibt.

## Ist-Stand am Code

- Die Schreibwege der Klassenarbeit sind `POST`/`PATCH` im `KlassenarbeitenController` (nur `Roles.Supervisor`, `:92`, `:130`); ein Abgleich mit
  Positionen existiert nicht (B-248 Ist-Stand, 2026-11-19).
- Die Schließ-Regel für den Wechsel Pflicht → Kür steht in [B-659](B-659-pflicht-wird-kuer-bricht-die-serie.md) (Entscheidung 2): erfüllt → heute zählt mit,
  offen → heute zählt nicht mit.
- Der Abgleich selbst entsteht in B-752.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt; übernommen aus B-248.*

1. **Der Abgleich läuft auch synchron, wenn der Vater Fenster, Termin oder Status ändert** (B-248 Entscheidung 2, zweiter Halbsatz). Begründung: ein
   Vater, der den Termin verschiebt, soll die Wirkung nicht erst beim nächsten Login des Kindes sehen. Kosten: ein zweiter Aufrufort im
   `KlassenarbeitenController` neben dem Login. Verworfen: nur beim Login.
2. **Abgesagt oder geschrieben schließt die Zusatzpflicht nach der Schließ-Regel von B-659** (B-248 Entscheidung 7): die laufende Periode wird
   geschlossen (erfüllt → heute zählt mit, offen → heute zählt nicht mit); ab dem Tag danach gibt es weder Pflicht noch Malus. Kosten: der Abgleich teilt den
   Helfer mit B-659. Schließt aus: Malus für einen Tag, an dem die Arbeit schon abgesagt war.
3. **Verschieben oder Fenster ändern lässt die schon angelegte Pflicht folgen** (B-248 AK 9): `DutySince` und `DutyUntil` werden beim nächsten
   Abgleich neu gerechnet, bei Fenster 0 entfällt die Pflicht für diese Arbeit nach der Schließ-Regel. Kosten: eine Verschiebung nach vorn kann
   rückwirkend nichts anlegen (der Anlegetag gilt). Schließt aus: eine Pflicht, die stur beim ursprünglichen Termin endet.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht (B-248 AK 9 und 11, geschärft).

1. Verschiebt der Vater Termin oder Fenster, folgen `DutySince` und `DutyUntil` der schon angelegten Zusatzposition beim nächsten Abgleich (synchron
   nach dem PATCH und beim nächsten Login). *Rot, wenn* das Fenster nur beim ersten Anlegen gelesen wird.
2. Auf `Fenster 0` gesetzt, entfällt die Pflicht der Arbeit nach der Schließ-Regel (keine neue, die laufende Periode geschlossen). *Rot, wenn* eine
   Arbeit mit Fenster 0 ihre Position behält.
3. Abgesagt oder geschrieben: die laufende Periode wird nach der Regel von B-659 geschlossen (erfüllt → heute zählt, offen → heute zählt nicht), danach gibt es
   weder Pflicht noch Malus. *Rot, wenn* der Status nicht gelesen wird oder immer „heute“ bzw. immer „gestern“ geschlossen wird.
4. Eine Übung, die auch im Bündel einer **zweiten** geplanten Arbeit liegt, behält ihre Pflicht bis zu deren Termin, wenn die erste abgesagt wird. *Rot, wenn*
   die Absage die Position für beide schließt.
5. Ein Wechsel von `Planned` zurück zu `Planned` (erneutes Speichern ohne Änderung) verschiebt nichts. *Rot, wenn* jedes PATCH die Spanne neu schreibt (Gegenprobe,
   im Ausgangszustand schon so; fällt nur durch einen Abgleich ohne Prüfung der Änderung).

## Schätzung

**Größe: M** – wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03): ein zweiter Aufruf des vorhandenen Abgleichs im PATCH, die
Schließ-Regel wiederverwenden, kein neues Schema.

- **`migration: nein`** – nachgesehen: alle Spalten legen B-659 und B-752 an.
- **`vertragsbruch: nein`** – nachgesehen: `UpdateClassTestDto` ändert sich nicht (B-752 fügt `PrepWindowDays` additiv hinzu).
- **`wo: backend`** – die Oberfläche steht in B-757.

### Risiken

- **Gleiche Regel an zwei Aufrufstellen:** Login und PATCH müssen denselben Abgleich rufen, nicht zwei Kopien; ein Test je Aufrufort.
- **Reihenfolge im PATCH:** Status und Termin zuerst speichern, dann abgleichen, in **einer** Transaktion mit dem Speichern, sonst sieht ein Fehler dazwischen
  eine halb geänderte Pflicht.
- **Dieselbe Übung in zwei Arbeiten** (B-752 Entscheidung 5): die Schließung der einen darf die andere nicht beenden (AK 4).
- **Dasselbe Tagesplaner-Risiko wie B-752:** bei Übernahme durch B-644 entfällt die Story.

### Angriffsplan

1. B-752 gebaut; im PATCH des `KlassenarbeitenController` nach dem Speichern den Abgleich rufen (Änderung von Termin, Fenster oder Status erkennen).
2. Schließ-Regel aus B-659 für den Statuswechsel anwenden; `DutyUntil` bei mehreren Arbeiten neu rechnen.
3. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `VorbereitungsPflichtFolgtTests` in `backend/Pugling.Api.Tests` (Aufbau wie `VorbereitungsPflichtTests`): AK 1 bis 5, je Fall ein Test; AK 3 mit
  beiden Zuständen (Periode erfüllt, offen).
- `VorbereitungsPflichtTests`, `KlassenarbeitenTests`, `PflichtMalusTests` und `RueckwirkendePflichtTests` bleiben grün.
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test`; Rollengang als Vater (Arbeit verschieben, absagen).

## Verlauf

- **2026-11-20** — angelegt beim Teilen von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) und geschätzt (Vorbereitungslauf, Sonnet): M, backend,
  keine Migration, kein Vertragsbruch. Hängt an B-752 und B-659.
