---
tags: [typ/story, status/geschaetzt, bereich/backend, rolle/creator]
aliases: [Birkenbihl prüft VocabularyId nicht]
status: geschaetzt
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-387 · Eine Birkenbihl-Übung speichert eine unbekannte `vocabularyId` an einem dekodierten Wort ohne Prüfung

Der `BirkenbihlController` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs`, ab `:462`) überschreibt weder
`ValidateConfigAsync` noch `NormalizeConfigAsync`: `POST …/birkenbihl` mit `sentences[0].decoding[0].vocabularyId = 987654` → `201`,
und `_self` zeigt ins Leere. Der Wort-`PATCH` desselben Felds (`:580-586`) antwortet dagegen `400 invalid_reference` – zwei Wege
zum selben Feld, zwei Antworten. Dieselbe Klasse wie [B-386](B-386-uebersetzung-speichert-unbekannte-vokabel-id.md) (Übersetzung).
Beim Ausformulieren zu prüfen: ob die Löschsperre im Vokabelspeicher Birkenbihl-Verweise kennt (B-386 ergänzt nur Übersetzungen),
und ob der CRUD-Weg wie der Wort-`PATCH` auch das Sprachpaar prüfen soll.

## User Story

Als **Creator** (oder KI-Agent) möchte ich, dass eine Birkenbihl-Übung eine Vokabel-Id, die es nicht gibt, beim Anlegen und Ersetzen
abweist wie der Wort-`PATCH` – und dass eine Vokabel, an der ein dekodiertes Wort hängt, nicht aus dem Speicher verschwindet.

## Ist-Stand am Code (Stand `f78ff5d`)

- `backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:474-509`: `BirkenbihlController` hat nur `NormalizeConfig` (Ids
  vergeben); kein `ValidateConfigAsync`. `POST`/`PUT` mit `decoding[].vocabularyId = 987654` → `201`/`200`.
- Der Wort-`PATCH` (`:590-596`) prüft Existenz **und** Sprachpaar → `invalid_reference`.
- Die Dekodier-Endpunkte schreiben über `SetConfig` und vergeben die Ids selbst aus Treffern – sie umgehen die Prüfung zu Recht.
- `VocabularyStoreController.ReferencingExercisesAsync` (`:411-457`) kennt seit B-386 Übersetzungen, Birkenbihl nicht: ein
  automatisch verlinktes Wort verliert seine Vokabel beim Löschen still.
- Das Frontend schickt beim **Anlegen** keine `vocabularyId` mit (`frontend/src/vater/exerciseConfig.tsx:298-307`), und Birkenbihl
  ist im Dialog bewusst nicht inhaltlich bearbeitbar (B-72). Betroffen sind also API-Clients, nicht die Oberfläche.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** `POST`/`PUT` prüfen `decoding[].vocabularyId` wie Übersetzung und Vokabelübung: `≤ 0` →
   `validation_error`, unbekannt → `invalid_reference`.
2. **(Defekt, im Lauf gegrillt)** Birkenbihl zählt als Verwendung (`usage`, Löschsperre `409`), mit derselben Begründung wie B-386:
   ein Client, der liest und ersetzt, stünde nach einem erlaubten Löschen vor einem `400`. Gebaut als **ein** Block für beide
   Config-Typen mit Id-Verweis, nicht als dritte Kopie.
3. **Nicht** Teil: das Sprachpaar im CRUD-Weg zu prüfen wie der `PATCH`. Ein Sprachwechsel der Übung würde sonst jedes verlinkte
   Wort zum Fehler machen – das ist eine Produktfrage, für die es keinen Fund gibt.

## Akzeptanzkriterien

1. `POST …/birkenbihl` mit unbekannter `vocabularyId` → `400 invalid_reference`; mit `0` → `400 validation_error`; `PUT` mit
   unbekannter → `400 invalid_reference`; eine bestehende Id speichert.
2. `usage` listet eine Birkenbihl-Übung, die die Vokabel verlinkt; `DELETE` → `409 vocabulary_in_use`; eine fremde Id im Satztext
   sperrt nicht.

## Schätzung

`S`, `wo: backend`, keine Migration, kein Vertragsbruch. Testweg: Fälle in `UnknownReferenceCodeTests` und `CatalogReadDeleteTests`
nach dem Muster von B-386.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`,
  `git show f4cf656`, 🟡). Außerhalb des Diffs von B-329 – der Weg antwortete nie `validation_error`, sondern nahm die Id still an –,
  darum ohne `entgangen_bei`, wie B-386.
- **2026-11-05** — ausformuliert, gegrillt, geschätzt in Lauf V (`art: Defekt`). Am Code geprüft: der Bearbeiten-Dialog schickt
  Birkenbihl-Ids nicht zurück, weil Birkenbihl dort bewusst nicht inhaltlich bearbeitbar ist (B-72) – kein weiterer Defekt.
