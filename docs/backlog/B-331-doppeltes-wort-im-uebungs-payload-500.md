---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Doppelte vocabularyId im PUT gibt 500]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review von B-323 in Lauf J (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
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

## Verlauf

- **2026-10-28** — angelegt aus dem `pugling-reviewer`-Fund 2 zu B-323 in Lauf J; aus Code gelesen, nicht gemessen.
  Außerhalb des Sprint-Diffs (Freigabe 3: ohne Zählung).
