---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator, rolle/supervisor]
aliases: [Synonym gegen fremdes Label, EnsureAsync prüft den Slug des Labels]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-341 Entscheidung 2, Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-351 · Ein Synonym darf nicht das Label oder den Slug eines anderen Schlagworts tragen

`3190f9e` lehnt ein **Label** ab, das ein anderes Schlagwort als Synonym führt. Die Gegenrichtung fehlt: ein **Synonym**,
das dem Label oder Slug eines anderen Schlagworts gleicht, geht durch. Danach nennt derselbe Text zwei Schlagwörter, und
welches `EnsureAsync` liefert, hängt an der Suchreihenfolge (Slug → Label → Synonym). Außerdem prüft `EnsureAsync`
Synonyme nur gegen den Slug des **Textes**, nicht des Labels: über Kind-Interessen mit `{slug, label}` entsteht so ein
zweites Schlagwort mit einem Label, das `Create` mit `409` ablehnt.

## User Story

Als **Creator** oder **Vater**, der das Schlagwort-Vokabular pflegt, möchte ich, dass ein Text genau ein Schlagwort meint
– egal ob er als Label, Slug oder Synonym eingetragen wurde und über welchen Weg (Verwaltung oder Kind-Interessen).

## Ist-Stand (Lauf L, am Code belegt)

- `InterestTagsController.Create`/`Update` (`LabelConflictAsync`) prüfen ein neues Label gegen fremde Slugs, Labels und
  Synonyme. Die Synonyme selbst (`Clean(dto.Synonyms)`) werden nicht geprüft.
- `SynonymTakenAsync` begründet die Regel mit „`EnsureAsync` fände einen zweiten Tag nie" – falsch: `EnsureAsync` sucht
  Slug, dann Label, dann Synonym (`InterestTagService.cs`), findet also beide, nur in einer Reihenfolge.
- `EnsureAsync` vergleicht Synonyme mit `InterestSlug.From(text)`; bei `{slug: "foo", label: "Soccer X"}` ist `text` der
  Slug, das Label wird nicht gegen Synonyme geprüft.

## Entscheidungen (Nutzer, 2026-10-30: „Beantworte die Fragen selber. Nehme immer deinen Vorschlag.")

1. **Symmetrisch:** ein Synonym, dessen Slug dem Slug oder dem gefalteten Label eines **anderen** Schlagworts gleicht, ist
   `409 duplicate_interest_tag` – beim Anlegen und beim Ändern. *Kosten:* ein Vater kann „Soccer" nicht als Synonym an
   „Fußball" hängen, solange es ein eigenes Schlagwort „Soccer" gibt; er muss erst eines löschen oder zusammenführen.
2. **Beim Ändern zählen nur neue Synonyme** (deren Slug das Schlagwort noch nicht führt). Das Formular schickt die Liste
   immer mit; ohne diese Grenze sperrte ein Altdaten-Konflikt jedes Speichern – derselbe Fehler wie B-341.
3. **Der Slug-Vergleich bleibt**, auch wo er strenger ist als B-141 AK 3 (exaktes Label): ein Synonym „soccer" sperrt das
   Label „SOCCER". Grund: `EnsureAsync` löst Text über den Slug auf, also ist „SOCCER" dort schon das Synonym. Das ist eine
   bewusste Ausnahme zu B-141 AK 3, nur zwischen Label und Synonym.
4. **`EnsureAsync`** sucht Synonyme gegen den Slug des Textes **und** des Labels.
5. **Kommentar an `SynonymTakenAsync`** richtigstellen (Reihenfolge statt „nie").
6. **Nach dem Review ergänzt, damit die User Story („ein Text meint genau ein Schlagwort") stimmt:** ein Synonym darf auch
   kein **fremdes Synonym** sein, und ein **ausdrücklicher Slug** beim Anlegen kein fremdes Synonym. *Kosten:* zwei
   Schlagwörter können nicht mehr dasselbe Synonym führen; wer das will, legt das Wort als eigenes Schlagwort an.

## Akzeptanzkriterien

1. `POST` mit einem Synonym, das ein fremdes Label in anderer Schreibweise ist → `409` (geprüft an einem umbenannten
   Schlagwort, dessen Label nicht mehr zu seinem Slug faltet).
2. `PATCH` mit einem neuen solchen Synonym → `409`, Synonyme unverändert.
3. `PATCH` eines Schlagworts, dessen **bestehendes** Synonym mit einem fremden Label kollidiert (Altdaten), mit derselben
   Liste und neuer Farbe → `200`.
4. Kind-Interesse `{slug, label}`, dessen Label ein fremdes Synonym ist → trifft dieses Schlagwort, kein neues.
5. Die B-341-Fälle bleiben grün (Aufbau über den DbContext, weil die API den Konflikt nicht mehr herstellt).
6. `POST` mit einem Synonym, das ein fremdes Synonym ist, oder mit einem ausdrücklichen Slug, der ein fremdes Synonym ist
   → `409`.

## Schätzung

`S`, Backend, keine Migration, kein Vertragsbruch (ein zusätzlicher `409` an zwei Routen, die ihn schon dokumentieren).
Testweg: `InterestTagLabelTests`, E2E `schlagwort-trotz-fremdem-synonym` auf den Slug-Fall umgestellt.

## Verlauf

- **2026-10-30** — **Freigabe des Nutzers (Autonomer Modus, Opt-in)** im Dialog nach Lauf L: „Beantworte die Fragen
  selber. Nehme immer deinen Vorschlag." – die Entscheidungen oben sind der Vorschlag aus B-341 Entscheidung 2.
- **2026-10-30** — gebaut. Rote Probe: zwei neue Fälle **2 failed** (erwartet 2; AK 1/2 und AK 4). Der Altdaten-Fall (AK 3)
  ist vorher zwangsläufig grün (es gab keine Prüfung); seine Mutante „alle Synonyme prüfen" → **1 failed**. Weitere
  Mutanten je **1 failed** an ihrem Fall: `PATCH` ohne Prüfung, `POST` ohne Prüfung, `EnsureAsync` ohne Label-Slug.
  Der B-341-Synonym-Fall baut seinen Konflikt jetzt über den DbContext; die E2E-Spec `schlagwort-trotz-fremdem-synonym`
  fährt den Slug-Fall (grün, ohne den B-341-Fix **1 failed**).
- **2026-10-30** — Review `pugling-reviewer`: 🟡 die Label-Hälfte hing an keinem Fall (frisches Schlagwort, Slug = Label)
  → Fall auf ein umbenanntes Schlagwort umgestellt, Mutante „ohne Label" **1 failed**; 🟡 ein ausdrücklicher Slug umging
  die Prüfung → `SynonymTakenAsync(slug)` beim Anlegen, Mutante **1 failed**; 🟡 Synonym gegen fremdes Synonym ungedeckt
  → Entscheidung 6, Mutante **1 failed**; 🟢 `FirstOrDefault().Text` ohne sichtbares `null`, `EnsureAsync`-Summary, Tippfehler,
  „verglichen nach Slug" in der `PATCH`-Summary – alle behoben. Danach `InterestTagLabelTests` **14/14**.
- **2026-10-30** — Abnahme (Nachtrag zu Lauf L): volle Suite **1033/1033**, `tsc -b` sauber, E2E-Gesamtlauf **102 passed**
  (`exit=0`, nach dem letzten Produktcode-Edit). Rollengang: Vater/Creator über `schlagwort-trotz-fremdem-synonym` (Slug-Fall)
  und `interessen`/`einkleidung` im Gesamtlauf; Sohn Regressionszeuge. Kein Delta-Review (die Nachbesserungen sind je mit
  gemessener Mutante belegt, Selbst-Check). **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
