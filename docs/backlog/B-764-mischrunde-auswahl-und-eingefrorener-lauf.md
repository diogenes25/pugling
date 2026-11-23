---
tags: [typ/story, status/in-arbeit, bereich/training, bereich/gamification, rolle/student]
aliases: [Mischwiederholung Auswahl, Mischrunde Start]
status: in-arbeit
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
wartet_auf: pugling-reviewer (Review am Sprintende 130, gemeinsam mit B-765)
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
- 2026-11-23 · in-arbeit (Nachtlauf H, Sprint 130, Freigabe 8: `geschaetzt` + `Wunsch`). Gebaut: Tabellen `MixedReviewRuns`/`MixedReviewCards` (Migration `20261123220000_MixMixedReviewRuns`, nur additiv, FKs Cascade, gefilterter Unique-Index „ein offener Lauf je Kind"; vier Zeilen in `SchemaGuardTests`), `MixedReviewService` (Auswahl `SelectAsync`, Start/Fortsetzen `StartAsync`, Übersicht), `MixedReviewController` unter `student/children/{childId}/mixed-review` (`GET`, `POST runs`, `ChildOwnershipFilter`, Start nur der Student), Fehlercodes `mixed_review_empty`, `mixed_review_completed`, `mixed_review_stale`, Option `MixedReviewRoundSize` = 10, Contract `MixedReviewDtos`, Client `GetMixedReviewAsync`/`StartMixedReviewAsync`. Die Sperre liest dieselbe Tagessicht wie das Bonus-Training (`ExtraTrainingService.GateOf`, aus `GateAsync` herausgezogen, Verhalten unverändert).
- 2026-11-23 · Auslegung innerhalb der Entscheidungen, **keine neue Produktentscheidung**, aber zur Prüfung am Morgen benannt: (a) Entscheidung 4 „nur Karten mit Zeile" ist als „eingeführt und, wo nötig, eingeprägt" gebaut (`PositionPlayService.Known`, B-643) – ihr Grund (keine Erstbegegnung vorbei an Dosis und Lernkarte) gilt für eine eingeführte, nicht eingeprägte Karte genauso; (b) Risiko 3 entschieden als „ein Wort, das zwei Positionen teilen, einmal je Runde, aus der überfälligeren Position" – die andere Karte bleibt fällig; (c) Stufe einer Karte = `TestTargetStage(StageForDay(…))`: ein Lernkarten-Tag wird wie im Test an seinem Ziel gefragt, ein reiner Anzeige-Tag gibt keine Karte; (d) eine offene Runde vom Vortag wird beim Start verworfen und neu gezogen (ihre Karten waren gestern fällig); (e) Positionen mit Aufbau-Weg spielen über `PlayedExercise(pos, heute)` mit – die Stufen-Logik ist nur gelesen, nicht verändert; (f) `GradesAsSet`-Listen (beliebige Reihenfolge) geben keine Karte.
- 2026-11-23 · AK-Fälle → Test (`backend/Pugling.Api.Tests/MischrundeAuswahlTests.cs`) → Mutante: AK 1 + AK 2 → `Runde_Nimmt_Faellige_Karten_Aus_Zwei_Plaenen_Und_Keine_Kuenftige` (M4 Fälligkeitsfilter weg → rot, M8 Sortierung weg → rot) · AK 3 → `Position_Mit_Offener_Pflicht_Gibt_Keine_Karte` (M2 → rot) · AK 4 → `Fremdes_Kind_Verboten_Vater_Liest_Nur` (fremdes Kind 404 wie unbekannt, Vater liest 200, startet 403; M9 → rot) · AK 5 → `Inaktiver_Und_Abgelaufener_Plan_Geben_Keine_Karte` und `Auswahl_Prueft_Selbst_Ob_Der_Plan_Spielbar_Ist` (M12 eigene Prüfung weg → rot; M10 Rollup-Prüfung weg → grün, erwartet: die eigene Prüfung trägt dann) · AK 6 → `Nie_Gesehene_Karte_Kommt_Nicht_Vor` (M3 Einführungsfilter weg → rot, M5 Einprägung weg → rot) · AK 7 → `Vor_Den_Tageszielen_Gesperrt_Danach_Offen` (M1 → rot) · AK 8 → `Offene_Runde_Wird_Fortgesetzt` + `Offene_Runde_Von_Gestern_Wird_Neu_Gezogen` (M6 → rot; M11 „nie nachschlagen" fällt nur bei „von gestern", nicht bei „fortgesetzt": das Fortsetzen hält zweifach – Nachschlagen **und** Unique-Index mit Fortsetzen im `catch` –, eine Einzelmutante macht es nicht rot; als Beleg für AK 8 zählt darum nur der grüne Doppelstart gegen den echten Index) · AK 9 → `Start_Legt_Keine_Lernsitzung_An` (**Absichtserklärung**: kein Code des Diffs legt eine Sitzung an, keine Mutante macht ihn rot) · Risiko 3 → `Dasselbe_Wort_In_Zwei_Positionen_Kommt_Einmal` (M7 → rot) · Leerfall → `Nichts_Faellig_Leere_Runde_Wird_Abgewiesen`.
- 2026-11-24 · Mutanten-Probe nach der letzten Änderung (`.claude/scripts/mutant-probe.py`, gemessen an `a782b4b2 + Arbeitsbaum`, 00:15): M1–M9, M12 rot am benannten Test; M10 grün wie erwartet; M11 abweichend (s. o.); Grundlinie nach dem Zurückspielen grün. Erste Messung (23:51): M1 `if (false)` kompilierte nicht (CS0162 unter `TreatWarningsAsErrors`) → neu formuliert und gemessen. Rote Probe in Zahlen: `MischrundeAuswahlTests` ohne Mutante 12/12 grün, je rote Mutante ≥ 1 gefallen am benannten Test.

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| M1 | MixedReviewService.cs:60 (Sperre) | red | red |
| M2 | MixedReviewService.cs:135 (offene Pflicht) | red | red |
| M3 | MixedReviewService.cs:142 (eingeführt) | red | red |
| M4 | MixedReviewService.cs:143 (fällig) | red | red |
| M5 | MixedReviewService.cs:167 (eingeprägt) | red | red |
| M6 | MixedReviewService.cs:64 (Runde von gestern) | red | red |
| M7 | MixedReviewService.cs:175 (Doppel-Wort) | red | red |
| M8 | MixedReviewService.cs:174 (Sortierung) | red | red |
| M9 | MixedReviewController.cs:42 (nur Student) | red | red |
| M10 | PositionProgressService.cs:467 (Rollup `Active`) | green | green |
| M11 | MixedReviewService.cs:63 (Nachschlagen) | red | abweichend |
| M12 | MixedReviewService.cs:124 (`PlanPlayableForChild`) | red | red |

- 2026-11-24 · Suite (Release) 2186/2187: rot war nur der Wächter `ProblemTextGuardTests.Every_409_Code_Has_A_German_Text_Or_A_Reasoned_Exception` (erwartet 0 Codes ohne deutschen Text, gemessen 3: `mixed_review_completed`, `mixed_review_empty`, `mixed_review_stale`). Behoben: deutsche Texte in `frontend/src/lib/api.ts` (`GERMAN_PROBLEM_TEXT`) und `i18n/de.ts`/`en.ts`; danach die Wächter-Klassen (ProblemText, Schema, OpenApi, EndpointCoverage, Ownership) 47/47 grün. **Fehlerzähler Sprint 130: 1** (roter Wächter, Behebung ändert Code – Freigabe 11). `dotnet format --verify-no-changes` auf den berührten Dateien: ein Objekt-Initialisierer in `MischrundeAuswahlTests.cs` einzeilig → formatiert (ohne Zählung, Kosmetik).
