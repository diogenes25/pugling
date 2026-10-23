---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/backend, rolle/creator]
aliases: [PublishersController Create Hand-Antwort, Nachbar von B-158]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 3, zweiter Lauf – Fund derselben Familie)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-244 · `PublishersController.Create` baut die Antwort von Hand, `Update` liest über `Project` zurück

Dasselbe Muster, das B-158 gerade im `SubjectsController` beseitigt hat – beim Nachbarn eine Tür weiter.
Gefunden in der Nachschau zu B-158 (Punkt 5 des Prüfauftrags: „trägt ein Nachbar dasselbe Muster?").

## User Story

Als **Entwickler** möchte ich, dass beide Schreibwege des `PublishersController` dieselbe Projektion
benutzen, damit ein künftiges Feld in `PublisherResponse` nicht in der POST-Antwort still fehlt.

## Ist-Stand am Code

- `backend/Pugling.Api/Controllers/Creator/PublishersController.cs:127-128`: `Create` antwortet mit
  `new PublisherResponse(publisher.Id, publisher.Name, publisher.Slug, 0, 0, publisher.CreatedAt)` – zwei
  Zähler als Literal `0`.
- `:171`: `Update` liest über `Project(…)` zurück.
- Zum Vergleich der reparierte Nachbar: `SubjectsController.cs:69-70` und `:99` (beide `Project`),
  `TextbookSeriesController.cs:184-185` und `:248`.

Die `0`-Zähler sind heute richtig (ein frisch angelegter Verlag hat keine Reihen). Das Muster ist trotzdem
das von B-158 Entscheidung 6: zwei Formen für eine Nutzlast.

## Die echte Lücke

Keine mit heutiger Wirkung – ein Feld, das morgen in `Project` dazukommt, fehlt im `201` still.

## Akzeptanzkriterien

1. `Create` antwortet über `Project(db.Publishers.AsNoTracking().Where(p => p.Id == …)).FirstAsync(ct)`.
2. `rg "new PublisherResponse\(" backend/Pugling.Api` trifft nur noch die Projektion.
3. Suite grün; der bestehende Create-Test liest die Antwort (Zahl im Verlauf).

## Offene Punkte

1. Nichts zu entscheiden – `Aufräumen`, Vorbild steht zwei Dateien weiter.

## Verlauf

- **2026-10-23** — angelegt aus der Nachschau zu Sprint 3 (zweiter Lauf) als Fund derselben Familie,
  direkt `ausformuliert` (Zeilen vom Prüfer genannt, von mir nachgelesen). Kein `entgangen_bei`: der
  Controller war nie Gegenstand einer abgenommenen Story.
