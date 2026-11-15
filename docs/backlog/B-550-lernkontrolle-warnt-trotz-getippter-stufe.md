---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/training, rolle/supervisor]
aliases: [Lernkontrolle ignoriert die Stufe, Selbsteinschätzung zählt trotz Stufe 4]
status: abgenommen
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 74 (Retro-Pflicht), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-469, B-16]
wartet_auf: ""
nachgeschaut: 2026-11-15
---

# B-550 · Die Positionszeile warnt „Selbsteinschätzung zählt“, obwohl die Stufe getippt prüft

Die Lernkontrolle der Positionszeile (B-469) rechnet nur aus Prüfart, „nur getippte Tests“ und dem Manifest-Flag
`selfAssessedTest` des Typs (`frontend/src/vater/PlanPositions.tsx:833`, Regel `lernkontrolleText` `:972-981`); die Stufe
der Position, den Stufenplan und die `defaultStage` der Übung kennt sie nicht. Der Server prüft im Test aber auf der
Tagesstufe `stageSchedule → stage → defaultStage → Typ-Standard` (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:80`)
und senkt ein Item nie unter die erste geprüfte Sprosse (`TestItemStage`, `:157-165`). Szenario: Der Vater stellt im
Formular, das B-16 um „Stufe“ erweitert hat, eine Vokabel-Position auf eine getippte Stufe (z. B. 4) und lässt „nur
getippte Tests“ aus. Ein Aufdecken mit „Gewusst!“ gibt es in diesem Test nicht – die Zeile zeigt trotzdem gelb
„Test, Selbsteinschätzung zählt“, und die Feldhilfe `lernkontrolle` (`frontend/src/lib/fieldHelp.ts:124-129`) sagt
„Aufdecken und „Gewusst!“ zählen mit“ und rät, den Schalter einzuschalten. Dasselbe gilt für eine Übung, deren Ersteller
eine getippte `defaultStage` vorgibt. Offen für die Ausformulierung: ob `PositionResponse` die `defaultStage` der Übung
mitbringt oder das Frontend sie nachschlagen muss, und was die Zeile bei einem Stufenplan sagt, der erst ab Tag n
getippt prüft.

## Verlauf

- **2026-11-14** — angelegt in der Nachschau zu Sprint 74 (Backlog-Lauf 2026-11-14), Beleg am Code `HEAD`.
- **2026-11-14** — Sprint 76 (Gesamt-Review), Backend gebaut: `PositionResponse.LearningCheck` (`LearningCheck(Kind, TypedFromDay)`, Enum `LearningCheckKind`: `Typed`, `SelfAssessed`, `Ladder`, `Playthrough`, `NoTest`, `Unreachable`), gerechnet in `PositionPlayService.LearningCheckOf` aus denselben Regeln wie Test und `IsGoalMetAsync` (Stufenplan → Positionsstufe → `defaultStage` der Übung → Typ-Standard; `IsTypedStage`/`IsDisplayOnlyStage`; `CheckMode None`; „nur getippt“ macht einen selbst eingeschätzten Tag unerreichbar, weil der Versuch dort nie `Graded` ist). Damit beantwortet: die `defaultStage` rechnet der Server mit, das Frontend schlägt nichts nach; ein Stufenplan meldet `Ladder` mit dem Tag, ab dem jeder Tag getippt prüft. **Stakeholder-Entscheidung im Auftrag des Nutzers:** der Server lehnt `requireTypedTest: true` mit einem Stufenplan ab, der eine selbst eingeschätzte Stufe enthält (`validation_error`, beim Anlegen und bei einem PATCH, der eine der beiden Hälften setzt). Begründung: die Kombination macht das Tagesziel an diesen Tagen unerreichbar, der Malus liefe gegen ein Kind, das seinen Teil getan hat; der Assistent schickt sie schon heute nicht (`wizardFinish.ts`). Kosten: eine ältere Position mit der Kombination bleibt bestehen (als `Unreachable` sichtbar) und lässt sich in allem anderen weiter bearbeiten; eine feste selbst eingeschätzte Stufe mit „nur getippt“ wird nicht abgelehnt, nur als `Unreachable` gemeldet. Tests `LernkontrolleTests` (8 Fälle). Die Positionszeile im Frontend zieht ein eigener Agent nach.
- **2026-11-15** — **abgenommen** (Abschluss des Laufs). Gesamt-Review des Laufs (Backend + Frontend), dort als Fund aus dem Zusammenspiel behoben. Volle Suite 1659/1659 (Release), Vitest 1088/1088, `tsc -b` grün, volle Playwright-Suite nach dem letzten Code-Edit 153 passed (zwei Specs mit veralteter Erwartung danach angepasst und grün: 3 passed). Der Server liefert `PositionResponse.learningCheck` (Typed/SelfAssessed/Ladder/Playthrough/NoTest/Unreachable), die Zeile rendert nur noch. Mutanten rot: G1 Positions-Stufe ignoriert (feste Stufe 4 ⇒ getippt, Kern dieser Story), G2 NoTest, G3 Unreachable, G4 Schwellentag, G5b Ablehnung „nur getippt“ + Leiter (Stakeholder-Entscheidung). Rest → B-552 (Basisstufe Selbsteinschätzung + „nur getippt“ wird noch nicht abgelehnt).
