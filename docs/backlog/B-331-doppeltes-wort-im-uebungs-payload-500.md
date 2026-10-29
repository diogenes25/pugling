---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Doppelte vocabularyId im PUT gibt 500]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review von B-323 in Lauf J (docs/pm-sitzung-2026-10-28-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-331 · Ein Wort, das im Übungs-Payload zweimal steht, endet in `500` – nach dem Speichern des Kopfes

`VocabularyController.ValidateConfigAsync` (`ExerciseControllers.cs`) prüft `refs`/`items` nicht auf Doppelte.
`refs=[a,b,a]` (oder dasselbe Inline-Wort zweimal) läuft durch; `ExerciseControllerBase.Update` speichert den Kopf,
dann legt `ExerciseItemService.ReconcileAsync` für das zweite `a` eine neue Zeile an (die erste hat den Pool schon
verlassen) und verletzt den Unique-Index `(ExerciseId, VocabularyId)` (`PuglingDbContext.cs:610`) → `DbUpdateException`
→ `500`, Übung halb geschrieben. Beim `POST` dasselbe. Die Einzel-Route meldet denselben Fall sauber als
`409 duplicate_vocabulary_in_exercise` (`AddItem`).

Unabhängig von der Plan-Sperre und älter als [B-323](B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md) (der
Präfix-Check lässt den Doppelten durch, weil er im angehängten Ende steht). Darum `entgangen_bei` leer.

Derselben Klasse („halb geschrieben, dann `500`“) gehört ein Inline-Item mit `vocabularyId: 0` oder negativ an: die
Existenzprüfung filtert `is > 0` (`ExerciseControllers.cs:54-60`), `ReconcileAsync` scheitert dann am Fremdschlüssel
(Nachschau Lauf J, aus Code gelesen).

Zu klären beim Ausformulieren: Code (`duplicate_vocabulary_in_exercise` wie an der Einzel-Route, oder
`validation_error`), Prüfung auch für Inline-Wörter per Schlüssel; erst messen (rote Probe), denn aus Code gelesen.

## User Story

Als **Creator**, der eine Vokabelübung über die API anlegt oder ersetzt (auch als KI-Creator), möchte ich, dass ein
Wort, das ich versehentlich zweimal schicke, mit einer klaren Meldung abgewiesen wird, damit meine Übung nicht halb
geschrieben mit einem `500` liegen bleibt.

## Ist-Stand (Lauf K, am Code belegt)

- `VocabularyController.ValidateConfigAsync` (`ExerciseControllers.cs:38-67`, Stand vor dem Fix) prüft Existenz und
  Pflichtfelder, keine Doppelten. Die Existenzprüfung der Inline-Items filtert `is > 0` (`:59`), eine
  `vocabularyId: 0` fällt also durch.
- `ExerciseControllerBase.Update` validiert (`:435`), speichert den Kopf (`:457`) und ruft danach `AfterSaveAsync`
  (`:458`) → `ExerciseItemService.ReconcileAsync`: das zweite Vorkommen findet im Pool keine freie Zeile mehr und legt
  eine zweite an → Unique-Index `(ExerciseId, VocabularyId)`. `POST` genauso (`:380` speichern, `:400` Reconcile).
- Zwei gleiche neue Inline-Wörter werden von `VocabularyStoreService.GetOrCreateAsync` über `Local` auf **denselben**
  Eintrag gelegt – derselbe Unique-Index.
- **Gemessen** (rote Probe unten): `PUT` mit `refs=[a,b,a]` → `500`; `POST` mit zweimal demselben Inline-Wort → `500`.

## Die echte Lücke

Was die Datenbank verbietet, prüft die Validierung nicht – und die Datenbank meldet es erst nach dem ersten Schreiben.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`400 validation_error`, nicht `409 duplicate_vocabulary_in_exercise`.** Die Anfrage widerspricht sich selbst; mit
   dem gespeicherten Zustand kollidiert nichts (das meint der `409` der Einzel-Route). Und `ValidateConfigAsync` kann
   heute nur einen Text zurückgeben – einen Code daneben zu führen ist genau der Umbau, den
   [B-329](B-329-uebung-unbekannte-referenz-validation-error.md) erst freigegeben haben will. *Kosten:* ein Client kann
   „doppelt im Payload“ nicht am Code von anderen Validierungsfehlern unterscheiden, nur am Text.
2. **Vergleich wie der Reconcile:** `refs` vor `items`; ein Inline-Wort, das schon im Store steht, zählt mit seiner Id
   (dieselbe Auflösung wie `PeekDesiredIdsAsync`), ein neues mit seinem Schlüssel (`VocabKey.Generate`). Damit fällt
   auch „Id und dasselbe Wort inline“. *Kosten:* eine Store-Abfrage je Inline-Wort in der Validierung.
3. **Inline-`vocabularyId <= 0` → `400`** mit eigenem Satz (dieselbe Klasse „halb geschrieben, dann `500`“).
   *Kosten:* keine – der Wert kann nie gültig gewesen sein.

## Akzeptanzkriterien

1. `PUT` mit einem doppelten Wort in jeder der vier Formen (`refs` doppelt, Item-Id doppelt, dasselbe neue Inline-Wort
   doppelt, Id plus dasselbe Wort inline) → `400 validation_error`; Items **und** Titel unverändert.
2. `PUT` mit einem Inline-Item `vocabularyId: 0` → `400`, nichts geschrieben.
3. `POST` mit doppeltem Inline-Wort → `400`, keine Übung angelegt.

## Schätzung

`S`, Backend, keine Migration, kein Vertragsbruch (`500` → `400`). Angriffsplan: rote Probe → Prüfung in
`ValidateConfigAsync` → Mutanten je Form. Testweg: Integrationstests in `ExerciseItemsAndProgressTests`.

## Verlauf

- **2026-10-28** — angelegt aus dem `pugling-reviewer`-Fund 2 zu B-323 in Lauf J; aus Code gelesen, nicht gemessen.
  Außerhalb des Sprint-Diffs (Freigabe 3: ohne Zählung).
- **2026-10-29** — Lauf K, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: zwei neue
  Fälle **2 failed** (erwartet 2) – `PUT` meldet „refs twice: 500“, `POST` `InternalServerError` statt `BadRequest`.
  Danach `ExerciseItemsAndProgressTests` **12/12**. Mutanten je Form, jede fällt an ihrer eigenen Form: „ohne `<= 0`“ →
  „inline vocabularyId 0: 500“; „ohne Schlüssel-Zweig“ → „inline word twice: 500“ und der `POST`-Fall; „`refs`
  übersprungen“ → „refs twice: 500“; „Inline-Wort nicht über den Store aufgelöst“ → „id and the same word inline: 500“;
  „Id-Vergleich nur für `refs`“ → „item id twice: 500“ (je **1 failed**, der `POST`-Fall nur bei der Schlüssel-Mutante).
- **2026-10-29** — Review `pugling-reviewer`: 🟡 `PatchItem` hängt ein Item ungeprüft auf ein belegtes Wort um → `500`
  (älter, außerhalb des Diffs, ohne Zählung) → [B-334](B-334-patch-item-auf-belegtes-wort-500.md), im Sprint behoben;
  🟢 der Fall „inline word twice" nahm `moon`, das ein anderer Test der Klasse in den Store legt – dann lief er je nach
  Reihenfolge durch den Id-Zweig statt den Schlüssel-Zweig → frisches Wort (ohne Zählung); 🟢 eine Store-Abfrage je
  Inline-Wort (`PeekDesiredIdsAsync`) belassen; 🟢 KI-Creator-Dubletten-Regel → [B-336](B-336-ki-creator-dublettenregel-schwaecher-als-store-schluessel.md).
- **2026-10-29** — Abnahme Lauf K, Teil 1: volle Suite **1015/1015**, Vitest **576/576**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet; danach nur ein Testkommentar berichtigt). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Creator/Vater über `schnellanlage-ohne-dublette` und `auch-richtig-im-uebungseditor`, Sohn als Regressionszeuge über seine Specs. Reviews: `pugling-reviewer`, `frontend-reviewer`, zwei Delta-Reviews (das letzte ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
