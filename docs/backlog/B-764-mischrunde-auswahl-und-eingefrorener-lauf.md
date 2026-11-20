---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/gamification, rolle/student]
aliases: [Mischwiederholung Auswahl, Mischrunde Start]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: "B-218"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-764 · Mischrunde: fällige Karten über alle spielbaren Pläne wählen und als eingefrorenen Lauf starten (Backend)

Der erste Teil von [B-218](B-218-mischwiederholung-ueber-alle-positionen.md): welche Karten in eine Mischrunde kommen und wie der Lauf angelegt und
fortgesetzt wird. **Zahlt noch nichts** und bewegt noch keinen Kasten; das tut B-765.

## User Story

Als *Schüler* möchte ich eine kurze Wiederholungsrunde aus den Karten, die in all meinen Positionen gerade fällig sind, damit nichts Fälliges liegen bleibt;
als *Vater* möchte ich, dass die Runde freiwillig bleibt und keine offene Pflicht aufzehrt.

## Ist-Stand am Code

Pfade relativ zu `backend/Pugling.Api/`; gemessen am 2026-11-20.

- Eine Mischrunde gibt es nicht; alle Spielwege hängen an einer Position (`Controllers/Student/PositionPracticeController.cs:19`).
- Fälligkeit je Position: `PositionItemProgress.DueOn` (`Models/PlanPositionEntities.cs:256`); `IsDue` heißt „nie gesehen **oder** `DueOn <= Tag`“
  (`Services/Shared/PositionPlayService.cs:629-630`), `DueItemIndicesAsync` liefert die Indizes je Position.
- Spielbare Pläne: `PositionPlayService.PlanPlayableForChild` (`:66-67`).
- Das Bonus-Training: `ExtraTrainingService` (`Services/Student/ExtraTrainingService.cs:28`), Sperre `GateAsync` (`:258-270`), Lauf `ExtraTrainingRun` mit **genau einer** Übung und
  einem Index, der je (Kind, Übung) höchstens einen offenen Lauf hält (`Models/GamificationEntities.cs:124`, `PuglingDbContext.cs:891`).
- Eine **leere** Lern-Runde einer Position mit Pool gilt als erfüllt (`PositionProgressService.cs`, `PlayedRoundMeetsGoalAsync`, „nothing was due“).

## Die echte Lücke

Die Auswahl über Positionsgrenzen und ein Lauf, der Karten aus mehreren Positionen trägt. Der vorhandene `ExtraTrainingRun` trägt eine Übung und taugt nicht.

## Entscheidungen

*Entscheidungen stammen aus [B-218](B-218-mischwiederholung-ueber-alle-positionen.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern verweisen dorthin.*

1. **Eine Spielart neben dem Bonus-Training, mit dessen Sperre** (B-218 Entscheidung 1): die Runde öffnet erst nach den Tageszielen (`GateAsync`). Der gemeinsame Deckel
   gehört zu B-765.
2. **Auswahl** (B-218 Entscheidung 3): nur spielbare Pläne, nur Positionen mit `UseLeitner`, **ohne offene Pflicht der laufenden Periode**; sortiert nach Überfälligkeit,
   dann niedrigste Box; Rundengröße 10 als Option (`ExtraTrainingOptions`). Typen: nur Vokabel zunächst (Entscheidung 7).
3. **Keine Lern-Sitzung** (B-218 Entscheidung 5): der Lauf legt keine `PracticeSession` mit `Mode = Lern` an.
4. **Beim Schätzen nachgetragen (Annahme, unbestätigt): nur Karten mit vorhandener `PositionItemProgress`-Zeile.** `IsDue` zählt eine **nie gesehene** Karte als fällig; ohne diese
   Einschränkung führte die Mischrunde neue Karten ein, vorbei an Tagesdosis und Erstbegegnung. *Kosten:* ungesehene Karten kommen nie über die Mischrunde. *Verworfen:* die
   Einschränkung weglassen.
5. **Eigene Lauf-Entität statt `ExtraTrainingRun` zu erweitern** (B-218 Entscheidung 1 ließ das offen): `ExtraTrainingRun` hängt an einer Übung, einer Form und einem
   Inhalts-Fingerabdruck, die Mischrunde an mehreren Positionen. *Kosten:* zweite Tabelle plus Kartenzeilen. *Verworfen:* eine nullbare Übung am vorhandenen Lauf (jede
   Lesestelle müsste den Sonderfall kennen).

## Akzeptanzkriterien

Nummerierung folgt B-218.

1. (AK 1) Hat ein Kind in zwei Positionen aus zwei aktiven Plänen je eine fällige Karte ohne offene Pflicht, bietet die Runde beide an. *Rot, wenn die Auswahl auf eine Position beschränkt bleibt.*
2. (AK 2) Eine Karte mit `DueOn` in der Zukunft kommt nicht vor. *Rot, wenn nach `onlyWeak` statt nach `DueOn` gefiltert wird.*
3. (AK 3) Eine fällige Karte einer Position mit offener Pflicht der laufenden Periode kommt nicht vor, und die Pflicht bleibt offen. *Rot, wenn die Auswahl diese Positionen nicht ausschließt.*
4. (AK 9) Ein Student-Aufruf mit fremdem `childId` ist verboten; der Supervisor darf lesen, nicht spielen. *Rot, wenn die Route ohne `ChildOwnershipFilter` hängt.*
5. (AK 10) Ein inaktiver oder abgelaufener Plan liefert keine Karte. *Rot, wenn die Auswahl `PlanPlayableForChild` auslässt.*
6. (**neu, aus Entscheidung 4**) Eine nie gesehene Karte kommt nicht vor. *Rot, wenn die Auswahl `IsDue` unverändert übernimmt.*
7. (**neu, aus Entscheidung 1**) Vor den Tageszielen liefert der Start die Sperre (`extra_training_locked`), nach ihnen den Lauf. *Rot, wenn die Runde das Tor umgeht.*
8. (**neu, aus der Konvention des Bonus-Trainings**) Ein offener Lauf wird fortgesetzt, nicht neu gezogen. *Rot, wenn zweimal Starten zwei Läufe oder zwei Kartensätze liefert (Doppeltipp).*
9. (**neu, aus Entscheidung 3**) Der Start legt keine `PracticeSession` an. *Rot, wenn nach dem Start eine Lern-Sitzung in der Pflicht-Auswertung zählt.*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector` (B-03): eine Auswahlabfrage über bekannte Bausteine, ein neuer Lauf mit Kartenzeilen, ein Start- und ein Lese-Endpunkt.

Nachgesehen am 2026-11-20:

- **`wo: backend`.** Die Oberfläche ist B-766.
- **`migration: ja`.** Neue Tabellen `MixedReviewRun` (Kind, Tag, Start, Abschluss, Münzen) und `MixedReviewCard` (Lauf, Position, Index, Stufe, Antwort, Ergebnis); FKs mit `Cascade`; Einträge in `SchemaGuardTests`.
- **`vertragsbruch: nein`.** Neue Routen und Antworten, nichts Bestehendes ändert sich.
- **Wallet:** zahlt nicht (B-765); kein Bump nötig.

### Risiken

1. **Offene Pflicht je Position bestimmen** über `HasDutyOn` und `IsGoalMetAsync` (`PositionProgressService.cs`) je Periode; für Wochenpflichten muss die Periode gelten, nicht der Tag.
2. **Ungesehene Karten** (Entscheidung 4) – ohne die Einschränkung ein Schlupfloch an der Tagesdosis vorbei.
3. **Dieselbe Übung in zwei Positionen:** eine Karte kann zweimal vorkommen (zwei Kästen); die Auswahl nimmt je (Position, Index), die Runde darf sie doppelt zeigen oder muss entduplizieren. Offen für den Bau.
4. **Fremde, ungecommittete Arbeit** ändert dieses Backend nicht.
5. **Überschneidung mit B-660:** B-660 zuerst; B-764 ändert dort nichts am Bonus-Pfad.

### Angriffsplan

1. Modell und Migration, Tor-Einträge.
2. `MixedReviewService.SelectAsync` (Auswahl) und `StartAsync` (Sperre, Einfrieren, Fortsetzen).
3. Controller unter `student/children/{childId}/mixed-review` (Vorbild `ExtraTrainingController`), `ChildOwnershipFilter`.
4. Contract, Client; `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `MischrundeAuswahlTests` (Vorbild `ExtraTrainingTests`, Aufbau über `TestApi.SeedLeitnerPosition`): AK 1 bis 9.
- Rote Proben: AK 3 – jemand lässt die Pflichtprüfung weg (dann entsteht nach der Runde eine leere, „erfüllte“ Pflicht-Runde); AK 6 – jemand übernimmt `IsDue` unverändert.
- `OwnershipMatrixTests` und `EndpointCoverageGuard` für die neuen Routen.
- Vor der Abnahme: `pugling-reviewer`.

## Verlauf

- 2026-11-20 · aus B-218 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): M, backend, Migration ja, kein Vertragsbruch. Reihenfolge B-660, dann B-764, B-765, B-766 (nicht parallel mit B-660, beide hängen am Bonus-Training).
