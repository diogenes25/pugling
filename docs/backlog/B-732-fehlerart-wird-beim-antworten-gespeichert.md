---
tags: [typ/story, status/geschaetzt, bereich/auswertung, bereich/training, rolle/student]
aliases: [Fehlerart, Akzentfehler, Tippfehler, Etappe E1a]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-732 · Die Fehlerart einer getippten Antwort wird beim Antworten bestimmt und gespeichert (B-644, Etappe E1a)

Dritte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Die Auswertung braucht das
„Warum" hinter einer falschen Antwort: Akzent, Tippfehler, falsches Wort, Ausweichen auf ein Synonym. Heute steht nur
„richtig/falsch" in der Datenbank; ob die Antwort ein Tippfehler war, rechnet die Anzeige und vergisst es wieder.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidung 13 stammt vom Vater, 18 ist stellvertretend und
unbestätigt.

## User Story

Als *Vater* möchte ich bei jedem falschen Wort sehen, ob mein Kind das Wort nicht kennt oder es nur falsch schreibt,
damit ich die Lernhilfe passend wähle. Als *Planer* (B-734) brauche ich die Fehlerart als gespeicherte Eingangsgröße,
die sich nicht ändert, wenn jemand später die Regeln anpasst.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Bewertung ist binär:** `AnswerGrader.Matches` (`Services/Shared/AnswerGrader.cs:26`), Vergleich über
  `StageMechanics.Normalize`.
- **„Falsch geschrieben" gibt es schon, nur flüchtig:** `AnswerGrader.IsMisspelled` (`:47`, B-524) misst Buchstabenabstand
  (Wörter bis fünf Buchstaben 1, darüber 2) und lässt ein Nachbarwort derselben Übung nicht als Tippfehler gelten. Es ist
  „Display only" und wird nicht gespeichert; Aufrufer: `PositionPracticeController.cs:648` und `:927`,
  `PositionTestsController.cs:447`, `ExercisePreviewService.cs:96`.
- **Speicherstellen ohne Fehlerart:** `ItemReviewEvent` (`Models/ItemProgressEntities.cs:61`) mit gegebener Antwort und
  richtig/falsch, geschrieben von `ItemProgressService.RecordAsync` (`Services/Shared/ItemProgressService.cs:37`; Aufrufer
  `PositionPracticeController.cs:731`, `:900`, `:907`); `TestItemResult` (`Models/StudyPlanEntities.cs:202`), angelegt in
  `PositionTestsController.cs:94` (freie Nennung) und `:190` (Fragenfolge).
- **Akzent:** Die Regel „Akzentfehler zählt im Üben halb" (B-624) ist **nicht gebaut**. Die Suche nach Akzent,
  Diakritik und Halbwertung in `Services/`, `Controllers/` und `Exercises/` findet nur den Apostroph-Kommentar in
  `StageMechanics.cs:55`. Die Entscheidung 18 verlangt dieselbe Vergleichsfunktion als Quelle; sie muss hier entstehen.
- **Getippt oder gewählt:** `StageMechanics.IsTyped(TestStage)` (`:17`) und `IsTyped(ClozeStage)` (`:27`), im Controller
  über `type.IsTypedStage`.
- **Kein Feld für die Stufe vor der Antwort:** `ItemReviewEvent` trägt die Stufe der Frage (`StageValue`), aber nicht das
  Leitner-Fach vor der Antwort; `RecordAsync` kennt es (`prog.Box`), bevor es fortgeschrieben wird.

## Entscheidungen (geerbt)

1. **Die Fehlerart wird beim Antworten bestimmt und mit der Antwort gespeichert, nur für getippte Antworten;
   Auswahl-Antworten tragen keine** (B-644 Entscheidung 18, stellvertretend, unbestätigt). Verworfen: nachträglich aus der
   gespeicherten Antwort bestimmen (instabil gegen Regeländerungen). Schließt aus: eine Fehlerart für Auswahl-Antworten.
2. **Vier Arten: Akzent, Tippfehler, falsches Wort, Ausweichen auf ein Synonym** (B-644 Entscheidung 13, Vater).
3. **Altdaten entfallen durch den Reset** (Entscheidung 12): bestehende Zeilen bleiben leer, es gibt kein Nachfüllen.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Zusätzlich speichert `ItemReviewEvent` das Leitner-Fach vor der Antwort (`BoxBefore`).** Begründung: Die
   Tagesauswertung ([B-733](B-733-tagesauswertung-und-leistungsprofil.md)) muss „gefestigt" und „zurückgefallen" je Tag
   zählen; aus dem Verlauf allein ist das Fach zu Tagesbeginn nicht rekonstruierbar. Dieselbe Migration, derselbe
   Schreibweg. Kosten: eine Spalte mehr. Verworfen: „gefestigt" nur als Momentaufnahme beim Berechnen (falsch, wenn
   nachträglich berechnet wird).
2. **Synonym heißt: die Antwort ist die richtige Lösung eines *anderen* Eintrags im Vokabelspeicher, der dieselbe
   Übersetzung trägt** (`Vocabulary.TranslationAlternatives`, `Models/VocabEntities.cs:39`, und gleiche Gegenseite). Ob
   der Speicher das hergibt, ist beim Bau nachzusehen; fällt es aus, bleibt die Art „falsches Wort" und die vierte Art
   wird eine eigene Folgestory (Theodors 11/20-Fehler „Ausweichen auf bekannte Synonyme" ist der Anlass).

## Akzeptanzkriterien

1. Lösung „café", Antwort „cafe" (nur ein Akzent fehlt) speichert die Fehlerart *Akzent* an `ItemReviewEvent` und an
   `TestItemResult`. *Rot, wenn* die Art fehlt, *Tippfehler* heißt oder erst beim Lesen entsteht.
2. Antwort mit Buchstabenabstand nach der Regel von `IsMisspelled` speichert *Tippfehler*; dieselbe Regel, kein zweiter
   Abstandsbegriff. *Rot, wenn* die Art von `IsMisspelled` abweicht (Test: beide Funktionen am selben Datensatz).
3. Eine andere Vokabel oder ein Nachbarwort der Übung speichert *falsches Wort*. *Rot, wenn* ein Nachbarwort als
   *Tippfehler* gespeichert wird (der Schutz aus `IsMisspelled`).
4. Die Antwort „Lösung eines anderen Eintrags mit gleicher Übersetzung" speichert *Synonym* (vorbehaltlich Annahme 2).
   *Rot, wenn* sie als *falsches Wort* durchgeht.
5. Eine Auswahl-Antwort und jede richtige Antwort tragen **keine** Fehlerart. *Rot, wenn* das Feld dort gefüllt ist.
6. Die Fehlerart steht mit der Antwort fest: Ändert jemand danach die Lösung der Übung, bleibt der gespeicherte Wert.
   *Rot, wenn* ein Lesepfad die Art neu berechnet.
7. Die Bewertung bleibt binär: `WasCorrect`, Punkte und Leitner-Schritt ändern sich nicht (`UebenFalschGeschriebenTests`,
   `AnswerGraderTests` bleiben unverändert grün). *Rot, wenn* eine Antwort wegen der Fehlerart als richtig oder halb zählt
   (B-624 ist ausdrücklich **nicht** Teil dieser Story).
8. `ItemReviewEvent` trägt `BoxBefore` für jede Antwort, die auf die Beherrschung zählt. *Rot, wenn* der Wert leer ist,
   obwohl ein `ItemProgress` fortgeschrieben wurde.

## Schätzung

**Größe: M** — Anker: wie der Batch-Pfad im `MediaSelector`: eine geteilte Stelle (`AnswerGrader`/`ItemProgressService`) mit
mehreren Aufrufern, eine Migration mit drei kleinen Spalten, kein Frontend.

- **`migration: ja`** — `ItemReviewEvent.ErrorKind` und `BoxBefore`, `TestItemResult.ErrorKind`, alle nullable;
  Bestandszeilen bleiben leer (Entscheidung 3). An die Kette hängen.
- **`vertragsbruch: nein`** — die Fehlerart ist intern; sie erscheint erst mit [B-733](B-733-tagesauswertung-und-leistungsprofil.md)
  in neuen DTOs. Kein bestehender Vertragstyp ändert sich.
- **`wo: backend`** — keine Oberfläche.

### Risiken

- **Synonym** ist die unsicherste Art (Annahme 2); sie darf als Folgestory fallen, ohne die drei anderen zu blockieren.
- **Tippfehler-Schwelle:** Akzent muss *vor* Tippfehler geprüft werden (ein fehlender Akzent ist Abstand 1); die Reihenfolge der
  vier Prüfungen ist die Regel und gehört in einen Test.
- **Drei Schreibwege** (`RecordAsync`-Aufrufer, `:94`, `:190`): vergisst einer die Art, bleibt die Spalte dort still leer.
  Darum je Weg ein Fall im Testweg.
- **Ein Tor über ein Artefakt beweist nur, dass es sich nicht ändert:** die Prüfung des Schreibwegs gehört in
  Integrationstests, nicht in einen Schema-Scan.

### Angriffsplan

1. `StageMechanics`: Akzent-faltender Vergleich als eine Funktion.
2. `AnswerGrader.Classify(given, item, siblings)` mit fester Prüfreihenfolge (Akzent, Tippfehler, Synonym, falsches Wort);
   `IsMisspelled` bleibt, wird aber von `Classify` mitbenutzt.
3. Migration und Modell; `RecordAsync` und die beiden Test-Schreibwege geben die Art mit.
4. Integrationstests je Schreibweg.

### Testweg

- `AnswerGraderTests`: Theorie über Lösung/Antwort/Erwartung (Akzent, Tippfehler, Nachbarwort, Synonym, leer, richtig,
  Auswahl). Rote Probe je Fall: Reihenfolge vertauschen lässt Fall „cafe" fallen.
- Neue Klasse `FehlerartTests` (Integration): Üben, Test (Fragenfolge), freie Nennung; Lösung nachträglich ändern (AK 6);
  Direktlesen der Zeile über den `DbContext`.
- Neue Migrationsklasse `FehlerartMigrationTests` nach `ActivatedAtMigrationTests`.
- Regression: `UebenFalschGeschriebenTests`, `PositionPracticeFlowTests`, `PositionTestFlowTests`, `ApostrophTests`.
- Vor der Abnahme: `pugling-reviewer`; ein Rollengang entfällt, solange die Art keine Oberfläche hat (im Verlauf benennen).

## Reihenfolge und Abhängigkeiten

Unabhängig von [B-730](B-730-tagesbegriff-in-deutscher-zeit.md) und [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md),
kann parallel laufen. Vor [B-733](B-733-tagesauswertung-und-leistungsprofil.md).

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E1a) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: B-624 ist nicht gebaut (keine Akzentregel), `IsMisspelled` ist nur
  Anzeige; `BoxBefore` als nötiger Zusatz für B-733 erkannt. M, backend, Migration, kein Vertragsbruch.
