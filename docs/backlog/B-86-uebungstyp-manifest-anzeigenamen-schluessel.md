---
tags: [typ/story, status/geschaetzt, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Übungstyp-Manifest i18n, DisplayName-Schlüssel, Anzeigename Vertragsbruch]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: ja
quelle: B-38 (geteilt)
ersetzt_durch: []
---

# B-86 · Das Übungstyp-Manifest liefert Anzeigenamen als Daten, nicht als Schlüssel

Zweite Teilstufe aus dem geteilten [B-38](B-38-mehrsprachige-oberflaeche.md) (dort Entscheidung 5): Solange
`BuiltInExerciseTypes.cs` einen fest-deutschen `DisplayName` je Übungstyp ausliefert, bleibt jede sonst
vollständige Frontend-Übersetzung (siehe [B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md)) an dieser
einen Stelle löchrig — ein Kind sähe „Leseverständnis" mitten in einer sonst englischen oder französischen
Arcade. Eigenständig geschätzt, weil sie einen Vertragsbruch mit Streuwirkung in drei Projekten trägt und
unabhängig von den Sprach-Teilstufen selbst gebaut werden kann.

## User Story

Als **Nutzer der übersetzten Oberfläche** möchte ich, dass auch der Name eines Übungstyps in meiner
gewählten Sprache erscheint, damit die Übersetzung nicht an einer einzigen, vom Server gelieferten Stelle
sichtbar abbricht.

## Ist-Stand am Code

- `backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs` liefert für alle zwölf eingebauten Übungstypen
  einen deutschen `DisplayName` fest im Code: Zeile 22 (`"Leseverständnis"`), 39 (`"Hörverständnis"`), 65
  (`"Aufsatz"`), 78 (`"Grammatik"`), 97 (`"Übersetzung"`), 114 (`"Birkenbihl"`), 132 (`"Lückentext"`), 188
  (`"Zuordnung"`), 225 (`"Rechenaufgaben"`), 265 (`"Rechen-Drill"`), 304 (`"Liste"`).
- Ausgeliefert über `GET creator/exercise-types` (Übungstyp-Manifest, siehe Memory
  „Übungs-Meta & Versionierung": Typ-Manifest als einzige Typquelle).
- Das Frontend liest den Namen laut `frontend/CLAUDE.md` roh durch (`src/lib/exerciseTypes.ts`) — „nicht
  aus einer Tabelle im Frontend". Der Server-Anzeigename IST der UI-Text, ohne Umweg.
- Der KI-Creator-Agent (`backend/Pugling.Agent.Creator`) liest denselben Anzeigenamen für seine Prompts
  (Briefing-Pipeline) — ein zweiter Konsument desselben Feldes.

## Die echte Lücke

Der Anzeigename ist heute **Daten vom Server, kein UI-Text im Frontend-Sinn** — genau umgekehrt zum Rest
der Oberfläche, wo Übersetzung eine reine Frontend-Angelegenheit ist (siehe B-85). Eine Übersetzung des
Frontend-Korpus allein lässt diese eine Stelle zwangsläufig unangetastet.

## Offene Punkte

- ~~Schlüssel oder mehrsprachige Werte direkt im DTO?~~ → siehe Entscheidung 1.
- ~~Was macht der KI-Creator-Agent mit einem Schlüssel statt einem lesbaren Namen?~~ → siehe Entscheidung 2.
- ~~Reihenfolge zu B-85?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **Das Manifest liefert einen stabilen `Key` (z. B. `"reading-comprehension"`) zusätzlich zum
   bisherigen `DisplayName`; das Frontend übersetzt anhand des `Key` über die i18n-Schicht aus B-85.**
   Begründung: additiv statt ersetzend hält den Übergang sauber — bestehende Konsumenten, die weiterhin
   `DisplayName` lesen (z. B. der KI-Creator-Agent für Prompts, wo ein lesbarer deutscher Name erwünscht
   bleibt), brechen nicht. Kosten: zwei Felder statt einem, `DisplayName` bleibt langfristig als
   „Anzeigename für Nicht-UI-Konsumenten" bestehen, statt vollständig zu verschwinden — bewusst in Kauf
   genommen, weil der KI-Creator einen sprechenden Namen für seine Prompts braucht, keinen technischen Slug.
2. **Der KI-Creator-Agent liest weiterhin `DisplayName`, nicht `Key`.** Begründung: das Briefing braucht
   einen für das Sprachmodell verständlichen Namen, kein technisches Kürzel — genau der in Entscheidung 1
   benannte zweite Konsument. Kosten: keine — das ist der Grund, warum `DisplayName` additiv bleibt statt
   ersetzt zu werden.
3. **Unabhängig von B-85 baubar, aber sinnvoll erst NACH einer laufenden i18n-Schicht.** Begründung: der
   `Key` ist wertlos ohne eine Übersetzungstabelle, die ihn auflöst — B-85 liefert diese Infrastruktur.
   Kosten: eine Reihenfolge-Abhängigkeit in der Empfehlung, keine harte technische Abhängigkeit (das
   Backend-DTO kann für sich allein gebaut und getestet werden).

## Akzeptanzkriterien

1. `GET creator/exercise-types` liefert je Übungstyp zusätzlich einen stabilen, sprachneutralen `Key`
   (additiv, `DisplayName` bleibt unverändert erhalten).
2. `Pugling.Client` und das OpenAPI-Vertragsdokument spiegeln das neue Feld.
3. Das Frontend (`src/lib/exerciseTypes.ts`) löst den `Key` über die i18n-Schicht in die jeweils gewählte
   Sprache auf, mit `DisplayName` als Fallback, solange kein Übersetzungseintrag existiert.
4. Der KI-Creator-Agent ist unverändert lauffähig (liest weiterhin `DisplayName`).
5. Bestehende Tests, die `DisplayName` prüfen (`ExerciseTypeManifestTests` u. a.), bleiben grün; ein neuer
  Fall prüft die Anwesenheit und Stabilität des `Key`-Feldes.

## Schätzung

**Größe: M** — ein additives Vertragsfeld über zwölf feste Werte, zwei Konsumenten-Anpassungen
(Frontend-Auflösung, Client-Regeneration), keine Migration (das Manifest ist kein DB-Modell). Vergleichbar
mit dem M-Anker (vokabel-basierter Batch-Pfad).

- **wo**: beides
- **migration**: nein — das Übungstyp-Manifest ist code-definiert, keine Tabelle.
- **vertragsbruch**: ja — additiv (neues Feld `Key`), kein Bruch bestehender Konsumenten, aber
  `Pugling.Client` muss regeneriert werden (Konvention: neuer Endpunkt/neues Feld → Client-Methode ergänzen).
- **Risiken**: der KI-Creator-Agent könnte an einer übersehenen Stelle doch versehentlich auf `Key` statt
  `DisplayName` umgestellt werden — Regressionstest deckt das ab (Akzeptanzkriterium 4).
- **Angriffsplan**: Backend zuerst — `Key` am Manifest-Eintrag ergänzen, OpenAPI/Client regenerieren,
  danach Frontend-Auflösung mit Fallback, zuletzt Tests.
- **Testweg**: `ExerciseTypeManifestTests.cs` (neuer Fall für `Key`), Vitest-Komponententest für die
  Frontend-Auflösung mit Fallback, `/smoke-test`.

## Verlauf

- **2026-08-03** — angelegt beim Teilen von [B-38](B-38-mehrsprachige-oberflaeche.md) (Entscheidung 5/8
  dort), direkt als `geschaetzt` übernommen: Ist-Stand und Kernentscheidung waren in B-38 bereits belegt,
  hier eigenständig ausgearbeitet und geschätzt. Autonom getroffen, Nutzerauftrag 2026-08-04.
