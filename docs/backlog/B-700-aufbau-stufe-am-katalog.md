---
tags: [typ/story, status/geschaetzt, bereich/api, bereich/katalog, rolle/creator]
aliases: [Aufbau-Stufe, Vorstufe von, Aufbau-Pfad im Katalog]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-536
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-700 · Katalog: eine Übung kennt ihre Aufbau-Stufe und ihre Ziel-Übung

Teil 1 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md): das Fundament. Der Katalog bekommt den Begriff
„diese Übung ist eine Vorstufe jener Ziel-Übung". Ohne ihn kann weder der Plan eine Stufe spielen
([B-701](B-701-position-spielt-die-aufbau-stufe.md)) noch der Agent Vorstufen erzeugen
([B-703](B-703-ki-creator-erzeugt-vorstufen.md)).

## User Story

Als *Creator* (Vater oder Lehrer) möchte ich im Katalog festhalten, welche Übung eine leichtere Vorstufe welcher
Ziel-Übung ist und auf welcher Stufe sie steht, damit ein Lernweg zum Stoff Teil des Katalogs wird statt des
Wissens im Kopf des Vaters.

## Ist-Stand am Code

- `Exercise` kennt kein Feld für Schwierigkeit, Vorstufe oder „baut auf auf": vorhanden sind nur `DefaultStage`,
  `GradeMin/GradeMax`, `SchoolTypes` und `GrammarTopicLinks` (`backend/Pugling.Api/Models/LearnEntities.cs:77-182`).
- **Namenskollision:** `Stage` heißt im Code schon etwas anderes – die Spielform *innerhalb* einer Übung
  (`PlanPosition.Stage`, `IExerciseType.StageOptions`, `ClozeStage`, `TestStage`;
  `backend/Pugling.Api/Models/PlanPositionEntities.cs:32`). Die Aufbau-Stufe heißt darum im Code **`BuildLevel`**
  (Entscheidung 1), nie `Stage`.
- Eine Übung hat heute schon eine Leiter der Spielformen (`EaseLadder`, B-635/B-643: Cloze
  `backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:198`, Vokabel
  `backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:85`). Das ist die Hilfe *je Karte*; die Aufbau-Stufe ist
  die Stufung *zwischen Übungen* desselben Themas. Beide bleiben getrennt.
- Das Thema trägt schon eine Verknüpfung: `ExerciseGrammarTopic` (B-155,
  `backend/Pugling.Api/Models/CurriculumEntities.cs:140`). Sie gruppiert aber nur lose („gehört zum Thema") und kennt
  keine Reihenfolge – als Träger der Stufen taugt sie nicht (Entscheidung 2).
- Der Vertrag trägt Zusatzfelder als trailing optionale Parameter (`BookPages = null`,
  `backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:32-36` und `:55`). Ein PUT ist eine
  Vollrepräsentation: ein fehlendes Feld löscht den Wert.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.* Sie ergänzen die Entscheidungen
von B-536 (Katalog-Stufe, Vater wählt aus), auf die sich diese Story stützt.

1. **Geerbt aus B-536, Entscheidung 1:** die Stufe sitzt am Katalog (1 = erkennen, 2 = einsetzen mit Auswahl,
   3 = frei bilden). Ergänzt: Feldname **`BuildLevel`**, weil `Stage` belegt ist. Verworfene Alternative:
   `Difficulty` – das suggeriert einen absoluten Wert über Themen hinweg, gemeint ist die Position *im Weg* zu einer
   Ziel-Übung. Kosten: ein weiteres Wort im Wörterbuch (`docs/translate.md` nachziehen). Schließt aus: dass Übungen
   verschiedener Themen einander „vorstufen".
2. **Der Weg ist eine Selbstreferenz: `Exercise.BuildTargetExerciseId` (nullable) zeigt von der Vorstufe auf die
   Ziel-Übung; die Ziel-Übung selbst trägt keinen Eintrag und gilt als oberste Stufe (3).** Begründung: die Position
   bleibt an der Ziel-Übung hängen (`PlanPosition.ExerciseId`, rund 38 Zugriffe im Code bleiben unberührt), die
   Vorstufen sind reine Zusätze. Verworfene Alternative: eine eigene Tabelle `BuildPath` mit Mitgliedern – sauberer
   bei vielen Stufen, aber ein zweites Aggregat samt Rechten für drei Zahlen. Verworfene Alternative: über
   `GrammarTopic` gruppieren – hat keine Reihenfolge und mischt Übungen, die gar keine Vorstufen sind. Kosten: eine
   Vorstufe kann nicht selbst Ziel einer weiteren sein (keine Ketten über zwei Ebenen; die Validierung lehnt das ab).
   Schließt aus: einen Lernpfad über mehrere Ziele („A → B → C").
3. **Eine Vorstufe liegt in derselben Unit wie ihre Ziel-Übung, ihr `BuildLevel` ist 1 oder 2, ihr Typ darf
   abweichen.** Begründung: dieselbe Unit heißt dasselbe Fach und denselben Stoff; der Typ darf wechseln (Auswahl
   statt freies Tippen), das ist der Sinn der Stufen. Kosten: eine Vorstufe aus einer anderen Unit muss kopiert
   werden. Schließt aus: ein unit-übergreifendes Vorstufen-Archiv.
4. **Rechte folgen der Ziel-Übung nicht:** eine Vorstufe hat eigenen Autor und eigene `ExerciseGrant`s. Setzen darf
   die Verknüpfung, wer die **Vorstufe schreiben** darf und die Ziel-Übung **lesen** darf (Lesen ist global im
   RWX-Modell). Begründung: so bleibt „fremde Übung als Ziel nehmen" möglich, ohne fremde Übungen zu ändern. Kosten:
   ein Autor kann zu einer fremden Übung Vorstufen bauen, ohne dass deren Autor es merkt. Schließt aus: Vorstufen nur
   mit Zustimmung des Ziel-Autors.
5. **Additiv im Vertrag; Löschen der Ziel-Übung wird abgelehnt (`409`), solange Vorstufen hängen.** Begründung:
   Cascade würde fremde Übungen löschen, SetNull machte sie zu verwaisten Stufen. Kosten: eine Ablehnung mehr im
   Löschpfad. Schließt aus: das stille Verschwinden eines Lernwegs.

## Akzeptanzkriterien

1. Eine Übung mit `BuildLevel = 1` und `BuildTargetExerciseId` einer Ziel-Übung derselben Unit lässt sich anlegen und
   zurücklesen. Rot macht ihn: ein Feld, das im Vertrag fehlt (`unknown_field` beim Senden, fehlender Wert beim
   Lesen).
2. Eine Ziel-Übung in einer anderen Unit, eine Ziel-Übung, die selbst Vorstufe ist, ein `BuildLevel` außerhalb 1–2
   und ein Verweis auf sich selbst werden je mit `400` abgelehnt. Rot macht ihn: jede einzeln entfernte Prüfregel –
   darum je ein eigener Testfall.
3. Ein PUT einer Vorstufe ohne die beiden Felder löscht sie (Vollrepräsentation). Rot macht ihn: wer die Felder in
   PATCH-Semantik zieht.
4. Das Löschen einer Ziel-Übung mit hängender Vorstufe liefert `409` mit dem bestehenden Code `exercise_in_use`, ohne Vorstufe `204`. Rot macht ihn: Cascade oder
   SetNull am Fremdschlüssel.
5. Ein Creator ohne Schreibrecht an der Vorstufe bekommt `403 not_owner`, auch wenn er die Ziel-Übung lesen darf. Rot
   macht ihn: ein Rechtecheck auf der Ziel-Übung statt auf der Vorstufe.
6. Die Migration nimmt vorhandene Übungen mit beiden Feldern `null` mit: eine Übung, die vor der Migration angelegt
   wurde, ist danach unverändert lesbar. Rot macht ihn: eine Migration, die die Spalte nicht-nullbar anlegt.

## Schätzung

**Größe: M** — wie der vokabel-basierte Batch-Pfad im `MediaSelector`: zwei Felder, eine Validierung, ein
Löschschutz, kein neues Aggregat. Kleiner als eine DB-Umbau-Etappe (L), weil nur eine nullable Spalte samt
Fremdschlüssel entsteht.

- **`migration: ja`** — neue Spalten an `Exercises` (Schema); die Migration wird an die Kette gehängt (letztes Glied
  heute `20261119065732_ShopStockPerChild`) und nimmt die Bestandszeilen mit (`SchemaGuardTests` G1b).
- **`vertragsbruch: nein`** — nachgesehen: `ExercisePayload<T>` und `ExerciseResponse<T>` tragen schon trailing
  optionale Felder (`BookPages = null`); zwei weitere sind additiv. Konstruiert ein Test die Antwort positional,
  zieht er nach – das ist kein Bruch des Vertrags.
- **`wo: backend`** — Katalog-Editor-Oberfläche in [B-704](B-704-aufbau-weg-in-der-oberflaeche.md).

### Risiken

- **PUT löscht still:** ein alter Client ohne die Felder löscht die Stufe beim nächsten PUT (gleiche Klasse wie
  `BookPages`). Gegenmittel: AK 3 hält es als Absicht fest, das Frontend schreibt immer die Vollrepräsentation.
- **Schema-Wächter:** `SchemaGuardTests` verlangt ein ausdrückliches Löschverhalten am Fremdschlüssel; es muss
  `Restrict` sein, nicht der Standard.
- **Katalog-Suche:** Vorstufen tauchen als eigene Treffer auf. Ein Filter „ohne Vorstufen" ist Anzeige und gehört in
  B-704, nicht hierher.

### Angriffsplan

1. Entity, Konfiguration, Migration
   (`dotnet dotnet-ef migrations add AufbauStufe --project backend/Pugling.Api --output-dir Data/Migrations`).
2. Validierung an der Stelle, die auch die `GrammarTopicIds` prüft (gleiche Fehlerform).
3. `ExercisePayload<T>`/`ExerciseResponse<T>` ergänzen, Controller und Mapper nachziehen, `Pugling.Client` bauen.
4. Löschschutz im Katalog-Löschpfad.
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `AufbauStufeKatalogTests` in `backend/Pugling.Api.Tests` (Muster `GrammatikThemenTests`): AK 1–5, je
  Fall eigener Test.
- Neue Klasse `AufbauStufeMigrationTests` nach dem Muster `ShopStockPerChildMigrationTests`: AK 6 mit Bestandszeile
  vor der Migration.
- `ConventionGuardTests`, `SchemaGuardTests` und `EndpointCoverageGuard` müssen grün bleiben.
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test` gegen die Wegwerf-DB.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt und geschätzt im Vorbereitungslauf (Sonnet): M, backend, Migration ja,
  Vertrag additiv. Entscheidungen stellvertretend, unbestätigt. Hängt an keiner Malus- oder Rollenfrage.
