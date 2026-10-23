---
tags: [typ/story, status/ausformuliert, bereich/doku, rolle/creator]
aliases: [Creator.http chapters, REST-Tutorial 404]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: doku
migration: ""
vertragsbruch: ""
quelle: Nachtlauf 2026-10-23, Sprint 2 (Sweep zu B-166)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-238 · Das ausführbare REST-Tutorial des Creators fährt die Kapitel-Route, die es seit B-106 nicht gibt

## User Story

Als **Creator mit dem REST-Client** möchte ich `docs/REST/Creator.http` Schritt für Schritt ausführen können,
damit das Tutorial zeigt, wie Fach → Reihe → Unit → Übung heute entsteht – statt ab Schritt 4 nur 404 zu liefern.

## Ist-Stand am Code

`docs/REST/Creator.http` (Stand `6af2c62`, 2026-08-10, 271 Zeilen) baut ab `:60` auf der entfernten Ebene:

| Zeile | Request | Lebende Route |
| --- | --- | --- |
| `:62` | `POST …/creator/subjects/{{subjectId}}/chapters` | gibt es nicht – eine Übung hängt an `textbook-series/{seriesId}/units/{seriesUnitId}` |
| `:72` | `@chapterId = {{createChapter.response.body.id}}` | Variable ohne Quelle, sobald `:62` 404 liefert |
| `:85` | `POST …/subjects/{{subjectId}}/chapters/{{chapterId}}/vocabulary` | `POST …/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary` |
| `:109` | `GET …/chapters/{{chapterId}}/vocabulary/{{exerciseId}}/items` | `GET …/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items` |

Alles ab `:112` (Katalogfilter, Preview, Tags, Grants, Sharing) hängt an `@exerciseId` aus `:105` und läuft
damit ebenfalls ins Leere. `Supervisor.http` und `Student.http` tragen keine Kapitel-Route (gegrept: 0).
`docs/REST/README.md:15` beschreibt die Datei als „Fach → Kapitel → Vokabelübung → Items → Preview → Tags".

## Die echte Lücke

Nicht ein Wort, sondern **drei fehlende Schritte**: Das Tutorial braucht vor der Übung eine Reihe
(`POST creator/textbook-series`, mit `subjectId`) und eine Unit (`POST …/{seriesId}/units`), deren Ids es
dann weiterreicht – genau die Umschreib-Arbeit, die B-166 für sein Wiki-Beispiel als „nicht bloß Pfad
ersetzen" benannt hat. Die Datei ist ausführbar und damit die einzige Doku, die beim Lesen *nicht* auffällt
und beim Ausführen sofort.

## Akzeptanzkriterien

1. `docs/REST/Creator.http` enthält keine Route mit `chapters`; vor der Übung stehen die Requests für Reihe und
   Unit, und `@seriesId`/`@seriesUnitId` reisen in die Übungs-Requests weiter.
2. Die Datei läuft im REST-Client von oben bis unten ohne 404 durch (Datum des Durchlaufs in der Datei, wie
   `docs/tutorial-creator.md` es hält); ersatzweise ein `curl`-Durchlauf derselben Requests gegen die laufende
   API, im `## Verlauf` benannt.
3. `docs/REST/README.md:15` beschreibt den neuen Weg (Fach → Reihe → Unit → Vokabelübung → Items → Preview → Tags).

## Offene Punkte

1. Welche Felder verlangt `POST creator/textbook-series` minimal (Name, Slug?, `subjectId`, Sprachen)? Aus
   `TextbookSeriesDtos.cs` und `docs/tutorial-creator.md` abschreiben, nicht raten.
2. Soll das Tutorial die geseedete Reihe „Green Line 1" nutzen (kürzer, aber abhängig vom Seed) oder eine
   eigene anlegen (länger, aber idempotent gegen Wiederholung über den Slug)? *Empfehlung: eigene, mit
   Zeitstempel im Namen wie bisher beim Fach.*
3. Gibt es einen maschinellen Beleg? Die `.http`-Dateien laufen nur im REST-Client (httpYac inkompatibel,
   siehe Memory); ein Test dafür existiert nicht. *Empfehlung: einmal von Hand durchfahren und das Datum in
   die Datei schreiben – wie `docs/tutorial-creator.md` es mit „verifiziert am" tut.*

## Verlauf

- **2026-10-23** — angelegt im Nachtlauf (Sprint 2) beim Grep-Sweep zu B-166, direkt `ausformuliert`: die
  Fundstellen stehen mit Zeile, die lebenden Routen sind gegen `ExerciseControllers.cs` geprüft. **Bewusst
  nicht in B-166 mitgenommen** (README: ein Fund beim Bauen wird eine eigene Story): dort sind es
  Wortersetzungen, hier ein Umbau mit drei neuen Requests, und der Beleg ist ein anderer (Ausführen, nicht
  Grep). `entgangen_bei` bleibt leer – die Datei wurde nie als Story abgenommen.
