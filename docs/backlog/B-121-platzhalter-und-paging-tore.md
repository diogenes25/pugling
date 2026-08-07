---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api, bereich/qualitaet]
aliases: [Platzhalter-Rot-Liste, Paging-Obergrenze-Tor]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-101 (Entscheidung 4) — abgespalten beim Bauen, weil eine dritte, im ursprünglichen Bericht
  (docs/api-design-bewertung.md) nicht erfasste Inkonsistenz auftauchte
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
wartet_auf: ""
---

# B-121 · Platzhalter-Rot-Liste und Paging-Tor aus B-101

Zwei der drei in [B-101](B-101-fehlercodes-und-drei-waechter.md) vorgesehenen Wächter (Wächter 1 — kein
generischer `Conflict` — ist dort bereits gebaut und abgenommen): ein Tor, das pro Sammlungs-Segment höchstens
einen Platzhalternamen zulässt, und ein Tor gegen unpaginierte Array-`GET`s. Abgespalten, weil das Bauen des
ersten Tors eine **dritte** Inkonsistenz aufdeckte, die der ursprüngliche Bericht nicht kannte — beide Tore
verdienen jetzt eine eigene, sorgfältige Verifikation gegen die *tatsächliche* Route-Oberfläche, nicht gegen
Bericht-Prosa.

## User Story

Als **Entwickler**, der eine neue Route anlegt, möchte ich, dass ein mechanisches Tor eine zweite,
inkonsistente Bezeichnung für dieselbe Sammlung sofort meldet und eine unbemerkt wachsende Zahl
unpaginierter Listen sichtbar macht — damit beides eine bewusste Entscheidung bleibt statt eines stillen
Nebeneffekts, den erst ein späterer Bericht wieder ausgräbt.

## Ist-Stand am Code

**Platzhalternamen je Sammlungs-Segment** (verifiziert per `grep` über `Controllers/**`, nicht nur aus dem
Bericht übernommen):

| Segment | Beobachtete Namen | Fundstellen |
| --- | --- | --- |
| `exercises` | `id`, `exerciseId` | `ExerciseCatalogController` (`{id}`), `ExercisePreviewController` (`{id}`) vs. `ExerciseGrantsController`/`ExerciseMediaController` (`{exerciseId}`) |
| `media` | `id`, `assetId`, `linkId` | `MediaAssetsController` (`{id}`) vs. `MediaVariantsController` (`{assetId}`); `ExerciseMediaController`s `media/{linkId:int}` ist ein **anderes** Entity (`MediaLink`) |
| `vocabulary` | `id`, `vocabularyId`, `exerciseId` | `VocabularyStoreController` (`{id}`) vs. `VocabularyMediaController` (`{vocabularyId}`); die Vokabel-**Übung** unter `textbook-series/…/units/…/vocabulary/{exerciseId}` ist ein **anderes** Entity |
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

## Entscheidungen

1. **Rot-Liste als Tupel `(Segment, Zweitname, Grund)`, keine zusätzliche Spalte für „Schuld" vs. „anderes
   Entity, korrekt".** Begründung: die Auflage aus B-101 verlangt nur den Grund-Text, der die Unterscheidung
   selbst trägt — ein viertes Feld wäre Struktur ohne mechanischen Nutzen, kein Test liest es unterschiedlich
   aus. Kosten: keine.
2. **Fünf Segmente statt der im Bericht behaupteten vier** — `units` (`{unitId}` vs. `{seriesUnitId}`) kommt
   dazu, verifiziert per erschöpfender `grep`-Suche über alle fünf Segmente, nicht nur geglaubt. Rot-Liste-
   Tupel: `(units, seriesUnitId, "debt: ExerciseRoutes.Base vs. unitId in SeriesUnitsController — dieselbe
   SeriesUnit")`. Kosten: keine — reine Dokumentation eines bestehenden Zustands.
3. **Platzhalter-Tor gebaut als reiner Test, kein Produktivcode geändert.** `ConventionGuardTests
   .Sammlungs_Segment_Traegt_Hoechstens_Einen_Platzhalternamen` — Selbsttest bestätigt: Entfernen des
   `units`-Tupels aus der Rot-Liste lässt das Tor sofort mit genau diesem Fund rot schlagen (siehe
   `## Verlauf`). Self-Protection: mindestens 150 Segment/Platzhalter-Paare gefunden (gemessen: deutlich
   mehr).
4. **Paging-Tor: nur Form (b) gebaut, Form (a) explizit NICHT — abweichend von der Empfehlung „beide" aus
   B-101s Arbeitsrunde.** Beim Nachzählen (Pflicht aus dem alten offenen Punkt 4, siehe unten) zeigte sich:
   Form (a) heißt nicht nur „ein Tor ziehen", sondern **sieben Endpunkte tatsächlich auf Pagination
   umstellen** (die 7 „zu reparierenden" aus dem Bericht). Das ändert das Antwortverhalten für jeden
   heutigen Aufrufer, der sich auf eine vollständige, unbegrenzte Liste verlässt — sobald ein Default-`take`
   greift, bekäme er stillschweigend nur einen Ausschnitt. Das ist eine Produktentscheidung (welcher
   Default, welche Kompatibilitätsgarantie für bestehende Aufrufer), keine reine Aufräumarbeit — Freigabe 1
   deckt `art: Aufräumen` und `Defekt`, nicht das hier verborgene `Wunsch`. Form (b) dagegen ändert kein
   Verhalten (reiner Zähl-Test) und ist gebaut. Kosten: die sieben Endpunkte bleiben unpaginiert; als eigene
   Story [B-122](B-122-top-level-listen-bekommen-paging.md) (`art: Wunsch`) gefasst statt hier autonom
   entschieden.
5. **Zahl frisch gemessen, nicht aus B-101 übernommen — und das hat sich ausgezahlt.** Der Bericht vom
   2026-08-04 nannte 35; die tatsächliche Zählung (per Reflexion über `ApiSurface`, nicht über das
   OpenAPI-Dokument) ergab **34** — zwei Tage und mehrere Stories liegen zwischen den beiden Zählungen.
   Gepinnt: **34**, exakt (keine Obergrenze, siehe README „Der Nenner ist die Falle").

## Akzeptanzkriterien

1. Ein Tor lehnt eine neue Route ab, die in einem bestehenden Sammlungs-Segment einen zweiten
   Platzhalternamen einführt, der nicht in der Rot-Liste steht. ✅ `Sammlungs_Segment_Traegt_Hoechstens_Einen_Platzhalternamen`.
2. Die Zahl der unpaginierten Array-`GET`s ist exakt gepinnt (34, gemessen 2026-08-06) — eine Änderung in
   beide Richtungen bricht das Tor. ✅ `Unpaginierte_Array_GETs_Sind_Gepinnt`.
3. Alles so grün wie vorher — kein Verhalten ändert sich (Abnahmeform für `art: Aufräumen`). ✅ (die sieben
   Endpunkte bleiben absichtlich unpaginiert, siehe Entscheidung 4).

## Schätzung

**Größe S** — zwei reflexionsbasierte Guard-Tests, kein Produktivcode. `wo: backend`. `migration: nein`.
`vertragsbruch: nein`.

### Testweg

Beide neuen Tests selbst sind der Testweg; beide wurden per Selbsttest verifiziert (Rot bei entferntem
`units`-Rot-Listen-Eintrag bzw. bei einer bewusst falschen Pin-Zahl während des Bauens, danach wieder grün).

## Verlauf

- **2026-08-06** — angelegt beim Bauen von B-101 (Entscheidung 4 dort): der `units`-Fund gehört hierher,
  nicht in eine Zeile im `## Verlauf` von B-101, weil er dieselbe Fehlerklasse eine Ebene weiter trägt und
  zwei eigene, sorgfältig zu bauende Tore braucht.
- **2026-08-06** — gegrillt, geschätzt und abgenommen: autonom, Nachtlauf-Freigabe 1 (`art: Aufräumen`).
  Platzhalter-Tor gebaut und mit einem temporären Entfernen des `units`-Tupels als rot verifiziert (Fund:
  „units: seriesUnitId, unitId (nicht in der Rot-Liste)"), danach wiederhergestellt und grün. Paging-Zahl
  frisch gemessen: **34** unpaginierte Array-GETs (nicht 35 wie im zwei Tage alten Bericht) — als exakte Pin
  gebaut. Form (a) (sieben Endpunkte tatsächlich paginieren) als eigene `Wunsch`-Story
  [B-122](B-122-top-level-listen-bekommen-paging.md) abgespalten, weil sie Bestandsverhalten für
  unbegrenzte Aufrufer ändern würde — eine Produktentscheidung, die Freigabe 1 nicht deckt. Volle Suite
  752/752 grün (750 vor dieser Story + 2 neue Wächter).
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob beide Tore (Platzhaltername, Paging-Pin 34) trotz
  B-101/B-63/B-100 danach weiterhin grün laufen — hält (`ConventionGuardTests.cs:186`
  `UnpaginatedArrayGetCount = 34`, Pin seit der Abnahme laut `git log -p` unverändert und weiterhin
  zutreffend). Kein Fund.
