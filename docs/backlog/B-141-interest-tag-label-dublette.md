---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Zwei Interessen-Tags mit demselben Label, vierte slug-idempotente Ressource]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-136 (Nachtlauf 2026-08-10, Entscheidung 3)
entgangen_bei: []
---

# B-141 · Zwei Interessen-Tags dürfen dasselbe Label tragen

Dieselbe Defektklasse wie B-124 → B-133 → [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md), zum
**vierten** Mal — jetzt bei der Ressource, die B-136 beim Grillen als Kandidatin benannt hat.
`InterestTagsController.Create` gibt bei einem Slug-Treffer bedingungslos die vorhandene Zeile zurück,
`Update` prüft Slug gegen Slug. Der Slug friert beim Umbenennen ein, also können danach zwei Tags dasselbe
`Label` tragen — und ein Label ist genau das, was in jedem Auswahlfeld steht. Der Kommentar am Controller
sagt es sogar selbst („otherwise two tags share a label in every picker") und erzwingt es trotzdem nicht.

**Warum nicht in B-136 mitgenommen** (dessen Ziel ist ohne diese Story erfüllt): Es ist **kein
gleichgelagerter Fall**, und beide Unterschiede sind teuer.

1. **`InterestTag.Label` trägt keine `NOCASE`-Collation.** Die tragen nur `Publisher.Name`,
   `TextbookSeries.Name` und `Vocabulary.Word`/`Translation` (`Data/PuglingDbContext.cs:222,238,292-293`,
   beim Grillen von B-136 nachgezählt). Eine schreibweisen-tolerante Namensprüfung braucht dort eine
   Schemaänderung — und die faltet die Migrationskette neu. Aus dem `S` der Verlags-Story würde ein `M`
   mit `migration: ja`.
2. **`Create` nimmt einen ausdrücklichen Slug entgegen** (`CreateInterestTagDto.Slug`), und der
   Update-Kommentar hält als **bewusste Entscheidung** fest, dass ein Label darum legitim vom Slug
   abweichen darf: „as strong as Create's rule and no stronger". Ob Label-Eindeutigkeit hier überhaupt
   gewollt ist, ist damit erst zu entscheiden — bei Verlag und Reihe stellte sich die Frage nicht.

Beim Ausformulieren zu klären: ob die Eindeutigkeit fachlich gewollt ist (Punkt 2), und falls ja, ob sie
schreibweisen-tolerant sein muss oder ein `Ordinal`-Vergleich reicht — Letzteres käme ohne Migration aus
und wäre die deutlich billigere Hälfte. Ebenfalls zu prüfen: ob damit die vierte Wiederholung den
geteilten Helfer bzw. Wächter rechtfertigt, den B-136 ausdrücklich zurückgestellt hat.

## User Story

Als **Creator** (und als Vater, der Interessen tippt) möchte ich, dass zwei Schlagwörter nie denselben Namen tragen,
damit ich in jedem Auswahlfeld weiß, welches ich nehme.

## Ist-Stand (Lauf K, am Code belegt)

- `InterestTagsController.Create` (`:99-100`) gibt bei einem Slug-Treffer die vorhandene Zeile zurück, sonst legt es an –
  ohne Blick auf das Label. `Update` (`:146`) prüft nur den abgeleiteten Slug gegen fremde Slugs.
- Der dritte Geburtsweg, `InterestTagService.EnsureAsync` (Kind-Interessen, Bild-Schlagworte, Backfill), sucht nach Slug
  und Synonym, nicht nach Label (`InterestTagService.cs:36-46`). Ein umbenanntes Schlagwort („Fußball“ → „Sport“,
  Slug bleibt `fussball`) wird dort also von „Sport“ nicht gefunden.
- **Gemessen** (rote Probe unten): Anlegen mit dem Label eines umbenannten Tags → `201`; Umbenennen auf das Label eines
  Tags mit eigenem Slug → `200`; Interesse „Sport“ → neuer Tag.

## Die echte Lücke

Der Slug soll Labels auseinanderhalten, kann es aber nicht: er ist beim Anlegen frei wählbar und friert beim Umbenennen
ein. Die Regel muss am Label selbst sitzen – an allen drei Stellen, an denen ein Label entsteht.

## Entscheidungen

1. ~~Ist ein eindeutiges Label gewollt?~~ **Ja, exakter (Ordinal-)Vergleich, keine Migration; Anlegen und Umbenennen
   → `409`** – Entscheidung des Nutzers, Grill-Runde G-3 vom 2026-10-23 (siehe Verlauf).
2. **Der idempotente Slug-Treffer beim Anlegen bleibt vor der Label-Prüfung** (autonom, Freigabe 1): ein wiederholter
   Katalog-Aufbau bekommt weiter `200` mit der vorhandenen Zeile. *Kosten:* keine.
3. **`EnsureAsync` behandelt ein exakt gleiches Label als Treffer** statt einen zweiten Tag anzulegen (autonom,
   Freigabe 1). Dort ist `409` keine Antwort – der Pfad heißt „finden oder anlegen“, und der Tag, den der Vater im
   Auswahlfeld unter diesem Namen sieht, ist der gemeinte. Ohne das hielte die Regel aus Entscheidung 1 nur an zwei von
   drei Geburtswegen. *Kosten:* ein Kind-Interesse „Sport“ landet auf dem Tag mit Slug `fussball` – richtig, weil er
   „Sport“ heißt, aber im Slug nicht ablesbar.
4. **Deutscher Text für `duplicate_interest_tag`** im Vater-Web (autonom): der Code ist über das Umbenennen in der
   Schlagwort-Verwaltung erreichbar und schlug bisher englisch durch (Regel über `GERMAN_PROBLEM_TEXT`). *Kosten:* ein
   Eintrag.
5. Der geteilte Helfer/Wächter über alle slug-idempotenten Ressourcen, den B-136 zurückgestellt hat, bleibt
   **zurückgestellt**: die Label-Regel gilt hier ohne `NOCASE` und ist damit anders als bei Verlag/Reihe.

## Akzeptanzkriterien

1. Anlegen mit einem Label, das ein anderer Tag trägt (bei anderem Slug) → `409 duplicate_interest_tag`, kein neuer Tag.
2. Umbenennen auf ein solches Label → `409`, Label unverändert; das eigene Label unverändert mitschicken → `200`.
3. Eine andere Schreibweise (Groß/Klein) bleibt erlaubt.
4. Ein getipptes Interesse mit exakt diesem Label trifft den vorhandenen Tag.
5. Das Vater-Web zeigt zu `duplicate_interest_tag` einen deutschen Satz.

## Schätzung

`S`, Backend + ein Frontend-Eintrag, keine Migration, kein Vertragsbruch (`409` stand schon im Vertrag von `PATCH`; am
`POST` neu dokumentiert). Testweg: `InterestTagLabelTests` (neu), `errorMessage.test.ts`.

## Verlauf

- **2026-08-10** — angelegt beim Grillen von [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md)
  (Entscheidung 3) im Nachtlauf. Der Ist-Stand ist am Code belegt (Controller und Collationen einzeln
  nachgesehen); `unverifiziert: true` bleibt trotzdem stehen, weil die **fachliche** Frage aus Punkt 2
  nicht erhoben ist.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht und ist herstellbar (Tag „Fußball“ → „Sport“ umbenennen, Slug bleibt `fussball`; danach „Sport“ neu anlegen ergibt zwei Labels „Sport“, `InterestTagsController.cs:99-100,141-148`). Der Kommentar `:122-125` behauptet die Regel, `:143-145` erlaubt das Gegenteil. Die fachliche Frage (ist ein eindeutiges Label gewollt?) bleibt beim Menschen; B-155 hängt weich daran.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-3 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): das Label soll **eindeutig** sein, mit einem **exakten (Ordinal-)Vergleich ohne Migration**. Anlegen und Umbenennen auf ein vorhandenes Label liefern 409; Schreibweisen-Varianten bleiben unterscheidbar. Der Controller-Kommentar (`InterestTagsController.cs:143-145`) wird angeglichen. Damit ist die fachliche Frage beantwortet und die Story als `Defekt` autonom grill- und baubar (Paket NP-16).
- **2026-10-29** — Lauf K, Teil 1: ausformuliert, Restpunkte autonom gegrillt (Freigabe 1 + die Nutzer-Entscheidung oben),
  geschätzt, gebaut. Rote Probe: `InterestTagLabelTests` **3 failed / 1 passed** (erwartet 3/1 – der Schreibweisen-Fall
  ist heute schon grün): Z. 25 `Created` statt `Conflict`, Z. 43 `OK` statt `Conflict`, Z. 76 neuer Tag `[11]` statt
  `[10]`. Danach alle Interessen-/Slug-/PATCH-/Medien-Klassen **108/108**. Mutanten: Anlegen ohne Label-Prüfung **1 failed**
  (Z. 25); Umbenennen ohne **1 failed** (Z. 43); case-insensitiv **1 failed** (Schreibweisen-Fall, Z. 57); `EnsureAsync`
  ohne Label **1 failed** (Z. 76); „sich selbst nicht ausnehmen“ blieb **grün** → Fall „eigenes Label unverändert
  mitschicken“ ergänzt, dann **1 failed** (Z. 60, `Conflict`). Frontend: `errorMessage.test.ts` mit gestrichenem Eintrag
  **1 failed / 7 passed**, mit Eintrag **8/8**.
- **2026-10-29** — Review `pugling-reviewer`: keine Funde an B-141 selbst; 🟢 die Label-Eindeutigkeit ist eine Prüfung im
  Code ohne Index (keine Migration, wie entschieden) – zwei gleichzeitige Anlagen können beide durchkommen. Bewusst so
  belassen, hier benannt.
- **2026-10-29** — Abnahme Lauf K, Teil 1: volle Suite **1015/1015**, Vitest **576/576**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet; danach nur ein Testkommentar berichtigt). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Creator/Vater über `schnellanlage-ohne-dublette` und `auch-richtig-im-uebungseditor`, Sohn als Regressionszeuge über seine Specs. Reviews: `pugling-reviewer`, `frontend-reviewer`, zwei Delta-Reviews (das letzte ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
