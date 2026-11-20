---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [Meldung Bezug Antwort, Ausgang berechtigt]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: "B-627"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-767 · Eine Meldung zeigt auf die beanstandete Test-Antwort, und der Vater erledigt sie mit „berechtigt“ oder „abgewiesen“ (Backend)

Der erste Teil von [B-627](B-627-berechtigte-meldung-bucht-nach.md): die Meldung friert beim Absenden den Bezug zur **Antwort** ein, und der Supervisor erledigt sie mit einem **Ausgang**.
**Bucht noch nichts um**; das tut B-768. Ohne diesen Teil lässt sich nichts umbuchen, was nicht bloß „irgendeine“ Antwort trifft.

## User Story

Als **Supervisor** möchte ich eine Meldung meines Kindes als berechtigt oder abgewiesen erledigen, damit klar ist, ob die App unrecht hatte.

## Ist-Stand am Code

Pfade relativ zu `backend/Pugling.Api/`; gemessen am 2026-11-20.

- `ExerciseFeedback` trägt Kind, Position, Übung, `ItemIndex`, `ExerciseItemId`, `VocabularyId`, Grund, Zähler, Text und `Status`; einen Verweis auf die **Antwort**: **nicht gefunden** (`Models/FeedbackEntities.cs:12-67`).
- `FeedbackStatus` ist zweiwertig (`Open`, `Done`, `Pugling.Contracts/Common/FeedbackBaseTypes.cs:40-46`); `UpdateFeedbackDto(FeedbackStatus Status)` (`Pugling.Contracts/Supervisor/FeedbackDtos.cs:33`).
- Absenden: `ExerciseFeedbackService.ReportAsync` (`Services/Shared/ExerciseFeedbackService.cs:40`) **führt offene Meldungen mit gleichem Kind, gleicher Übung, gleicher Karte und gleichem Grund zusammen** (Zähler hoch, Text angehängt, `:49-52`) und überschreibt dabei `PlanPositionId` (`:70`). Die Position ist **nicht** Teil dieses Schlüssels.
- Die Antwort im Vater-Web ist eine Heuristik: die jüngste gespeicherte Antwort des Kindes zur Karte (`ChildAnswerAsync`, `:123-151`).
- Erledigen: `FeedbackController.Update` (`Controllers/Supervisor/FeedbackController.cs:51-62`); ein fremdes Kind liefert `404`, wie ein unbekannter Id.
- Ein `TestItemResult` hat keinen eigenen `DbSet`, es ist über seinen Versuch erreichbar (`Models/StudyPlanEntities.cs:209-226`).

## Die echte Lücke

Ein nullbarer Bezug an der Meldung, ein Ausgang am Erledigen und eine Regel für die Zusammenführung, die den Bezug mitnimmt.

## Entscheidungen

*Entscheidungen stammen aus [B-627](B-627-berechtigte-meldung-bucht-nach.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern verweisen dorthin.*

1. **Der Bezug wird beim Absenden eingefroren** (B-627 Entscheidung 1): der Server sucht die jüngste gespeicherte **Test**-Antwort des Kindes (nicht vom Supervisor gestartet, abgeschlossen) zur Karte der Position und speichert deren `TestItemResult`-Id in
   der Meldung. Gibt es keine, bleibt der Bezug leer und die Meldung kann nicht „berechtigt“ gebucht werden. Der Client schickt die Id **nicht** mit. *Kosten:* neue nullbare Spalte mit FK (`SetNull`), Migration.
2. **Ausgang als eigenes Feld, `Done` bleibt** (B-627 Entscheidung 4): nullbarer Ausgang an der Meldung (`Justified` oder `Rejected`, als Text gespeichert), additives optionales Feld in `UpdateFeedbackDto` und in der Antwort;
   `FeedbackStatus` bleibt zweiwertig, `Done` ohne Ausgang bleibt für Altbestand. *Kosten:* Spalte und ein Feld.
3. **Beim Schätzen nachgetragen (Annahme, unbestätigt): die Zusammenführung berücksichtigt den Bezug.** Eine Meldung wird nur dann in eine offene zusammengeführt, wenn auch der Bezug **gleich** ist (beide leer zählt als gleich); eine
   Meldung zu einer **anderen** Antwort derselben Karte öffnet eine neue Zeile. Sonst bezöge sich eine zusammengeführte Meldung auf mehrere Antworten, und B-627 Entscheidung 1 („je Antwort eine Meldung“) wäre nicht zu halten.
   *Kosten:* mehr Zeilen. *Verworfen:* den Bezug beim Zusammenführen auf die jüngste Antwort umbiegen (die ältere bliebe unerledigt, ohne dass jemand es merkt).
4. **Wer entscheidet** (B-627 Entscheidung 8): nur ein Supervisor des Kindes; **ein fremder Supervisor erhält `404`** wie bisher (`FeedbackController.cs:57-58`). Das weicht vom Wortlaut von B-627 AK 9 („403“) ab: der Bestand antwortet `404`,
   damit die Antwort nicht verrät, welche Ids es gibt; das Kind erhält `403` über die Rollensperre.

## Akzeptanzkriterien

Nummerierung folgt B-627.

1. (AK 1) Eine Meldung zu einer Test-Antwort speichert beim Absenden den Bezug zu **dieser** Antwort (`TestItemResult`-Id); gibt es keine gespeicherte Antwort, bleibt der Bezug leer. *Rot, wenn nach dem Absenden kein Bezug in der Zeile steht oder der Bezug erst beim Bestätigen gesucht
   wird (Test: zwischen Meldung und Bestätigung beantwortet das Kind die Karte erneut, der Bezug bleibt der gemeldete).*
2. (AK 2) Der Supervisor erledigt mit „berechtigt“ oder „abgewiesen“; `Done` allein bleibt für Altbestand. *Rot, wenn der Vertrag keinen Ausgang kennt.*
3. (AK 9, Teil) Nur der Supervisor dieses Kindes setzt den Ausgang; ein fremder Supervisor erhält `404`, das Kind `403`. *Rot, wenn die Rollen- oder Eigentumsprüfung fehlt.*
4. (AK 10, **Absichtserklärung**) Eine Meldung ohne Bestätigung ändert weiter nichts (B-623 Entscheidung 6, `FeedbackEntities.cs:8-10`). Der Ausgangszustand erfüllt das schon; rot würde nur, wer die Meldung selbst wirken ließe. Der vorhandene Test
   `Meldung_aendert_weder_Kasten_noch_Wallet_noch_Cursor` hält es.
5. (**neu, aus Entscheidung 3**) Zwei Meldungen mit gleichem Grund an derselben Karte, aber zu **verschiedenen** Test-Antworten, ergeben zwei Zeilen; zu derselben (oder ohne) Antwort eine Zeile mit Zähler 2. *Rot, wenn die Zusammenführung den Bezug ignoriert.*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector` (B-03): zwei Spalten, eine Suche, eine geänderte Zusammenführung, ein optionales Vertragsfeld.

Nachgesehen am 2026-11-20:

- **`wo: backend`.** Die Oberfläche ist B-769.
- **`migration: ja`.** Zwei nullbare Spalten an `ExerciseFeedback` (Bezug mit FK und `SetNull`, Ausgang); der Ausgang als Text (Tor `Persistierte_Enums_Sind_Strings`, `SchemaGuardTests.cs:137`), FK-Verhalten im Tor `:194`.
- **`vertragsbruch: nein`.** `UpdateFeedbackDto` bekommt ein **optionales** Feld, `FeedbackResponse` additive Felder; `FeedbackStatus` bleibt. Das erzeugte Frontend-Schema (`frontend/src/lib/contract.ts`) zieht nach; die `unknown_field`-Wächter lehnen Felder ab, die der Vertrag nicht kennt, nicht neue optionale.
- **Wallet:** berührt das Konto nicht.

### Risiken

1. **Die Zusammenführung** (Entscheidung 3) ändert den Schlüssel einer vorhandenen Regel; der Test `Gleicher_offener_Grund_an_derselben_Karte_wird_zusammengelegt` muss unverändert grün bleiben (dort ohne Bezug).
2. **Die Meldung kennt die Position,** nicht den Versuch; die Suche geht über `PlanPositionId` und `ItemIndex`. Ein Versuch ohne `ItemIndex` (falsche Erwähnung im Mengenmodus) trägt keinen Bezug.
3. **Eine Meldung im Üben einer Vokabelkarte** hat im Test vielleicht gar keine Antwort; der Bezug bleibt leer, und „berechtigt“ bucht nichts um. Das ist Entscheidung 2 von B-627 (nur Test), kein Fehler.
4. **Alle Spiel-Meldungen vor dem Neustart** haben keinen Bezug (Altbestand) – gewollt, nicht rückfüllen.

### Angriffsplan

1. Modell, Migration, Tor-Einträge.
2. `ReportAsync`: Bezug suchen und einfrieren, Schlüssel der Zusammenführung um den Bezug erweitern.
3. `UpdateFeedbackDto` und `FeedbackResponse` additiv erweitern; `SetStatusAsync` nimmt den Ausgang.
4. Contract, Client; `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- `ExerciseFeedbackTests` (vorhanden, Aufbau und Fälle dort): neue Fälle für AK 1, 2, 3, 5; die vorhandenen bleiben unverändert grün (AK 4 ist dort schon als `Meldung_aendert_weder_Kasten_noch_Wallet_noch_Cursor` abgedeckt, eine Absichtserklärung).
- Rote Proben: AK 1 – der Bezug wird erst beim Erledigen gesucht (Test mit zweiter Antwort dazwischen); AK 5 – die Zusammenführung ignoriert den Bezug.
- `OwnershipMatrixTests` laufen mit.
- Vor der Abnahme: `pugling-reviewer`.

## Verlauf

- 2026-11-20 · aus B-627 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): M, backend, Migration ja, kein Vertragsbruch. Zusammenführung berücksichtigt den Bezug (Annahme, unbestätigt); fremder Supervisor bekommt weiter `404`. Reihenfolge B-767, B-768, B-769; nach B-624, B-625 und B-626 messen.
