---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, bereich/gamification, rolle/supervisor]
aliases: [Prognose-Schnappschuss, PredictedLevel]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-683
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-25"
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
- **2026-11-24** — Nachprüfung vor dem Bau (Nachtlauf L, Sprint 161, [Protokoll](../pm-sitzung-2026-11-24-l.md)): Ist-Stand
  bestätigt (keine Prognose an `Klassenarbeit`, Stufe aus B-741 in `ChildSubjectLevel`, Lauf in `SubjectLevelService`).
  Überholt ist die Risikozeile „Zeitzone“: der Klausurtag ist seit B-730 der Lerntag (`LearningDay.Today`, deutsche
  Zeit), nicht das UTC-Datum. `in-arbeit`.
- **2026-11-24** — gebaut: Spalte `Klassenarbeit.PredictedLevel` (nullable), Migration
  `20261124201304_KlassenarbeitPredictedLevel` (nur `AddColumn`, kein Rückfüllen, Snapshot-Diff genau die Spalte).
  `SubjectLevelService.SnapshotTestDayAsync` am Ende **jeder** Bewertung (die Senke aller drei Wege: Kind-Anmeldung,
  `student/me/subject-levels`, `supervisor/children/{id}/subject-levels`): erst eine Leseprobe, dann ein bedingtes
  `ExecuteUpdate` (`ScheduledDate == Lerntag`, Fach gesetzt **und** heute gemessen, `PredictedLevel IS NULL`, Wert = die
  frisch bewertete `Level`-Zeile). Kein Ledger. Abweichend vom Angriffsplan keine eigene öffentliche Methode
  `SnapshotDueAsync`: der Aufruf sitzt in der einen Bewertung, so kann kein Weg ihn vergessen.
- **2026-11-24** — AK → Test (`KlassenarbeitPrognoseTests`): AK 1 → `Klausurtag_schreibt_die_Stufe_einmal` (M1) · AK 2 davor/
  danach → `Nur_am_Klausurtag_nicht_davor_und_nicht_danach` (M3/M2) · AK 3 → `Ohne_Fach_kein_Schnappschuss`, **als
  Absichtserklärung beschriftet** (ein NULL-Fach gleicht in SQL keiner Stufenzeile; keine einzeilige Änderung macht ihn
  rot) · AK 4 ohne Maß → `Ohne_Mass_bleibt_der_Schnappschuss_leer` (M4) · AK 4 später am Tag / am Folgetag / Arbeit nach
  der Tagesbewertung angelegt → `Spaeter_am_Klausurtag_entstandene_Stufe_fuellt_noch_am_Folgetag_nicht` (M5, M2) · AK 5 →
  `Verschobener_Termin_laesst_den_Schnappschuss_stehen` (M7) · AK 6 → `Schnappschuss_bucht_nichts` (M8) · Migration →
  `PredictedLevelMigrationTests`. Rote Probe (M0, Aufruf entfernt): erwartet 4 rot (`Klausurtag…`, `Spaeter…`,
  `Verschobener…`, `bucht_nichts`), gemessen 4 – alle vier Namen in der Ausgabe; die drei Null-Fälle sind vor dem Fix
  grün und tragen je ihren eigenen Mutanten.
- **2026-11-24** — Review `pugling-reviewer` (Agent, nur gelesen): kein Blocker. 🟡 **Fachwechsel nach dem Schnappschuss
  ließ die Stufe des alten Fachs stehen** – widerspricht Entscheidung 2. Behoben (gezählt, Sprint 161: 1):
  `KlassenarbeitenController.Update` verwirft `PredictedLevel` bei einem echten Fachwechsel; am Klausurtag nimmt der nächste
  Zugriff ihn im neuen Fach neu, danach bleibt „keine Prognose“. Test `Fachwechsel_verwirft_die_Prognose_des_alten_Fachs`
  (M9 behält → rot, M10 verwirft auch beim gleichen Fach → rot). 🟢 ohne Zählung behoben: Leseprobe vor dem UPDATE (sonst
  nimmt jeder Lesezugriff die SQLite-Schreibsperre; M6), AK-4-Test legt ein zweites gemessenes Fach an, damit der frühe
  Rücksprung „Kind ohne Maß“ den Fall nicht verdeckt.
- **2026-11-24** — eigener Fund bei der Mutanten-Probe (gezählt nach Freigabe 11, Sprint 161: 4): nach dem Einbau der
  Leseprobe standen die Bedingungen doppelt – einmal in der Probe, einmal im `UPDATE`. M1–M4 am `UPDATE` blieben grün
  (gemessen 4 von 4 „green (abweichend)“, erwartet rot), weil die Probe dieselbe Zeile schon aussiebte. Behoben: ein
  Filter `due`, den Probe und `ExecuteUpdate` teilen, je Bedingung eine Zeile; die Probe danach neu gemessen.
- **2026-11-24** — Mutanten-Probe nach der letzten Änderung (`.claude/scripts/mutant-probe.py`, Debug, Filter
  `KlassenarbeitPrognoseTests|KlassenarbeitCountdownTests`; zwei Läufe, 2026-11-24 23:15 und 2026-11-24 23:21, beide an `5993c86b + Arbeitsbaum geändert`, Grundlinie
  danach jeweils grün). Gilt zugleich für B-746 (M11–M13):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M0 kein Schnappschuss | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:85 | red | red |  |
  | M1 ueberschreibt | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:102 | red | red |  |
  | M2 auch danach | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:101 | red | red |  |
  | M3 auch davor | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:101 | red | red |  |
  | M4 gespeicherte Stufe ohne Mass | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:103 | red | red |  |
  | M5 nur bei geaenderter Zeile | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:85 | red | red |  |
  | M6 Vorpruefung sperrt immer | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:107 | red | red |  |
  | M7 Verschieben loescht | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:189 | red | red |  |
  | M8 Buchung | backend/Pugling.Api/Services/Student/SubjectLevelService.cs:85 | red | red |  |
  | M9 Fachwechsel behaelt | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:186 | red | red |  |
  | M10 gleiches Fach verwirft | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:186 | red | red |  |
  | M11 Mapper ohne Wert | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:40 | red | red |  |
  | M12 Mapper Ersatzwert | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:40 | red | red |  |
  | M13 Kind sieht Prognose | backend/Pugling.Contracts/Student/ClassTestCountdownDtos.cs:11 | red | red |  |

- **2026-11-24** — Verifikation nach der letzten Änderung: .NET 2353/2353 (Release, Ausgangsstand 2343), Vitest 130 Dateien /
  1445 Tests, `tsc -b` sauber, `dotnet format --verify-no-changes` sauber. Rollengang: Kopie der echten DB (:5232, Migration dort eingespielt) – Friedrichs Arbeit vom 24.11. trägt nach dem Lesen der Stufen durch den Vater die Prognose 6, Theodors Arbeit vom 23.11. bleibt leer; E2E `klassenarbeit-abweichung.spec.ts` grün. `abgenommen`, Code in Commit
  `2886acf6`. Die `## Entscheidungen` sind weiter „stellvertretend, unbestätigt“ (Vorbereitungslauf 2026-11-20); die
  Bestätigung durch den Nutzer steht aus.
