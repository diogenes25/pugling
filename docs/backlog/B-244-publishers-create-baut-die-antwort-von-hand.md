---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/backend, rolle/creator]
aliases: [PublishersController Create Hand-Antwort, Nachbar von B-158]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
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
- `:170-172`: `Update` liest über `Project(…)` zurück.
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

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Aufräumen`).

1. **`Create` liest über `Project` zurück**, exakt wie `Update` (`:172`) und wie der reparierte Nachbar
   `SubjectsController.cs:69-70`: `Project(db.Publishers.AsNoTracking().Where(p => p.Id == publisher.Id),
   User.CreatorId()).FirstAsync(ct)`. Der Kommentar „both counts are zero by construction" fällt – er begründete
   die Hand-Kopie. **Kosten:** eine Abfrage mit zwei `Count`-Unterabfragen, die für einen frischen Verlag 0
   liefern; dieselbe Form, die `Update` ohnehin bezahlt.
2. **Die Zusicherung bekommt eine Mutante, keine Absichtserklärung:** `PublishersTests.Anlegen_IstIdempotent_…`
   liest heute nur `slug`. Neu: der `201`-Body wird Feld für Feld gegen den `GET`-Body desselben Verlags gelesen
   (`seriesCount`, `foreignSeriesCount`, `createdAt`, `name`, `slug`). Im Ausgangszustand ist das grün (0 = 0) –
   darum wird die Mutante **gefahren**: `Create` antwortet vorübergehend mit `ForeignSeriesCount = -1` → der Fall
   muss **1 rot von 1** zeigen, dann `git checkout`. Begründung: CLAUDE.md („eine Zusicherung, die den
   Ausgangszustand prüft, kann nie fehlschlagen") plus der Retro-Vorschlag des ersten Laufs (Mutante benennen
   **und** fahren). **Kosten:** ein Mutanten-Lauf (~1 min).
3. **AK 2 bleibt ein Grep:** `rg "new PublisherResponse\(" backend/Pugling.Api` → nur noch `Project` (`:35`).

## Schätzung

**Größe XS** (Anker B-158 Punkt 1: dieselbe Umstellung an einer Stelle). `wo: backend`, keine Migration,
kein Vertrag (die Antwortform ändert sich nicht, nur ihre Herkunft).

**Angriffsplan:** Test erweitern (POST-Body = GET-Body) → Mutante fahren (1 rot von 1) → `Create` auf `Project`
→ Suite → Grep → Reviewer → Commit.

**Risiko:** keins mit Verhalten; `CreatedAtAction` braucht weiter `publisher.Id` aus der getrackten Entität.

**Testweg:** Mutante 1 rot von 1, Suite grün, Grep 1 Treffer.

## Verlauf

- **2026-10-23** — **abgenommen** (zweiter Nachtlauf, Sprint 5). `pugling-reviewer`: `CreatedAtAction`-Route
  unverändert; `AsNoTracking` nach `SaveChangesAsync` auf demselben Context liest die committete Zeile (wie
  `Update :172` und `SubjectsController.cs:67-70`); der `GetRawText()`-Vergleich ist stabil (gleiche
  Record-Projektion, gleicher Serializer, `CreatedAt` in beiden Pfaden aus der DB); **kein Fund**; Nit
  übernommen (Mutanten-Kommentar nennt jetzt „1 failed / 4 passed in this class", Zeilenzeiger `:172`).
  **Rollengang Creator:** Verlag anlegen und sofort lesen – der Test tut genau das gegen den In-Process-Server
  (POST-Body = GET-Body). Commit: siehe Nachtrag.
- **2026-10-23** — **gebaut** (zweiter Nachtlauf, Sprint 5). **Mutante gefahren, nicht nur benannt:** Test
  `Anlegen_IstIdempotent_…` vergleicht jetzt den `201`-Body (`GetRawText`) mit dem `GET`-Body desselben
  Verlags; mit der Mutante (`Create` antwortet von Hand mit `ForeignSeriesCount = -1`) **1 rot von 5** im
  Filter `PublishersTests` (erwartet 1 rot von 1 am neuen Fall – genau dieser fiel), Mutante zurückgenommen,
  `Create` auf `Project(db.Publishers.AsNoTracking().Where(p => p.Id == publisher.Id), User.CreatorId())
  .FirstAsync(ct)` umgestellt. `rg "new PublisherResponse\("` → nur noch `:35` (Projektion). Suite
  **879/879**.

- **2026-10-23** — gegrillt und geschätzt im zweiten Nachtlauf (Sprint 5, Freigabe 1).

- **2026-10-23** — angelegt aus der Nachschau zu Sprint 3 (zweiter Lauf) als Fund derselben Familie,
  direkt `ausformuliert` (Zeilen vom Prüfer genannt, von mir nachgelesen). Kein `entgangen_bei`: der
  Controller war nie Gegenstand einer abgenommenen Story.
