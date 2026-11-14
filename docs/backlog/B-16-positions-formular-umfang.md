---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/training, rolle/supervisor]
aliases: [Positions-Edit-Formular]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: memory/lehrplan-umbau.md
---

# B-16 · Prüfauftrag: deckt das Positions-Edit-Formular alle Felder ab?

Aus dem Lehrplan-Umbau blieb die Frage offen, ob das Bearbeiten-Formular einer `PlanPosition` alle Felder
abdeckt (Ziel, Punkte, Stufe, Leitner, Malus). Stand der Notiz: 2026-07-05, seither nicht nachgeprüft — der
Frontend-Ausbau hat es vermutlich längst erledigt.

**Dies ist ein Prüfauftrag, keine Änderung.** Erstes Ergebnis kann „gegenstandslos" sein — dann wird die
Story `verworfen`, und das ist der Erfolg. Nach der Recherche gegen den Code: **nicht gegenstandslos** —
vier Felder des Vertrags fehlen im Formular, darunter die namentlich genannte „Stufe". Die Story läuft
darum doch die volle Pipeline.

## User Story

Als Vater möchte ich beim Anlegen und Bearbeiten einer Plan-Position auch Stufe, Auswahl-Scope und den
Tempo-Bonus einstellen können, damit ich nicht auf den Server-Standard angewiesen bin, wenn ich eine
Übung feiner abstimmen will.

## Ist-Stand am Code

> **Stand 2026-11-14:** Die Lücke besteht. `settingsToDto` (`frontend/src/vater/PlanPositions.tsx:286-302`, Datei
> inzwischen 866 Zeilen; `settingsToUpdateDto` `:311-313`) sendet weiter **kein** `stage`, `scope`, `maxBox`, `boxIntervalDays`, `stageSchedule`,
> `speedThresholdSeconds`, `speedBonusPoints`. Der Vertrag (`UpdatePositionDto`, `StudyPlanDtos.cs:180-185`) hat
> inzwischen **22** Felder: dazu kamen `timeSlots`/`clearTimeSlots` und `newItemsPerDay` – alle drei stehen schon im
> Formular (`settingsToUpdateDto` setzt `clearTimeSlots`). Seit B-79 prüft der Server `Stage`, ein rohes Zahlenfeld
> liefe in `validation_error` (Entscheidung G-4: Auswahl aus dem Manifest). `PlanPositions.test.ts` existiert bereits.
> Die Zeilenangaben unten gelten für den Stand vom 2026-08-03.
>
> **Nachgemessen 2026-11-14 für das neue Grillen:**
> - Die Stufen je Typ kennt der Server als `IExerciseType.StageOptions` (`backend/Pugling.Api/Exercises/IExerciseType.cs:133`);
>   sie sind die erlaubte Menge der Prüfung (`Exercises/StageValidation.cs`, leer = Typ ohne Stufenwahl) und vollständig
>   (`ExerciseTypeManifestTests.cs:61-77`). Das **Manifest** (`Pugling.Contracts/Common/ExerciseTypeManifest.cs`, ausgeliefert von
>   `ExerciseTypesController.cs:27` bzw. `ExerciseTypeRegistry.Manifests` `:28`) trägt sie **nicht**; heute erreicht sie
>   das Frontend nur über die Vorschau (`PreviewData.Stages`, `ExercisePreviewDtos.cs:33`).
> - Das Frontend hat eine fest kodierte Liste nur für Vokabeln, `VOCAB_FORMS` (`frontend/src/vater/exerciseConfig.tsx:109-118`),
>   genutzt vom Creator-Formular für `DefaultStage` – genau die Kopie, die G-4 für die Position ausschließt.
> - `PlanPosition.Stage` ist nullbar, `null` = Standard der Übung (`Models/PlanPositionEntities.cs:31-32`); PATCH setzt nur
>   bei `dto.Stage is not null` (`PlanPositionsController.cs:262`), ein Zurück auf `null` ist unmöglich. Der einzige
>   `Clear…`-Schalter des DTO ist `ClearTimeSlots` (`:284`; Doku `StudyPlanDtos.cs:140-145`).
> - `Scope` ist ein nicht-nullbares Enum mit Vorgabe `All` (`PlanPositionEntities.cs:42`), die Tempo-Felder sind
>   nicht-nullbare `int` mit 0 = aus (`:99-101`, Anlegen erbt `?? sb?.… ?? 0`, `PlanPositionsController.cs:215-216`).
> - `PositionResponse` trägt `ExerciseType` und `Stage` (`StudyPlanDtos.cs:93-98`); die Zeile im Bearbeiten-Modus kennt
>   also ihren Typ, die Anlege-Zeile kennt ihn aus `ExerciseSummary.type` der gewählten Übung (`PlanPositions.tsx:632-650`).

- **Vertrag** — `UpdatePositionDto` trägt 19 Felder (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:136-140`):
  `Order, Stage, ItemCount, Scope, Cadence, OrderStrategy, GoalThreshold, RequireTypedTest, UseLeitner,
  MaxBox, BoxIntervalDays, StageSchedule, PointsGoalMet, PenaltyCoins, NewContentPoints, ComboThreshold,
  ComboBonusPoints, SpeedThresholdSeconds, SpeedBonusPoints`. `CreatePositionDto` (:100-104) spiegelt
  dieselben Felder plus `ExerciseId`.
- **Formular** — `PositionSettings`/`PositionFields` (`frontend/src/vater/PlanPositions.tsx:78-219`) und
  `settingsToDto` (:130-144) decken **11 von 19** Feldern ab: `cadence` (→`Cadence`), `goalThreshold`
  (→`GoalThreshold`), `itemCount` (→`ItemCount`), `orderStrategy` (→`OrderStrategy`), `pointsGoalMet`,
  `penaltyCoins`, `newContentPoints`, `comboThreshold`, `comboBonusPoints`, `useLeitner`, `requireTypedTest`.
  Dieselbe Komponente bedient Anlegen (`AddPosition`, :222-303) **und** Bearbeiten (`PositionRow`, :306-382).
- **Fehlend im Formular (8 von 19):**
  - `Order` — keine Umsortier-UI; Zeilen zeigen `pos.order + 1` nur lesend (:335).
  - `Stage` — kein Eingabefeld. Deckt sich mit dem schon offenen [B-79](B-79-position-stufe-unvalidiert.md),
    das zeigt, dass die API `Stage` roh und ungeprüft als `int` entgegennimmt.
  - `Scope` — kein Auswahl-Feld für All/New/Old (`ItemScope`,
    `backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:15-23`).
  - `MaxBox`, `BoxIntervalDays`, `StageSchedule` — keine Leitner-Feinjustage jenseits des An/Aus-Schalters
    `useLeitner`.
  - `SpeedThresholdSeconds`, `SpeedBonusPoints` — der dritte Bonus-Kanal fehlt komplett; asymmetrisch zu
    `comboThreshold`/`comboBonusPoints`, die beide im Formular stehen (`PlanPositions.tsx:196-204`).

## Die echte Lücke

Die ursprüngliche Sorge der Notiz („Ziel, Punkte, Stufe, Leitner, Malus") ist bei vier von fünf Themen
gedeckt — **außer „Stufe"** (`Stage`), die im Formular vollständig unerreichbar ist. „Leitner" hat nur den
Schalter, nicht die Feinjustage (`MaxBox`/`BoxIntervalDays`/`StageSchedule`). Dazu kommen zwei Felder, die
die Notiz nicht kannte: `Scope` und der komplette Tempo-Bonus-Kanal. `Order` ist keine Formularfeld-Lücke
im engen Sinn — es gibt schlicht **keine** Umsortier-Funktion in der UI, unabhängig vom Bearbeiten-Formular
der übrigen Felder.

Seit G-4 gehört zur Lücke auch ein Backend-Teil: die Stufen-Optionen je Typ fehlen im Manifest, und `Stage` lässt sich
per PATCH nicht auf „Standard der Übung“ zurücksetzen.

## Offene Punkte

1. ~~Woher kommen die Stufen-Optionen?~~ → Entscheidung 4 (Nutzer, G-4).
2. ~~Wie geht „zurück auf Standard“ bei `Stage`?~~ → Entscheidung 5 (Nutzer, G-4).
3. ~~Brauchen `Scope` und die Tempo-Felder auch einen Leeren-Schalter?~~ → Entscheidung 6.
4. ~~Was geschieht mit einer gewählten Stufe, wenn der Vater beim Anlegen die Übung wechselt?~~ → Entscheidung 7.
5. ~~Was bedeutet ein geleertes Tempo-Feld im Bearbeiten-Modus?~~ → Entscheidung 8.

## Entscheidungen

1. **Reordering (`Order`) bleibt außerhalb dieser Story.** Es gibt aktuell keine Umsortier-UI (nicht
   einmal Pfeile) — das ist ein eigenständiges UX-Feature ohne Bezug zu den übrigen, längst vorhandenen
   Formularfeldern. Kosten: `Order` bleibt nur über die API erreichbar; eine eigene Idee, falls je gebraucht.
2. **Die Leitner-Feinjustage (`MaxBox`/`BoxIntervalDays`/`StageSchedule`) bleibt zurückgestellt.** Alle drei
   haben dokumentierte Methoden-Defaults, sind Experten-Einstellungen (Kastenzahl, Intervalle, Stufenplan
   je Box), und `BoxIntervalDays`/`StageSchedule` bräuchten eigene Listen-Editoren — deutlich mehr Aufwand
   als die übrigen Lücken, ohne erkennbaren akuten Bedarf. Kosten: bleiben Server-Default, bis eine eigene
   Story sie verlangt.
3. **Die vier übrigen fehlenden Felder werden ergänzt:** `Stage`, `Scope`, `SpeedThresholdSeconds`,
   `SpeedBonusPoints`. ~~Alle vier sind einfache Zahlen-/Auswahlfelder nach dem Muster, das schon da ist~~
   (überholt 2026-11-14: `Stage` ist eine Auswahl aus dem Manifest, Entscheidung 4). Tempo-Bonus spiegelt exakt
   `comboThreshold`/`comboBonusPoints`; `Scope` ist ein Drei-Werte-Select wie `orderStrategy`. Kosten: vier neue
   Felder in `PositionSettings`, `PositionFields`, `settingsToDto`, `defaultSettings`, `settingsFrom` — einmal für
   Anlegen **und** Bearbeiten, da beide dieselbe Komponente nutzen.
4. ~~**`Stage` bekommt ein einfaches Zahlenfeld, keine Validierung gegen `StageOptions`.**~~ (überholt durch B-79 und
   G-4.) **Neu, Entscheidung des Nutzers in G-4 (2026-10-23):** Die Stufe ist eine **Auswahl je Übungstyp aus dem
   Server-Manifest**. Ausgeformt im Backlog-Lauf 2026-11-14: `ExerciseTypeManifest` bekommt das additive Feld
   `Stages` (`IReadOnlyList<StageOption>`), gefüllt aus **derselben** Liste `IExerciseType.StageOptions`, die die
   Prüfung benutzt – an einer Stelle angereichert (Registry bzw. Basisklasse), nicht je Typ von Hand doppelt
   eingetragen. Die erste Option im Formular ist „Standard der Übung“ (= `null`); ein Typ ohne Optionen zeigt **kein**
   Stufen-Feld. `VOCAB_FORMS` bleibt dem Creator-Formular, das Positions-Formular benutzt es nicht. Begründung: eine
   Liste, die Server und Formular teilen, kann nicht auseinanderlaufen; ein ungültiger Wert ist im Formular gar nicht
   erst wählbar. Kosten: ein additives Vertragsfeld, `contract.ts` neu erzeugen, das Manifest wird etwas größer.
5. **Neu, Entscheidung des Nutzers in G-4 (2026-10-23):** „Zurück auf Standard“ geht über einen ausdrücklichen
   **Leeren-Schalter** `ClearStage` (`bool`, Vorgabe `false`) in `UpdatePositionDto`, nach dem Muster `ClearTimeSlots`:
   im Controller erst der Wert, dann der Schalter (Leeren gewinnt). Das Formular schickt `clearStage: true`, wenn der
   Vater „Standard der Übung“ wählt und die gespeicherte Position eine Stufe hatte. Kosten: ein additives Feld, eine
   Zeile in der Tabelle von `PatchSemanticsTests` (das Tor wird sonst rot), die DTO-Doku zu „genau ein Feld hat einen
   Schalter“ (`StudyPlanDtos.cs:140-145`) wird nachgezogen.
6. **Nur `Stage` bekommt einen Schalter; `Scope` und die Tempo-Felder nicht.** *Stakeholder-Entscheidung im Auftrag
   des Nutzers.* Begründung: `Scope` ist nie leer (Vorgabe „Alle“ ist selbst wählbar), die Tempo-Felder kennen 0 = aus
   als eigenen Wert – „zurück auf Standard“ ist dort ein wählbarer Wert, kein Leeren. Kosten: eine nach dem Anlegen
   ererbte Tempo-Empfehlung der Übung lässt sich nicht „neu erben“, nur überschreiben.
7. **Übungswechsel beim Anlegen setzt die Stufe auf „Standard der Übung“ zurück.** *Stakeholder-Entscheidung im
   Auftrag des Nutzers.* Begründung: die Optionen gehören zum Typ; eine Stufe der vorigen Übung wäre für die neue
   womöglich ungültig und liefe in `validation_error` – oder, schlimmer, gültig und ungewollt. Kosten: wer die Stufe
   vor der Übung wählt, muss sie neu wählen.
8. **Tempo-Felder folgen dem Combo-Muster, auch beim Bearbeiten.** Leer heißt beim Anlegen „erbt den Vorschlag der
   Übung“, beim Bearbeiten „bleibt“ (PATCH-`null`); ausschalten heißt 0 eintragen. *Stakeholder-Entscheidung im
   Auftrag des Nutzers.* Begründung: ein abweichendes Muster neben den Combo-Feldern wäre schwerer zu lesen als die
   bekannte Eigenheit. Kosten: dieselbe kleine Falle wie bei Combo heute (geleertes Feld ändert beim Bearbeiten nichts);
   die Feldhilfe sagt „0 = aus“.

## Akzeptanzkriterien

1. `GET api/v1/creator/exercise-types` liefert je Typ `stages` gleich `IExerciseType.StageOptions` (Wert und Label),
   leer für Typen ohne Stufenwahl (Integrationstest in `ExerciseTypeManifestTests`). **Rot**, wenn das Feld aus einer
   eigenen Liste oder gar nicht gefüllt wird.
2. `PATCH …/positions/{id}` mit `clearStage: true` setzt `stage` auf `null`; mit `stage: 3, clearStage: true` ebenfalls
   `null`; ohne Schalter bleibt die Stufe (Fall in `PatchClearFieldTests`, Zeile in `PatchSemanticsTests`). **Rot**, wenn
   der Controller den Schalter nicht oder vor dem Wert anwendet.
3. `PositionFields` (Anlegen **und** Bearbeiten) zeigt ein Feld „Stufe“ als Auswahl mit „Standard der Übung“ plus genau
   den Manifest-Optionen des Übungstyps, und **kein** Stufen-Feld für einen Typ ohne Optionen (Vitest). **Rot**, wenn das
   Formular eine feste Liste (`VOCAB_FORMS`) benutzt oder das Feld bei leerer Liste dennoch zeigt.
4. `settingsToDto` sendet `stage` (Zahl oder `null`), `scope`, `speedThresholdSeconds`, `speedBonusPoints` (leer → `null`);
   `settingsToUpdateDto` sendet `clearStage: true` genau dann, wenn die gespeicherte Stufe gesetzt war und jetzt
   „Standard“ gewählt ist (Vitest in `PlanPositions.test.ts`, je Fall eine rote Probe). Das Anlegen schickt den Schalter
   nicht (`CreatePositionDto` kennt ihn nicht – sonst `unknown_field`).
5. Beim Anlegen setzt ein Wechsel der gewählten Übung die Stufe auf „Standard der Übung“ zurück (Vitest in
   `PlanPositionsAdd.test.tsx`). **Rot**, wenn der `onChange` der Radio-Zeile `stage` nicht zurücksetzt.
6. `Order`, `MaxBox`, `BoxIntervalDays`, `StageSchedule` bleiben bewusst außerhalb dieser Story (Entscheidungen 1+2) —
   kein Akzeptanzkriterium verlangt sie.

## Schätzung

**Größe: M** (unterer Rand) — vorher S als reines Frontend; G-4 bringt zwei additive Vertragsfelder (`Stages` im
Manifest, `ClearStage` im PATCH) mit je einem Integrationstest dazu. Vergleich: weniger als B-535 (drei Regeln,
Migration), mehr als ein reiner Formular-Nachtrag.

`wo: beides` · `migration: nein` (keine Spalte; `Stage` ist schon nullbar) · `vertragsbruch: nein` (beide Felder
additiv, `ClearStage` mit Vorgabe `false`; `Pugling.Client` braucht keine neue Methode)

**Risiken**

- Das Manifest wird an mehreren Stellen gelesen (`PositionProgressService.cs:53`, `:226`, KI-Creator `types`);
  ein additives Feld bricht keinen davon, aber `docs/api-examples` (Doku-Capture) ändert sich mit.
- Die Positions-Stufe ist anti-cheat-tragend (`StageValidation`: eine ungültige Stufe *verrät* Antworten). Die Auswahl
  ändert die Serverprüfung nicht – sie bleibt das Netz; das Formular macht Ungültiges nur unwählbar.
- `StageSchedule` (per API gesetzt) überstimmt je Box die Stufe; das Formular zeigt ihn nicht. Bleibt so (Entscheidung 2),
  die Feldhilfe zur Stufe nennt es.
- **Konflikt mit B-540 und B-140**: alle drei ändern `PlanPositions.tsx` (dieser die `PositionSettings`/`PositionFields`
  und den `onChange` der Radio-Zeile). Nacheinander bauen.

**Angriffsplan (Backend zuerst)**

1. `ExerciseTypeManifest.Stages` (additiv) + Anreicherung aus `StageOptions` an einer Stelle; AK 1 im
   `ExerciseTypeManifestTests`.
2. `UpdatePositionDto.ClearStage` + Controller (`PlanPositionsController.cs:262`, Schalter nach dem Wert); AK 2 in
   `PatchClearFieldTests`/`PatchSemanticsTests`; DTO-Doku nachziehen.
3. `contract.ts` neu erzeugen; `exerciseTypes.ts` bekommt `stages(type)` im Lookup.
4. `PlanPositions.tsx`: vier Felder in `PositionSettings`/`defaultSettings`/`settingsFrom`/`settingsToDto`/
   `settingsToUpdateDto`/`PositionFields`, Reset im Radio-`onChange`; Feldhilfe-Themen in `lib/fieldHelp.ts`.
5. Vitest (AK 3–5), Doku-Capture prüfen.

**Testweg**

- Integrationstests: `ExerciseTypeManifestTests` (AK 1), `PatchClearFieldTests` + Tabellenzeile in `PatchSemanticsTests`
  (AK 2).
- Vitest: `PlanPositions.test.ts` (AK 3, 4), `PlanPositionsAdd.test.tsx` (AK 5).
- Playwright: kein neuer Weg; `e2e/vater-von-null.spec.ts` muss grün bleiben (Happy-Path des Planbaus). Rollengang an der
  laufenden App: eine Vokabel-Position auf „Buchstabenkästchen“ stellen, im Sohn-Test erscheint die Stufe.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen `UpdatePositionDto`/`CreatePositionDto`
  (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:100-140`) und `PlanPositions.tsx:78-219`
  belegt — 8 von 19 Vertragsfeldern fehlen im Formular, darunter die namentlich gesuchte „Stufe" (`Stage`);
  keine gegenstandslose Frage (autonom geprüft, Nutzerauftrag 2026-08-04).
- **2026-08-03** — gegrillt: vier Entscheidungen getroffen — Reordering und Leitner-Feinjustage
  (`MaxBox`/`BoxIntervalDays`/`StageSchedule`) zurückgestellt (eigene Aufwände, kein akuter Bedarf), die
  vier übrigen Skalarfelder (`Stage`, `Scope`, `SpeedThresholdSeconds`, `SpeedBonusPoints`) werden ergänzt,
  `Stage` ohne eigene Validierung (bleibt bei B-79) (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, `wo: frontend`, kein Vertragsbruch, kein Backend-Schritt; Testweg
  neuer Vitest-Test für `PlanPositions` (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-10-23** — **Zurück auf `ausformuliert`** in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Die Lücke besteht (`stage`, `scope`, `speedThresholdSeconds`, `speedBonusPoints` fehlen in `PositionSettings`/`settingsToDto`, `PlanPositions.tsx`), aber **Entscheidung 4 ist überholt**: seit B-79 prüft der Server `Stage` (`PlanPositionsController.cs:112-114,147,222`), ein rohes Zahlenfeld liefe in `validation_error`. Neu zu entscheiden: Stufe als Auswahl (Optionen aus dem Manifest statt der fest kodierten `VOCAB_FORMS`?) und wie „zurück auf Standard“ geht, wenn PATCH `null` als „bleibt“ liest (`StudyPlanDtos.cs:133-135`). Die Datei hat 773 statt ~382 Zeilen, `PlanPositions.test.ts` existiert bereits; neu ist das Zeitfenster (`timeSlots`, `clearTimeSlots`).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-4 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) zur überholten Entscheidung 4: die Stufen kommen **als Auswahl je Übungstyp aus dem Server-Manifest** (nicht aus der fest kodierten `VOCAB_FORMS`), und „zurück auf Standard“ geht über einen **ausdrücklichen Leeren-Schalter** nach dem PATCH-Muster `Clear…`. Additiv im Vertrag. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-17); trifft er auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an.
- **2026-11-13** — Backlog-Abgleich gegen den Code: Ist-Stand-Abschnitt nachgezogen (Feldzahl 22, `settingsToDto` bei :282, Testdatei vorhanden). Lücke besteht.
- **2026-11-14** — Abgleich Backlog-Lauf: Lücke besteht unverändert (`settingsToDto` `PlanPositions.tsx:286-302` ohne `stage`/`scope`/`speed…`; Vertrag `UpdatePositionDto` `StudyPlanDtos.cs:180-185` mit 22 Feldern; Stufenprüfung `PlanPositionsController.cs:130`). Zeilen im Stand-Block berichtigt. Hinweis: der Status `ausformuliert` steht, obwohl G-4 (2026-10-23) die überholte Entscheidung 4 ersetzt hat – Entscheidungen 3/4 und AK 2 sprechen noch vom rohen Zahlenfeld; vor `gegrillt` nachziehen.
- **2026-11-14** — Refinement Backlog-Lauf: neu gegrillt nach G-4 – Entscheidung 3 berichtigt, 4 durch die Nutzer-Entscheidung ersetzt und ausgeformt (`ExerciseTypeManifest.Stages` aus `IExerciseType.StageOptions`), 5 `ClearStage` (Nutzer, G-4), dazu Stakeholder-Entscheidungen 6–8 im Auftrag des Nutzers (nur `Stage` bekommt einen Schalter, Übungswechsel setzt die Stufe zurück, Tempo-Felder im Combo-Muster); Ist-Stand um Manifest/PATCH-Belege ergänzt; AKs neu mit roter Probe je Fall. Geschätzt: M, `wo: beides`, keine Migration, kein Vertragsbruch (additiv).
- **2026-11-14** — Sprint 74, Frontend gebaut (Schritte 3–5): `ExerciseTypeLookup.stages(type)` (`frontend/src/lib/exerciseTypes.ts`); `PositionSettings` um `stage`/`stageStored`/`scope`/`speedThresholdSeconds`/`speedBonusPoints`, `settingsToDto`/`settingsToUpdateDto` (`clearStage`), `PositionFields` mit „Inhalte-Auswahl“, „Stufe“ (nur bei Manifest-Stufen, erste Option „Standard der Übung“) und zwei Tempo-Feldern, Stufen-Reset im Radio-`onChange` (`frontend/src/vater/PlanPositions.tsx`); Feldhilfe `scope`, `stage`, `speedThresholdSeconds`, `speedBonusPoints` (`lib/fieldHelp.ts`). Tests: `PlanPositions.test.ts` „Positions-Formular – Stufe, Auswahl, Tempo (B-16)“ (AK 4, fünf Fälle); neue Datei `PlanPositionsStufe.test.tsx` (eigenes Manifest, weil `loadExerciseTypes` modulweit cacht) mit „zeigt beim Anlegen …“ und „zeigt beim Bearbeiten …“ (AK 3) sowie „ein Übungswechsel setzt die Stufe … zurück“ (AK 5). Abweichung: AK 3 liegt nicht in `PlanPositions.test.ts` (reine `.ts`, kein Render).
