---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/backend, rolle/student, rolle/supervisor, rolle/creator]
aliases: [Rückmeldung Backend, ExerciseFeedback]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-623
grund: ""
ersetzt_durch: []
---

# B-628 · Rückmeldungen zur Karte: Speicher, Student-Endpunkt und die Sichten für Betreuer und Ersteller (Backend)

Teil 1 von 3 aus [B-623](B-623-feedback-knopf-an-der-karte.md), beim Schätzen am 2026-11-17 geteilt. Dieser Teil
baut das Fundament: Das Kind meldet zu einer Übung oder Karte einen oder mehrere feste Gründe und optional einen
Text; die Betreuer sehen und erledigen die Meldungen, der Ersteller sieht die gezählten Gründe an seiner Übung,
und das Kind erfährt, dass seine Meldung erledigt ist. Oberflächen: [B-629](B-629-rueckmeldung-frontend.md).
Wirkung beim Erstellen: [B-630](B-630-rueckmeldung-im-ki-creator.md).

## User Story

Als **Lernender** möchte ich zu einer Karte mit wenigen Tipps melden, was nicht stimmt oder gut war, und als
**Supervisor** möchte ich diese Meldungen mit Kontext sehen und erledigen, damit schlechte Übungen korrigiert und
neue besser werden.

## Ist-Stand am Code

Siehe [B-623 → Ist-Stand am Code](B-623-feedback-knopf-an-der-karte.md#ist-stand-am-code) (2026-11-17,
`847a5cfd`): kein Rückmeldeweg im Production-Build, `Remark` als Entwickler-Werkzeug, `ExerciseRating` nur
zwischen Creatorn, Karte über `ItemIndex` → `ItemId`/`VocabularyId` adressierbar. Ergänzt beim Schätzen:

- Letzte Migration der Kette: `backend/Pugling.Api/Data/Migrations/20261117061523_StudyPlanSeriesUnit.cs`.
- Die Löschregel am `ExerciseItem` ist uneinheitlich: Lernstand hängt mit `Cascade` daran
  (`backend/Pugling.Api/Data/PuglingDbContext.cs:688`), ein anderer Bezug mit `SetNull` (`:698`).

## Entscheidungen

Übernommen aus [B-623](B-623-feedback-knopf-an-der-karte.md#entscheidungen) (Grill-Runde mit dem Vater,
2026-11-17); dort stehen Begründung und Kosten. Diesen Teil binden **E2** (Reichweite je Grund – das Backend
liefert sie mit), **E4** (acht Gründe, Mehrfachauswahl, Grund oder Text), **E5** (im Test erst nach dem Abgeben),
**E6** (keine Wirkung auf Lernstand, Punkte, Pflicht, Malus, Cursor), **E7** (zwei Sichten), **E8** (Hinweis an
das Kind nach *erledigt*) und **E9** (neue Entität, Karte optional, Zusammenlegen).

## Akzeptanzkriterien

1. (B-623 AK2) Ein `POST` unter `student/…` legt die Rückmeldung mit Kind, Plan, Position, Übung und – falls
   angegeben – Karte an. Fremde Kontext-Ids (fremdes Kind, fremde Position, Karte einer anderen Übung) werden
   mit `404`/`403` abgewiesen. Ohne Grund **und** ohne Text: `400`.
2. (B-623 AK3) Kasten, Punkte, Pflicht, Malus, Wallet und Sitzungs-Cursor sind nach der Meldung gleich wie davor.
3. (B-623 AK4) Während eines laufenden Abschlusstests wird eine Meldung zu dessen Karten abgewiesen; nach dem
   Abgeben ist sie erlaubt.
4. (B-623 AK5) Derselbe Grund desselben Kindes an derselben Karte erzeugt, solange offen, keine zweite Zeile:
   Zähler hoch, Freitext angehängt.
5. (B-623 AK6) `supervisor/…` liefert die Meldungen der eigenen Kinder (Multi-Supervisor), filterbar nach Kind
   und Status, mit Übung, Karte, letzter Antwort des Kindes und Freitext; `PATCH` setzt *erledigt* für alle
   Betreuer. Ein fremder Supervisor sieht nichts.
6. (B-623 AK7) `creator/exercises/{id}/…` liefert dem Ersteller die gezählten Gründe seiner Übung (je Karte und
   gesamt) **ohne** Freitext und **ohne** Kind.
7. (B-623 AK9) Das Kind liest seine erledigten, noch nicht gesehenen Meldungen und quittiert sie einmalig.
8. Die Reichweite (nur dieses Kind / alle) ist je Grund im Vertrag abgebildet, sodass B-630 sie nicht neu
   erfindet.

## Schätzung

**Größe: L** — wie eine DB-Umbau-Etappe: eine neue Entität mit Migration, Endpunkte auf allen drei Ebenen,
ein Eigentums-Check über Kind/Plan/Position/Karte und ein Rückkanal an das Kind.

- **`migration: ja`** — neue Tabelle(n), angehängt an `20261117061523_StudyPlanSeriesUnit` (bzw. an die
  jüngste, falls B-618/B-621 vorher landen). Rein additiv, Bestandszeilen unberührt.
- **`vertragsbruch: nein`** — nur neue DTOs, ein neues Enum und neue Routen in `Pugling.Contracts`; nichts
  Bestehendes ändert sich. `Pugling.Client` bekommt neue Methoden (B-630 braucht sie).

### Risiken

1. **Die Karte verschwindet beim Bearbeiten der Übung.** Ein `PUT` auf Inline-Typen ersetzt Items
   ([B-328](B-328-inline-typen-put-verschiebt-lernstand.md)). Mit `Cascade` gingen die Meldungen genau dann
   verloren, wenn der Vater die gemeldete Karte korrigiert. Darum Karte mit **`SetNull`** und
   `VocabularyId` als Momentaufnahme, damit „falsch übersetzt: *voyage*“ die Korrektur überlebt.
2. **Lösungsfeld-Regel.** Die Sichten zeigen die Antwort des Kindes; ein Feld namens
   `Answer`/`Solution`/`CorrectAnswer`/`Translation` im Nutzlast-Graphen eines Student-Endpunkts ließe
   `ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated` rot werden. Die
   Student-Antworten (POST, Hinweis nach *erledigt*) tragen deshalb nur Ids, Gründe und Status.
3. **Mehrfachauswahl und Zusammenlegen.** Zusammengelegt wird je **Grund** (E9), abgegeben aber mehrere Gründe
   mit einem Text. Modell: eine Zeile je (Kind, Karte bzw. Übung, Grund) mit Zähler und angehängten Texten –
   kein JSON-Listenfeld (sonst Tor G7/`ValueComparer`).
4. **Ein Abschlusstest ist im Gang.** Die Prüfung „Test abgegeben?“ hängt am Versuch; der Student-Endpunkt
   braucht die Versuchs-Id oder leitet sie serverseitig ab, sonst lässt sich E5 umgehen.
5. **Echte DB.** Die Migration läuft gegen `pugling.db` der Söhne; das Startskript sichert vorher.

### Angriffsplan

1. Entität(en) + `DbContext`-Konfiguration (`SetNull` an der Karte) + Migration; `SchemaGuardTests` grün.
2. Vertrag: Enum der Gründe mit Reichweite, DTOs, Client-Methoden.
3. Student-`POST` mit Eigentums-Check nach dem Muster `RemarksController.cs:171`, Test-Sperre, Zusammenlegen.
4. Supervisor-Liste + `PATCH` *erledigt*; Creator-Zählung; Student-Hinweis + Quittung.
5. Doku-Capture und Rollen-Tutorials nachziehen (`DocsCaptureTests`).

### Testweg

- Neue Integrationstestklasse `ExerciseFeedbackTests` in `Pugling.Api.Tests`: je AK ein Fall, dazu je Ebene
  „fremd sieht nichts“; AK2 als Vorher/Nachher-Vergleich von Kasten, Wallet und Pflicht-Report.
- `ConventionGuardTests` und `SchemaGuardTests` laufen im Test-Tor mit.
- `/smoke-test` gegen die Wegwerf-DB: Kind meldet, Vater erledigt, Kind quittiert.
- Reviewer: `pugling-reviewer`.

## Verlauf

- **2026-11-17** — angelegt beim Schätzen von B-623 (Teil 1 von 3: Backend), geschätzt L.
