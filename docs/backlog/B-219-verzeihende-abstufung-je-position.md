---
tags: [typ/story, status/in-arbeit, bereich/training, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Verzeihende Abstufung, Box-Abstieg, Soft Demotion]
status: in-arbeit
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-219 · Verzeihende Abstufung als Positions-Option

Ein Fehler setzt ein Item bei uns auf Box 1 zurück, Leitner-rein und hart. Khan Academy stuft aus dem
Prozentwert einer Runde (70–99 % Familiar, 100 % Proficient) und verzeiht in der Mastery Challenge einen
von zwei Fehlern je Skill. Die Idee: eine Option an der `PlanPosition`, mit der ein Fehler das Item nur
**eine** Box senkt und erst ein zweiter Fehler in Folge auf Box 1 zurücksetzt. Der Default bleibt das
heutige Verhalten; der Supervisor entscheidet je Position, wie hart die Wiederholung greift.

## User Story

Als **Vater** möchte ich je Position einstellen können, dass ein einzelner Ausrutscher ein schon gefestigtes Wort nur
eine Box zurückwirft und erst ein zweiter Fehler in Folge ganz auf Box 1, damit mein Sohn eine Position mit vielen
sicheren Wörtern nicht wegen eines Flüchtigkeitsfehlers als ungerecht hart erlebt – während die ehrliche Messung
des Lernstands je Vokabel unverändert hart bleibt.

## Ist-Stand am Code

Nachgemessen am 2026-11-15:

- **Harter Reset der Position**: `PositionPlayService.ApplyReview`
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:485-501`): richtig → `Box + 1` (gedeckelt auf
  `pos.MaxBox`, `:493`), falsch → `prog.Box = 1` (`:498`) und `DueOn = today` (`:499`). Keine Option.
- **Zwei Aufrufer**: die gewertete Leitner-Wiederholung (`Controllers/Student/PositionPracticeController.cs:531`) und
  der Selbstauskunfts-Sprung der Stufenleiter (`climbs`, `:553-558`, B-476), der die Box bewegt, ohne zu werten.
- **Plan-übergreifender Lernstand**: `ItemProgressService.cs:70` setzt falsch ebenfalls auf 1 – bleibt laut
  Entscheidung des Nutzers vom 2026-10-23 hart (siehe Verlauf), wird hier nicht angefasst.
- **Fortschrittszeile**: `PositionItemProgress` (`Models/PlanPositionEntities.cs:198-222`) trägt `Box`, `DueOn`,
  `ReviewCount`, `LastReviewedAt`, `IntroducedAt`, `NewBonusPaidAt` – **kein** Merker für „letzte Antwort war
  falsch“. Die Antwort-Historie (`ReviewEvent`) hängt an der Sitzung, nicht am Item; ein „zweiter Fehler in Folge“
  ist daraus nicht billig ablesbar.
- **Positionsfelder**: `PlanPosition.UseLeitner`/`MaxBox` (`Models/PlanPositionEntities.cs:123-125`);
  Vertrag `PositionResponse` (`Pugling.Contracts/Supervisor/StudyPlanDtos.cs:100`), `CreatePositionDto` (`:166-167`),
  `UpdatePositionDto` (`:221-222`, `bool?` = „bleibt“); Controller übernimmt `UseLeitner` bei `not null`
  (`Controllers/Supervisor/PlanPositionsController.cs:280`) bzw. beim Anlegen mit Übungs-Vorgabe (`:200`).
- **Punkte**: `ScoringService.BasePoints` zahlt eine richtige Wiederholung mit `Math.Max(2, 8 - box)` auf die Box
  **vor** der Antwort (`Services/Shared/ScoringService.cs:110-117`), eine falsche zahlt nichts (`:67-68`). Der
  Tageszeit-Multiplikator (`MultiplierAt`) wirkt auf diese Basis, unabhängig von der Box.
- **Gegen Mehrfachwertung am selben Tag**: `alreadyScoredToday` (`PositionPracticeController.cs:457`) – eine Karte,
  die heute schon gewertet wurde, bewegt die Box heute nicht noch einmal (`scored`/`climbs` sind dann `false`).
- **Box treibt die Kartenstufe**: `PositionPlayService.cs:155-157` (`type.CardStage(dayStage, c.Box)`, B-476) – eine
  höhere Box heißt eine anspruchsvollere Stufe.
- **Formular**: `frontend/src/vater/PlanPositions.tsx` – Leitner-Häkchen `:463`, Lesen `settingsFrom` `:275-290`,
  Schreiben `settingsToDto` `:327`, `settingsToUpdateDto` `:356`; Feldhilfe `frontend/src/lib/fieldHelp.ts:112`
  (`useLeitner`).
- Letzte Migration der Kette: `20261114114551_DailyBoxOpenedAt`.

## Die echte Lücke

Es gibt keine Positions-Option und keinen Merker, der einen *zweiten* Fehler in Folge vom ersten unterscheidet. Beides
fehlt zusammen: ohne Merker lässt sich die Regel „erst der zweite Fehler setzt auf 1“ nicht serverseitig entscheiden.

## Offene Punkte

1. ~~Gilt die Milde auch für den plan-übergreifenden Lernstand?~~ → nein, **Entscheidung des Nutzers** 2026-10-23.
2. ~~Bool oder Enum an der Position?~~ → Entscheidung 1.
3. ~~Default?~~ → Entscheidung 2 (aus, wie vom Nutzer 2026-10-23 festgelegt).
4. ~~Woran erkennt der Server „zweiter Fehler in Folge“?~~ → Entscheidung 3.
5. ~~Wird der Merker auch bei abgeschalteter Option gepflegt?~~ → Entscheidung 4.
6. ~~Wechselwirkung mit Punkten, Tageszeit-Multiplikator und Anti-Cheat?~~ → Entscheidung 5.
7. ~~Gilt die Milde auch für den Selbstauskunfts-Sprung (`climbs`)?~~ → Entscheidung 6.
8. ~~Wie fällig ist ein mild abgestuftes Item?~~ → Entscheidung 7.
9. ~~Formularfeld und PATCH-Semantik?~~ → Entscheidung 8.

## Entscheidungen

Übernommen: **Entscheidung des Nutzers** (2026-10-23, Grill-Runde G-5): die Milde wirkt **nur in der Position**;
`ItemProgressService.cs:70` bleibt hart; Default an der Position = heutiges Verhalten. Alle übrigen Punkte:

1. **Ein `bool ForgivingDemotion` an `PlanPosition`, kein Enum.** Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: es gibt genau zwei Verhaltensweisen; ein Enum („Hart/Mild/Prozent“) wäre Vorrat für eine dritte, die
   niemand bestellt hat. Kosten: kommt je eine dritte Stufe (etwa Khan-Prozentwertung), wird aus dem bool ein Enum
   mit eigener Migration.
2. **Default `false`** – für neue Positionen und für alle Bestandszeilen der echten `pugling.db` (Spalten-Default der
   Migration). Stakeholder-Entscheidung im Auftrag des Nutzers, deckungsgleich mit der Entscheidung des Nutzers vom
   2026-10-23. Begründung: die laufenden Pläne der Söhne ändern ihr Verhalten nicht still; der Vater schaltet bewusst
   ein. Keine Übungs-Vorgabe (`Exercise.Default…`) – die Härte ist eine Aussage über *diese* Pflicht, nicht über den
   Stoff. Kosten: wer die Milde will, muss sie je Position setzen.
3. **Merker `PositionItemProgress.LastMissed` (bool, Default `false`).** Stakeholder-Entscheidung im Auftrag des
   Nutzers. Regel in `ApplyReview`: richtig → `LastMissed = false`; falsch und Option an und `LastMissed == false` →
   `Box = Max(1, Box - 1)`, `LastMissed = true`; falsch sonst → `Box = 1`, `LastMissed = true`. Begründung: ein bool
   genügt für „zweiter in Folge“; ein Zähler wäre Vorrat. Kosten: eine zweite Spalte in derselben Migration.
4. **Der Merker wird immer gepflegt, auch bei ausgeschalteter Option.** Stakeholder-Entscheidung im Auftrag des
   Nutzers. Begründung: schaltet der Vater die Milde mitten im Plan ein, zählt der Server die wirkliche Vorgeschichte
   – sonst bekäme ein Wort, das gestern schon falsch war, heute noch einmal Milde. Kosten: ein Feldschreiben mehr je
   Wiederholung, ohne Verhaltensänderung bei `false`. Bestandszeilen starten mit `false` („letzte war richtig“) – die
   erste falsche Antwort nach dem Einschalten ist darum mild, auch wenn die letzte vor der Migration falsch war;
   bewusst getragen, weil das nur einmal je Wort greift.
5. **Punkte und Anti-Cheat bleiben unberührt; keine neue Regel im `ScoringService`.** Stakeholder-Entscheidung im
   Auftrag des Nutzers. Begründung, am Code gerechnet: eine falsche Antwort zahlt nie (`ScoringService.cs:67-68`), die
   nächste richtige zahlt `Max(2, 8 - box)` auf die Vor-Box. Die Milde hält die Box **höher**, die Folgepunkte sind
   also **kleiner** als nach dem harten Reset – absichtliches Danebenliegen wird weniger lohnend, nicht mehr. Der
   Tageszeit-Multiplikator wirkt auf die Basis und ist box-unabhängig, er ändert daran nichts. `alreadyScoredToday`
   bleibt das Tor gegen Mehrfachwertung: ein zweiter Fehler am **selben** Tag wird gar nicht gewertet, „in Folge“
   heißt darum praktisch „beim nächsten gewerteten Wiedersehen“. Kosten: keine.
6. **Gilt auch für den Selbstauskunfts-Sprung (`climbs`).** Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: beide Wege gehen durch dasselbe `ApplyReview`; eine Sonderregel für die Selbstauskunft wäre eine
   zweite Wahrheit über dieselbe Box. Kosten: ein „kann ich noch nicht“ auf der Selbstauskunfts-Stufe senkt bei
   eingeschalteter Milde ebenfalls nur eine Box – vertretbar, weil diese Stufe ohnehin nichts zahlt.
7. **Fälligkeit bei milder Abstufung wie beim harten Fehler: `DueOn = today`.** Stakeholder-Entscheidung im Auftrag
   des Nutzers. Begründung: ein Fehler soll am selben Tag noch einmal geübt werden (Lernwirkung), die Box sagt nur, wie
   weit das Wort zurückfällt. Kosten: keine – wertungsfrei, weil `alreadyScoredToday` heute nicht noch einmal zählt.
8. **Formular: ein Häkchen „Ausrutscher verzeihen“ neben dem Leitner-Häkchen, nur bedienbar bei `useLeitner`.**
   Stakeholder-Entscheidung im Auftrag des Nutzers. PATCH: `bool? ForgivingDemotion` im `UpdatePositionDto`
   (`null` = bleibt, `false` = aus) – **kein** `Clear…`-Schalter nötig, weil ein bool keinen „Standard-als-null“-Zustand
   hat (Muster `UseLeitner`, `PlanPositionsController.cs:280`); damit kein neuer Fall in `PatchSemanticsTests`.
   Angelegt: `bool? ForgivingDemotion` im `CreatePositionDto`, `null` = `false`. Antwort: `bool ForgivingDemotion` in
   `PositionResponse`. Alle drei als **letzte Parameter mit Vorgabe**, damit positionale C#-Aufrufer
   (`Pugling.Client`, Tests) nicht brechen. Feldhilfe `forgivingDemotion` in `lib/fieldHelp.ts`. Begründung: die
   Option ist nur mit Leitner sinnvoll; ein ausgegrautes Häkchen erklärt das, statt es zu verstecken. Kosten: ein
   weiteres Formularfeld in einem ohnehin vollen Formular.
9. **Bestandszeilen über den Spalten-Default der Migration, kein Modell-Default.** Stakeholder-Entscheidung im
   Auftrag des Nutzers (2026-11-16). SQLite kann eine `NOT NULL`-Spalte nur mit `DEFAULT` anhängen; `AddColumn(…,
   defaultValue: false)` füllt die Bestandszeilen mit „hart“ bzw. „letzte war richtig“. Das Modell trägt **kein**
   `HasDefaultValue` (G9 bleibt bei zwei bewussten Defaults), EF schreibt beide Spalten bei jedem `INSERT` selbst.
   Begründung: `false` ist der Fail-Safe (heutiges Verhalten); ein `UPDATE` nach dem Anhängen wäre derselbe Wert.
10. **Abgewählter Leitner leert die Milde nicht.** Stakeholder-Entscheidung im Auftrag des Nutzers (2026-11-16). Das
    Häkchen wird nur ausgegraut, der gespeicherte Wert bleibt und wirkt erst wieder mit Leitner. Begründung: ohne
    Leitner gibt es keine Box, die Milde ist dann wirkungslos; schaltet der Vater Leitner wieder an, steht seine Wahl
    noch da. Die Positionszeile nennt die Milde im Leitner-Kästchen („an · max 5 · verzeihend“), damit der Vater sie
    sieht, ohne zu bearbeiten.
11. **Der Fehler-Merker folgt nur geprüften Antworten.** Stakeholder-Entscheidung im Auftrag des Nutzers
    (2026-11-16), präzisiert Entscheidung 6. Der Selbstauskunfts-Sprung (`climbs`) bewegt die Box weiter mild bzw.
    hart, lässt `LastMissed` aber in **beide** Richtungen unverändert – gesichert und zurückgeschrieben wie dort schon
    `ReviewCount` (`PositionPracticeController.cs:570-573`). Begründung: ein ungeprüftes „gewusst“ löschte sonst einen
    getippten Fehler, und der nächste geprüfte Fehler wäre wieder mild. Kosten: ein ehrliches „kann ich noch nicht“
    auf der Umdrehkarte zählt nicht als erster Fehler einer Folge. Am Code gemessen kein Vorteil für den Sohn: die
    Umdrehkarte gibt es nur in Box ≤ 1 (`VocabularyExerciseType.CardStage`), danach steht die Karte in Box ≤ 2, und
    Milde und harter Reset unterscheiden sich erst ab Box 3 – dorthin führt nur ein geprüfter Treffer, der den Merker
    ohnehin löscht.

## Akzeptanzkriterien

1. **Mild an**: Ein Item in Box 4 einer Position mit `forgivingDemotion: true` wird falsch beantwortet → Box 3,
   fällig heute. Am nächsten Tag erneut falsch → Box 1. (Integrationstest über `…/practice/…/answer` mit der
   ersetzbaren Uhr.) **Rot**, wenn `ApplyReview` die Option ignoriert (Box 1 schon beim ersten Fehler) oder den
   Merker nicht liest (Box 2 beim zweiten).
2. **Unterbrochene Folge**: Box 4 → falsch (3) → nächster Tag richtig (4) → nächster Tag falsch → Box 3, nicht 1.
   **Rot**, wenn eine richtige Antwort `LastMissed` nicht zurücksetzt.
3. **Mild aus (Default)**: dieselbe Folge wie AK 1 auf einer Position ohne die Option → Box 1 schon beim ersten
   Fehler. **Rot**, wenn der Default `true` wäre oder `ApplyReview` die Milde ohne Option anwendet.
4. **Plan-übergreifend hart**: In AK 1 steht der plan-übergreifende Lernstand (`ItemProgress.Box`, lesbar über die
   Vokabel-Fortschritts-Sicht) nach dem ersten Fehler auf 1. **Rot**, wenn jemand die Milde in
   `ItemProgressService` nachzieht – schützt die Entscheidung des Nutzers.
5. **Box 1 bleibt Box 1**: ein falsch beantwortetes Item in Box 1 mit Option bleibt auf 1 (nie 0). **Rot** ohne das
   `Max(1, …)`.
6. **Vertrag**: `POST` ohne das Feld legt `forgivingDemotion: false` an; `PATCH { forgivingDemotion: true }` setzt es,
   `PATCH {}` lässt es stehen (Integrationstest im bestehenden Positions-Test). **Rot**, wenn der Controller das Feld
   nicht übernimmt oder bei `null` überschreibt.
7. **Formular beidseitig**: `settingsFrom` liest `forgivingDemotion` zurück, `settingsToDto`/`settingsToUpdateDto`
   schicken es; das Häkchen ist bei ausgeschaltetem Leitner `disabled` (Vitest `PlanPositions.test.ts`). **Rot**,
   wenn das Feld beim Read-back verloren geht.
8. **Migration nimmt Bestand mit**: die neue Migration hängt an `20261114114551_DailyBoxOpenedAt`, beide Spalten mit
   Default `false`, `SchemaGuardTests` grün (kein Modell-Drift). **Rot** (G-Tore), wenn das Modell ohne Migration
   geändert oder die Kette neu gefaltet wird.

## Schätzung

**Größe M** (Anker B-03: ein neuer Pfad über Backend und Frontend; dazu eine Migration). `wo: beides`,
`migration: ja` (zwei Spalten, angehängt, Default `false` – Bestandsdaten bleiben unverändert), `vertragsbruch: nein`
(drei additive Felder mit Vorgabe).

### Risiken

| Risiko | Gegenmittel |
| --- | --- |
| Migration auf der echten `pugling.db` | nur `AddColumn` mit `defaultValue: false`; vor dem Commit gegen eine **Kopie** der echten DB starten (Heimnetz-Regel: nie gegen das Original messen). |
| Selbstauskunfts-Pfad überschreibt `ReviewCount` nach `ApplyReview` (`:556-558`) | der Merker sitzt in `ApplyReview` und wird dort nicht zurückgedreht; AK 1 einmal über eine Selbstauskunfts-Stufe mitprüfen. |
| Testfolge über mehrere Tage | die ersetzbare Uhr (`TimeProvider`) der Test-Factory nutzen; Muster in `PositionPracticeFlowTests`. |
| Kartenstufe hängt an der Box (`PositionPlayService.cs:155-157`) | gewollt: das mild abgestufte Wort bleibt auf einer anspruchsvolleren Stufe. Kein Code nötig, im Rollengang beobachten. |

### Angriffsplan

1. Entity: `PlanPosition.ForgivingDemotion`, `PositionItemProgress.LastMissed`; Migration `ForgivingDemotion`
   anhängen.
2. `PositionPlayService.ApplyReview`: Regel aus Entscheidung 3 (der Merker immer, die Milde nur mit Option).
3. Vertrag: drei Felder (Entscheidung 8), Controller Anlegen/PATCH/Map; Vertrag (`docs/openapi/v1.json`) neu
   erzeugen.
4. Integrationstests AK 1–6.
5. Frontend: `PositionSettings.forgivingDemotion`, `settingsFrom`/`settingsToDto`/`settingsToUpdateDto`, Häkchen,
   Feldhilfe; Vitest AK 7.

### Testweg

Integrationstests in `PositionPracticeFlowTests` (AK 1–5) und im Positions-Controller-Test (AK 6); `SchemaGuardTests`
(AK 8); Vitest `PlanPositions.test.ts` (AK 7). Rollengang: als Vater Häkchen setzen, als Sohn ein Wort aus hoher Box
falsch tippen und die Box im Feedback sehen (oder E2E über die Planbau-Specs, falls einer die Position bearbeitet).

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 2 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht; der harte Reset sitzt an **zwei** Stellen (`PositionPlayService.cs:283`, `ItemProgressService.cs:66`) – zu klären, ob die Milde auch für den plan-übergreifenden Lernstand je Item gilt.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): die verzeihende Abstufung wirkt **nur in der Position** (`PositionPlayService.cs:283`). Der plan-übergreifende Lernstand je Vokabel (`ItemProgressService.cs:66`) bleibt hart und damit die ehrliche Messung. Der Default an der Position bleibt das heutige Verhalten. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-18); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt; die Zeilen sind gewandert: harter Reset der Position `backend/Pugling.Api/Services/Shared/PositionPlayService.cs:468` (`prog.Box = 1`), plan-übergreifend `Services/Shared/ItemProgressService.cs:70`. Keine Positions-Option für milde Abstufung vorhanden.
- **2026-11-15** — Backlog-Lauf 2026-11-15: ausformuliert (Ist-Stand nachgemessen, Reset jetzt `PositionPlayService.cs:498`, zwei Aufrufer), gegrillt (acht Stakeholder-Entscheidungen im Auftrag des Nutzers; die Entscheidung des Nutzers vom 2026-10-23 übernommen) und geschätzt: **M**, `beides`, Migration ja (zwei Spalten, Default `false`), kein Vertragsbruch.
- **2026-11-16** — Backlog-Lauf 2026-11-16: gebaut (Stufe `in-arbeit`). Migration `20261116065113_ForgivingDemotion` hängt an `20261115215256_ExerciseItemExplanation` (die Kette ist seit der Schätzung um ein Glied gewachsen): zwei `AddColumn<bool>` mit `defaultValue: false`, sonst nichts; Snapshot-Diff genau die zwei Properties. Regel in `PositionPlayService.ApplyReview`, Vertrag additiv (`PositionResponse`/`CreatePositionDto`/`UpdatePositionDto` je letzter Parameter mit Vorgabe), Controller Anlegen/PATCH/Map, Formular-Häkchen „Ausrutscher verzeihen“ (ausgegraut ohne Leitner), Feldhilfe, Positionszeile „· verzeihend“. Zwei Stakeholder-Entscheidungen im Auftrag des Nutzers (2026-11-16) als Entscheidung 9 und 10 ergänzt. Volle Suite 1708/1708 grün (exit 0), Vitest 66/66 in den drei Positions-Dateien, `dotnet format --verify-no-changes` sauber.
- **2026-11-16** — Nachweis je AK-Fall (Mutanten per `.claude/scripts/mutant-probe.py`, Debug, gemessen an `88b7b50c` + Arbeitsbaum, Grundlinie nach dem Zurückspielen grün):
  - AK 1, Option ignoriert → `VerzeihendeAbstufungTests.Mild_Erster_Fehler_Eine_Box_Zweiter_In_Folge_Box_1` (`backend/Pugling.Api.Tests/VerzeihendeAbstufungTests.cs:21`), M1 `prog.Box = 1` → rot.
  - AK 1, Merker nicht gelesen → derselbe Test (`:21`), M2 ohne `!prog.LastMissed` → rot.
  - AK 2, Treffer setzt `LastMissed` nicht zurück → `Mild_Treffer_Unterbricht_Die_Folge` (`:36`), M3 → rot.
  - AK 3, Milde ohne Option → `Hart_Ohne_Option_Ein_Fehler_Box_1` (`:48`), M4 → rot.
  - AK 4, Milde im plan-übergreifenden Lernstand → `Mild_Gilt_Nicht_Fuer_Den_Planuebergreifenden_Lernstand` (`:58`, geseedeter `ItemProgress.Box = 4`, sonst könnte die Zusicherung nie fallen), M5 in `ItemProgressService.cs:70` → rot.
  - AK 5, ohne `Max(1, …)` → `Mild_Box_1_Bleibt_Box_1` (`:87`), M6 → rot.
  - AK 6, Default an / PATCH ignoriert / `null` überschreibt / Antwort ohne Feld → `PlanPositionCrudTests.Verzeihende_Abstufung_Default_Aus_Patch_Setzt_Leerer_Patch_Laesst_Stehen` (`backend/Pugling.Api.Tests/PlanPositionCrudTests.cs:547`), M7/M8/M9/M10 → je rot.
  - AK 7, Read-back → `PlanPositions.test.ts:452` „liest den gespeicherten Stand zurück“, M12 → rot; Senden → `:460` „schickt das Häkchen …“, M13 → rot; ausgegraut ohne Leitner → `PlanPositionsStufe.test.tsx:200`, M14 → rot.
  - AK 8, Modell ohne passende Migration → `SchemaGuardTests.Modell_Und_Migrationen_Stimmen_Ueberein` (`SchemaGuardTests.cs:33`), M11 Snapshot-Property umbenannt → rot.
  - Offen bis `abgenommen`: Probe der Migration gegen eine **Kopie** der echten `pugling.db` und der Rollengang (Vater setzt das Häkchen, Sohn tippt ein Wort aus hoher Box falsch).
- **2026-11-16** — Defekt im Increment (Review): der Selbstauskunfts-Pfad löschte über `ApplyReview` den Merker
  `LastMissed`. Behoben nach Entscheidung 11; Nachweis (`.claude/scripts/mutant-probe.py`, Debug, gemessen an
  `63522e03` + Arbeitsbaum nach dem letzten Edit, Grundlinie nach dem Zurückspielen grün; der Mutant entspricht dem
  Stand `63522e03`):
  - AK 2, Selbstauskunft „gewusst“ → `VerzeihendeAbstufungTests.Mild_Selbstauskunft_Gewusst_Loescht_Den_Fehler_Merker_Nicht`
    (`backend/Pugling.Api.Tests/VerzeihendeAbstufungTests.cs:99`), Mutant ohne Zurückschreiben → rot.
  - AK 2, Selbstauskunft „nicht gewusst“ → `Mild_Selbstauskunft_Nicht_Gewusst_Setzt_Den_Fehler_Merker_Nicht` (`:115`),
    derselbe Mutant → rot.
  - Beide Fälle sichern den Merker selbst, nicht die Box: die Folge „getippt falsch → Selbstauskunft gewusst →
    getippt falsch ⇒ Box 1“ ist über die Stufenleiter nicht erreichbar (Begründung in Entscheidung 11).
