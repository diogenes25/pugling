---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Birkenbihl prüft VocabularyId nicht]
status: abgenommen
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
und `_self` zeigt ins Leere. Der Wort-`PUT` (`…/words/{wordId}`) desselben Felds (`:580-586`) antwortet dagegen `400 invalid_reference` – zwei Wege
zum selben Feld, zwei Antworten. Dieselbe Klasse wie [B-386](B-386-uebersetzung-speichert-unbekannte-vokabel-id.md) (Übersetzung).
Beim Ausformulieren zu prüfen: ob die Löschsperre im Vokabelspeicher Birkenbihl-Verweise kennt (B-386 ergänzt nur Übersetzungen),
und ob der CRUD-Weg wie der Wort-`PUT` (`…/words/{wordId}`) auch das Sprachpaar prüfen soll.

## User Story

Als **Creator** (oder KI-Agent) möchte ich, dass eine Birkenbihl-Übung eine Vokabel-Id, die es nicht gibt, beim Anlegen und Ersetzen
abweist wie der Wort-`PUT` (`…/words/{wordId}`) – und dass eine Vokabel, an der ein dekodiertes Wort hängt, nicht aus dem Speicher verschwindet.

## Ist-Stand am Code (Stand `f78ff5d`)

- `backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:474-509`: `BirkenbihlController` hat nur `NormalizeConfig` (Ids
  vergeben); kein `ValidateConfigAsync`. `POST`/`PUT` mit `decoding[].vocabularyId = 987654` → `201`/`200`.
- Der Wort-`PUT` (`…/words/{wordId}`) (`:590-596`) prüft Existenz **und** Sprachpaar → `invalid_reference`.
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
- **2026-11-05** — gebaut in Lauf W, Sprint 8. Die Existenzprüfung ist als `ExerciseControllerBase.VocabularyIdsProblemAsync` **ein**
  Helfer für Vokabelübung, Übersetzung und Birkenbihl (vorher zwei Kopien, jetzt keine dritte); die Verwendung kennt Birkenbihl über
  `LinkedVocabularyIds` (ein Block für beide Config-Typen mit Id-Verweis). AK 1 → `UnknownReferenceCodeTests.cs:171` (unbekannt),
  `:172` (`0`), `:174` (bestehende Id → `201`), `:177` (`PUT` unbekannt); AK 2 → `CatalogReadDeleteTests.cs:206` (Usage), `:209`
  (`409`), `:213` (fremde Id im Satztext bleibt löschbar). Rote Proben: Validator abgeschaltet → rot in `:171`; `0` ausgefiltert →
  rot in `:172`; Usage ohne Birkenbihl → rot in `:206`; Vorfilter entscheidet → rot in `:213`; je 1 von 18, alle kompiliert. Volle
  Suite 1089/1089.
- **2026-11-05** — Review `pugling-reviewer`: kein 🔴/🟡; die Extraktion ist verhaltensgleich (Meldungen, Reihenfolge, Ids). 🟢 Der
  Wort-Endpunkt ist ein `PUT`, kein `PATCH` – in Summary, Test-Summary und hier berichtigt (nur Text, **Zähler Sprint 8: 0 von 5**).
  🟢 **Bekanntes Verhalten:** eine Altdatenbank kann tote Ids an Birkenbihl-Wörtern tragen (das Löschen war bisher erlaubt); ein
  `PUT` solcher Übung meldet dann `invalid_reference` – dieselbe Parität wie B-386, das Frontend schickt Birkenbihl-Configs nicht
  zurück (B-72). 🟢 `"decoding": null` endet schon vorher in einer 500 → [B-391](B-391-birkenbihl-decoding-null-ist-500.md).
- **2026-11-05** — **abgenommen.** Volle Suite 1089/1089 (danach nur Kommentartext geändert); Rollengang Creator als E2E: volle Playwright-Suite 115 passed gegen ein Backend, das nach dem letzten Code-Edit startete. Birkenbihl- und Übersetzungs-Anlage im Browser (`uebungstypen.spec.ts`) und der Vokabelspeicher grün; Vater und Sohn unberührt (nur Creator-Schreibwege), ihre Specs in derselben Suite grün. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `cae15a1`.
