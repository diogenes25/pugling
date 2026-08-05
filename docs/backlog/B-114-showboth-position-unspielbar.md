---
tags: [typ/story, status/in-arbeit, bereich/lehrplan, rolle/student]
aliases: [ShowBoth ohne Knopf, Kennenlern-Stufe unspielbar]
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-05 der Commits 4469662…b20600f (Befund 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-96]
---

# B-114 · Eine Kennenlern-Position hatte für das Kind keinen einzigen Knopf — und kostete Münzen

B-96 hat `ShowBoth` zur echten Anzeigenurstufe gemacht, die die Klausur mit `400 stage_not_testable`
ablehnt. Was dabei nicht mitgezogen wurde: der Server sagte dem Client weiter, die Position sei prüfbar.
Ohne Leitner blieb damit **kein** Weg ins Spiel, und die Tagespflicht war unerfüllbar — das Kind verlor
nachts Malus-Münzen für eine Übung, die es nicht spielen konnte.

## User Story

Als Sohn möchte ich jede zugewiesene Position spielen können, damit ich nicht für eine Aufgabe bestraft
werde, die die App mir gar nicht anbietet.

## Ist-Stand am Code

Zum Zeitpunkt des Funds (Stand `b20600f`, B-96 war `abgenommen`):

- `PositionProgressService.ComputeDayAsync` leitete `Testable` allein aus dem `ExerciseCheckMode` des
  Typs ab — eine Aussage über den **Übungstyp**, nicht über den **Tag**. Auf einer freien Anzeigestufe
  blieb sie `true`.
- `SohnHome.PositionCard` bot darum nur „TEST" an; die Klausur antwortet dort mit
  `400 stage_not_testable` (die Regel, die B-96 eingezogen hat). `canPractice` war zusätzlich an
  `checkMode === "None"` gebunden, also auch falsch.
- `IsGoalMetAsync` verlangte für `ExerciseCheckMode.StudyPlanTest` einen bestandenen `TestAttempt`. Auf
  einem Tag ohne prüfbare Stufe konnte der nie entstehen → `GoalPenalty` über
  `SettleClosedPeriodsAsync`.

## Die echte Lücke

B-96 hat die Stufe serverseitig **korrekt** verboten, aber die zwei abgeleiteten Aussagen nicht
nachgezogen: „ist heute prüfbar?" und „gilt die Pflicht heute als erfüllt?". Beide hingen am Typ statt am
Tag. Der Reviewer von B-96 hat das nicht gefunden, weil er „ist der Code richtig?" beantwortet — und der
Code war in sich richtig. Die Frage „kann das Kind spielen?" stellt nur ein Rollengang, und der fand nicht
statt (siehe README → „Der Rollengang fällt am leichtesten weg").

## Entscheidungen

1. **`Testable` wird eine Tages-Aussage.** `IsDisplayOnlyDay(pos, plan, day)` fragt den Typ nach der
   Stufe *dieses* Tages; `Testable` ist false, sobald sie eine freie Anzeigestufe ist. **Kosten:** der
   Vertrag von `PositionStatus` bekommt eine präzisere Bedeutung, die in der `<summary>` erklärt werden
   muss — ein Client, der `Testable` als Typ-Eigenschaft gelesen hat, liest jetzt anders.
2. **Die Pflicht fällt an einem solchen Tag auf die gespielte Runde zurück** (dieselbe Regel wie bei
   `ExerciseCheckMode.None`), additiv: ein Zeitraum, dessen bewerteter Tag eine echte Stufe trägt,
   verlangt weiter den Test. **Kosten:** an einem Anzeigetag genügt Durchspielen — gewollt, denn es gibt
   nichts zu bewerten.
3. **Das Frontend fragt `testable`, nicht `checkMode`.** **Kosten:** keine; die alte Bedingung war
   ohnehin redundant.

## Akzeptanzkriterien

1. Eine `ShowBoth`-Position ohne Leitner meldet `testable: false` und `checkMode: "StudyPlanTest"`.
2. Genau ein Weg ist offen: der Sohn sieht „DURCHSPIELEN", nicht „TEST".
3. Die gespielte Runde erfüllt an diesem Tag das Tagesziel — kein Malus.

## Schätzung

**Größe: S**, `wo: beides`, keine Migration, kein Vertragsbruch (nur eine präzisierte `<summary>`).
Testweg: Integrationstest in `PositionPracticeFlowTests`, der die Position anlegt, `overview` liest und
die Runde spielt.

## Verlauf

- **2026-08-05** — gefunden im Code-Review der autonomen Bau-Runde (Befund 1) und **sofort behoben**,
  Commit `e2d622b`: `IsDisplayOnlyDay`, `Testable` als Tages-Aussage, Rückfall der Pflicht auf die
  gespielte Runde, `SohnHome` fragt `testable`. Rote Probe zuerst: der neue Test
  `ShowBoth_OhneLeitner_IstNichtPruefbar_UndDieGespielteRundeErfuelltDiePflicht` scheiterte gegen den
  Vorzustand (kein Knopf, `goalMet` blieb falsch), danach grün. **729/729** Backend.
- **2026-08-05** — **als eigene Story nachgetragen**, damit die Entgleitung in der Messung erscheint
  (README → „Die eine Zahl über die Wirkung"). Vorher stand sie nur als Zeile im `## Verlauf` von B-96 und
  fehlte damit genau in der Zahl, für die sie der Anlass ist. `entgangen_bei: [B-96]`.
- **2026-08-05** — bleibt auf `in-arbeit`: `pugling-reviewer`/`frontend-reviewer` sind an je drei
  serverseitigen `529` gescheitert, die Eintrittsbedingung von `abgenommen` ist also unerfüllt. Alles
  andere ist belegt.
