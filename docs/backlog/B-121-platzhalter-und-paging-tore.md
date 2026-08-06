---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/api, bereich/qualitaet]
aliases: [Platzhalter-Rot-Liste, Paging-Obergrenze-Tor]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-101 (Entscheidung 4) — abgespalten beim Bauen, weil eine dritte, im ursprünglichen Bericht
  (docs/api-design-bewertung.md) nicht erfasste Inkonsistenz auftauchte
grund: ""
ersetzt_durch: []
---

# B-121 · Platzhalter-Rot-Liste und Paging-Tor aus B-101

Zwei der drei in [B-101](B-101-fehlercodes-und-drei-waechter.md) vorgesehenen Wächter (Wächter 1 — kein
generischer `Conflict` — ist dort bereits gebaut und abgenommen): ein Tor, das pro Sammlungs-Segment höchstens
einen Platzhalternamen zulässt, und ein Tor gegen unpaginierte Array-`GET`s. Abgespalten, weil das Bauen des
ersten Tors eine **dritte** Inkonsistenz aufdeckte, die der ursprüngliche Bericht nicht kannte — beide Tore
verdienen jetzt eine eigene, sorgfältige Verifikation gegen die *tatsächliche* Route-Oberfläche, nicht gegen
Bericht-Prosa.

## Ist-Stand am Code

**Platzhalternamen je Sammlungs-Segment** (verifiziert per `grep` über `Controllers/**`, nicht nur aus dem
Bericht übernommen):

| Segment | Beobachtete Namen | Fundstellen |
| --- | --- | --- |
| `exercises` | `id`, `exerciseId` | `ExerciseCatalogController` (`{id}`), `ExercisePreviewController` (`{id}`) vs. `ExerciseGrantsController`/`ExerciseMediaController` (`{exerciseId}`) |
| `media` | `id`, `assetId`, `linkId` | `MediaAssetsController` (`{id}`) vs. `MediaVariantsController` (`{assetId}`); `ExerciseMediaController`s `media/{linkId:int}` ist ein **anderes** Entity (`MediaLink`) |
| `vocabulary` | `id`, `vocabularyId`, `exerciseId` | `VocabularyStoreController`/`VocabularyTagsController` (`{id}`) vs. `VocabularyMediaController` (`{vocabularyId}`); die Vokabel-**Übung** unter `textbook-series/…/units/…/vocabulary/{exerciseId}` ist ein **anderes** Entity |
| `tags` | `tagId`, `id` | `TagsController` (`{tagId}`) vs. `VocabularyTagsController`s verschachtelte `tags/{id:int}` |
| `units` | `unitId`, `seriesUnitId` | **Neu gefunden, nicht im Bericht:** `SeriesUnitsController` (`textbook-series/{seriesId}/units/{unitId:int}`) vs. `ExerciseRoutes.Base` (`Controllers/Creator/ExerciseControllers.cs:17`: `.../units/{seriesUnitId:int}`), von allen 13 Übungstyp-Controllern geerbt |

Fünf Segmente statt der im Bericht behaupteten vier. `media`s `linkId` und `vocabulary`s `exerciseId` sind
**kein** Fall derselben Fehlerklasse — sie adressieren ein anderes Entity über dasselbe literale
Pfadsegment, keine zwei Namen für dieselbe Sache. `units` dagegen ist echte Schuld wie `exercises`/`tags`:
zwei Namen für **dieselbe** `SeriesUnit`.

**Paging** (aus B-101 übernommen, noch nicht nachgezählt in dieser Story): 35 Array-liefernde `GET`s ohne
`take` im aktuellen `docs/openapi/v1.json` (Stand der B-101-Arbeitsrunde, 2026-08-04) — diese Zahl ist vor
dem Bau dieser Story **neu zu zählen**, nicht aus dem alten Bericht zu übernehmen (er ist über zwei Tage
alt und mehrere Stories haben seither Endpunkte verändert).

## Offene Punkte

1. **Rot-Liste-Tupel für `units` ergänzen** — der Fund dieser Story. Empfehlung: `(units, seriesUnitId,
   "debt: SeriesUnitsController nennt denselben Platzhalter unitId, ExerciseRoutes.Base seriesUnitId —
   nicht vereinheitlicht, siehe B-101")`.
2. **Muss die Rot-Liste zwischen „Schuld" und „anderes Entity, korrekt" unterscheiden, oder reicht ein
   Tupel-Format ohne diese Unterscheidung?** Die Auflage aus B-101 (Entscheidung 2 der Arbeitsrunde) verlangt
   nur `(Segment, Zweitname, Grund)` — der Grund-Text selbst trägt die Unterscheidung, keine zusätzliche
   Spalte. Empfehlung: so lassen, ein viertes Feld wäre Struktur ohne mechanischen Nutzen (kein Test liest
   es unterschiedlich aus).
3. **Paging-Form (a), (b) oder beide** — B-101s Arbeitsrunde empfiehlt „beide", aber das ist eine
   Produktentscheidung über die Form eines neuen Tors, keine reine Nachzähl-Frage. Bei `art: Aufräumen`
   bleibt das autonom entscheidbar (Freigabe 1) — Empfehlung: **beide**, wie in B-101 begründet, hier nur
   bestätigt statt neu verhandelt.
4. **Zahl neu zählen, nicht aus B-101 kopieren.** Vor dem Bau: `docs/openapi/v1.json` frisch generieren
   (`dotnet test --filter ContractDocumentTests` schreibt es), dann Array-`GET`s ohne `take` zählen.

## Verlauf

- **2026-08-06** — angelegt beim Bauen von B-101 (Entscheidung 4 dort): der `units`-Fund gehört hierher,
  nicht in eine Zeile im `## Verlauf` von B-101, weil er dieselbe Fehlerklasse eine Ebene weiter trägt und
  zwei eigene, sorgfältig zu bauende Tore braucht.
