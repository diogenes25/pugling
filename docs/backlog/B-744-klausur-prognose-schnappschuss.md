---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/gamification, rolle/supervisor]
aliases: [Prognose-Schnappschuss, PredictedLevel]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-683
grund: ""
ersetzt_durch: []
---

# B-744 · Am Klausurtag die Prognose festhalten: `Klassenarbeit.PredictedLevel`

Ohne gespeicherte Prognose kann die echte Note nichts eichen. Am `ScheduledDate` der Klassenarbeit wird die laufende Stufe des Fachs
einmalig an die Klassenarbeit geschrieben. Abgespalten aus [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (Entscheidung 5, erster
Teil). Baut auf [B-741](B-741-notenstufe-berechnen-und-speichern.md) auf; die Wirkung der Note
([B-745](B-745-klausurnote-wirkt-auf-die-notenstufe.md)) und die Anzeige ([B-746](B-746-klausur-abweichung-fuer-den-vater.md)) folgen.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-683.*

## User Story

Als *Vater* möchte ich, dass die Prognose am Tag der Arbeit festgehalten wird und nicht erst Tage später beim Eintragen der Note,
damit der Vergleich zwischen Prognose und Note ehrlich ist.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- `Klassenarbeit` trägt Kind, Fach (nullable), Titel, Thema, `ScheduledDate`, Status, `Grade`, `GradeComment`, `CreatedAt`
  (`Models/KlassenarbeitEntities.cs:67-91`); eine Prognose oder ein Schnappschuss: nicht gefunden.
- Das Datum ist Pflicht (`DateOnly`, `:78`), das Fach nicht (`SubjectId` nullable, `:72`).
- Ein Tageslauf für Klassenarbeiten existiert nicht. Träge Läufe hängen an der Kind-Anmeldung
  (`Controllers/AuthController.cs:82-90`) und am Kauf (`Controllers/Student/MeController.cs:296`).
- Die Stufe je Fach kommt aus B-741 (Tabelle `ChildSubjectLevel`).
- Migrationskette: jüngstes Glied `20261119065732_ShopStockPerChild`.

## Entscheidungen

1. **Der Schnappschuss ist eine neue nullable Spalte `Klassenarbeit.PredictedLevel`, vom täglichen Lauf des `ScheduledDate`
   einmalig gesetzt** (B-683 Entscheidung 5). Kosten: eine Migration; Klassenarbeiten ohne Lauf am Klausurtag (kein Zugriff des
   Kindes an diesem Tag) haben keinen Schnappschuss und eichen nicht. Verworfen: die Prognose zum Zeitpunkt der Noteneintragung.
   Schließt aus: Altmaterial ohne Prognose als Eichpunkt.
2. **Nur Klassenarbeiten mit Fach bekommen einen Schnappschuss** (B-683 Entscheidung 7). Kosten: Der Vater muss das Fach setzen.
   Schließt aus: eine Note, die ein falsches Fach hebt oder senkt.
3. **Der Lauf schreibt keine Münzen** (B-683 Entscheidung 8).

## Akzeptanzkriterien

1. Der Lauf am `ScheduledDate` schreibt die laufende Stufe des Fachs einmal an die Klassenarbeit; ein zweiter Lauf überschreibt sie
   nicht. *Rot, wenn* der Schnappschuss fehlt oder beim zweiten Lauf ersetzt wird.
2. Vor dem `ScheduledDate` bleibt `PredictedLevel` leer, an einem späteren Tag ohne Lauf am Klausurtag ebenfalls. *Rot, wenn* der
   Lauf an einem anderen Tag als dem Klausurtag schreibt.
3. Eine Klassenarbeit ohne Fach bekommt keinen Schnappschuss. *Rot, wenn* sie das Fach „irgendeines" trifft.
4. Fehlt dem Kind im Fach eine Stufe (kein Maß), bleibt der Schnappschuss leer, statt mit einem Ersatzwert gefüllt zu werden; am
   Klausurtag selbst darf ein später entstehender Wert ihn noch füllen, an keinem anderen Tag. *Rot, wenn* der Lauf bei fehlender
   Stufe 0, 6 oder einen anderen Ersatzwert schreibt.
5. Das Verändern des `ScheduledDate` danach ändert einen schon gesetzten Schnappschuss nicht. *Rot, wenn* ein verschobener Termin
   den Schnappschuss löscht oder neu setzt.
6. Der Lauf bucht nichts aufs Kassenbuch. *Rot, wenn* ein `ChildPointsEntry` entsteht.

## Schätzung

**Größe: M**, wie der Batch-Pfad im `MediaSelector` (B-03) mit Migration: eine Spalte, ein Lauf-Aufruf, fünf Randfälle.

- **`wo: backend`**: kein Vertragsfeld in diesem Stück (die Anzeige liegt in B-746).
- **`migration: ja`**: nullable `INTEGER`-Spalte auf `Klassenarbeiten`, kein Rückfüllen (bestehende Arbeiten bleiben leer), eine
  Migration wird an die Kette gehängt.
- **`vertragsbruch: nein`**: nichts in `Pugling.Contracts`.

### Risiken

- **Der Klausurtag ist ein dünner Faden.** Der träge Lauf hängt am Zugriff des Kindes; liegt die Arbeit an einem Tag ohne App-Zugriff,
  fehlt der Schnappschuss, und die Eichung ([B-745](B-745-klausurnote-wirkt-auf-die-notenstufe.md)) hat keinen Vergleich. Das ist
  entschieden (Kosten oben), wird aber eine häufige Lücke sein. Eine mögliche Abmilderung wäre, am ersten Zugriff **danach** zu
  setzen, solange noch keine Note eingetragen ist; das widerspricht dem Wortlaut „am Klausurtag" und ist darum nicht eingeplant,
  sondern als Frage zu notieren.
- **Zeitzone:** Der Klausurtag ist das UTC-Datum des Servers (`DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)`); nahe
  Mitternacht deutscher Zeit kann der Lauf einen Tag früher oder später greifen (`CLAUDE.md`, Fallstrick „Zeit/UTC").
- **Neue Spalte, alte Zeilen:** G1 (Modell gleich Snapshot) und die Migration müssen die echte DB mit vorhandenen
  Klassenarbeiten tragen; die Spalte muss nullable bleiben.
- **Doppelter Lauf, zwei Anfragen:** Das Setzen muss bedingt sein (nur wenn `PredictedLevel` leer), sonst überschreibt der
  Verlierer eines Rennens den Gewinner.

### Angriffsplan

1. Spalte `PredictedLevel` an `Klassenarbeit`, Migration `KlassenarbeitPredictedLevel` an die Kette hängen.
2. Methode `SnapshotDueAsync(childId, today)` im Dienst der Stufen (B-741): setzt die Stufe des Fachs an jede Klassenarbeit des
   Kindes mit `ScheduledDate == today`, Fach gesetzt, `PredictedLevel` leer; bedingtes Update.
3. Aufruf an derselben Stelle wie der Stufen-Lauf (`AuthController`, Kind-Anmeldung, und der Lesepfad aus B-742).
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Testklasse `KlassenarbeitPrognoseTests` (Integrationstest über `PuglingWebAppFactory`; Klausurtag über `TestClock`, Aufbau
  der Klassenarbeit wie in `KlassenarbeitenTests`) für AK 1 bis 6.
- Neue Testklasse `KlassenarbeitPredictedLevelMigrationTests` nach dem Muster von `ShopStockPerChildMigrationTests`: bestehende
  Klassenarbeiten bleiben unversehrt, Spalte leer.
- `SchemaGuardTests` G1 und G1b laufen mit.
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test` mit einer Klassenarbeit für heute. Ein Rollengang folgt mit B-746.

## Verlauf

- **2026-11-20** — angelegt aus [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (geteilt); `geschaetzt`: M, backend, Migration
  ja, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
