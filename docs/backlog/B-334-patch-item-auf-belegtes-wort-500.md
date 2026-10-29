---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [PatchItem auf ein Wort, das ein anderes Item trägt, gibt 500]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review von B-331 in Lauf K (docs/pm-sitzung-2026-10-29-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-334 · Ein Item auf das Wort eines anderen Items umzuhängen endet in `500`

## Ist-Stand (Lauf K, am Code belegt)

`PatchItem` (`ExerciseControllers.cs`) setzt `item.VocabularyId` ohne Prüfung, ob ein anderes Item derselben Übung schon
auf diesem Wort steht; `SaveChangesAsync` scheitert dann am Unique-Index `(ExerciseId, VocabularyId)` → `500`. Halb
geschrieben wird nichts (nur ein Speichern). `AddItem` prüft denselben Fall vorab und antwortet
`409 duplicate_vocabulary_in_exercise`. **Gemessen** (rote Probe unten): `500`.

Älter als der Sprint (`PatchItem` lag nicht im Diff), darum `entgangen_bei` leer; gefunden vom `pugling-reviewer` beim
Review von [B-331](B-331-doppeltes-wort-im-uebungs-payload-500.md), derselbe Defekt an der Einzel-Route.

## User Story

Als Creator, der ein Wort seiner Übung austauscht, möchte ich bei einem Wort, das die Übung schon hat, dieselbe klare
Meldung bekommen wie beim Hinzufügen – nicht einen `500`.

## Entscheidungen (autonom, Freigabe 1 – `Defekt`)

1. **Dieselbe Regel und derselbe Code wie `AddItem`** (`409 duplicate_vocabulary_in_exercise`), hier kollidiert die
   Anfrage tatsächlich mit dem gespeicherten Zustand. Das Item selbst ist ausgenommen: sein eigenes Wort noch einmal zu
   nennen ist kein Duplikat. *Kosten:* eine Abfrage.

## Akzeptanzkriterien

1. `PATCH` eines Items auf das Wort eines anderen Items → `409 duplicate_vocabulary_in_exercise`, Items unverändert.
2. `PATCH` mit dem eigenen Wort (plus Hinweis) → `200`.

## Schätzung

`XS`, Backend; Integrationstest in `ExerciseItemsAndProgressTests`.

## Verlauf

- **2026-10-29** — angelegt aus dem `pugling-reviewer`-Fund 🟡 zu B-331 (außerhalb des Diffs: ohne Zählung) und im selben
  Sprint gebaut. Rote Probe **1 failed** (erwartet 1) an Z. 487 – `InternalServerError` statt `Conflict`. Mutanten: Prüfung
  entfernt **1 failed** (Z. 489); eigenes Item nicht ausgenommen **1 failed** (Z. 495, das eigene Wort gibt `409`).
- **2026-10-29** — Abnahme Lauf K, Teil 1: volle Suite **1015/1015**, Vitest **576/576**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet; danach nur ein Testkommentar berichtigt). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Creator/Vater über `schnellanlage-ohne-dublette` und `auch-richtig-im-uebungseditor`, Sohn als Regressionszeuge über seine Specs. Reviews: `pugling-reviewer`, `frontend-reviewer`, zwei Delta-Reviews (das letzte ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
