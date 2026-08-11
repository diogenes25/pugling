---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Verlag doppelt nach Umbenennen, DuplicatePublisher greift zu spät]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zum Sprint 1 des Nachtlaufs (2026-08-09), Fund 2
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-08-10
wartet_auf: ""
---

# B-136 · Beim Verlag steht dieselbe Dublettenlücke wie bei der Reihe

Die Defektklasse aus [B-133](B-133-zwei-reihen-ein-anzeigename.md), eine Ressource weiter: Der
Verlags-Wächter vergleicht Slug gegen Slug, und der Slug friert beim Umbenennen ein.

## User Story

Als **Creator** möchte ich, dass ein Verlagsname genau einen Verlag meint — sonst stehen im
Auswahlfeld einer Reihe zwei ununterscheidbare „Cornelsen".

## Ist-Stand am Code

- `Controllers/Creator/PublishersController.cs:77-78` — `Create` trifft eine bestehende Zeile **nur**
  über den Slug und gibt sie idempotent zurück.
- `Controllers/Creator/PublishersController.cs:111-113` — `Update` prüft Slug gegen Slug.
- `ApiErrors.DuplicatePublisher` existiert bereits.
- `Data/PuglingDbContext.cs` — `Publisher.Name` trägt seit [B-128](B-128-katalogsuche-case-sensitiv.md)
  die `NOCASE`-Collation. Die ist heute **wirkungslos**: es gibt im Backend keinen einzigen
  Gleichheitsvergleich auf `Publisher.Name` (nachgemessen vom Reviewer). Diese Story macht sie tragend.

Der Ablauf ist derselbe wie bei der Reihe: Verlag „Klett" (Slug `klett`) in „Cornelsen" umbenennen →
`POST {name:"Cornelsen"}` → Slug `cornelsen` ist frei → zweiter Verlag „Cornelsen".

## Die echte Lücke

Nicht „eine vergessene Prüfung", sondern **dieselbe Klasse zum dritten Mal**: B-124 hat sie beim
Umbenennen der Reihe geschlossen, B-133 beim Anzeigenamen der Reihe, und beim Verlag steht sie noch.
Der eigentliche Befund ist, dass „idempotent über den Slug" und „Anzeigename ist eindeutig" zwei
verschiedene Zusicherungen sind, die im Code wie eine aussehen.

## Offene Punkte

1. ~~**Nur die Prüfung, oder gleich das Muster festhalten?**~~ → Entscheidungen 1 und 2.
2. ~~**Gilt es auch für `InterestTag`?**~~ → erhoben (Entscheidung 3): ja, dieselbe Lücke — aber **kein
   gleichgelagerter Fall**.

## Entscheidungen

1. **Nur die Prüfung, wörtlich nach dem Vorbild `TextbookSeriesController` — und zwar in beiden
   Richtungen.** (a) Ein Slug-Treffer wird nur dann idempotent beantwortet, wenn der Anzeigename
   übereinstimmt (`OrdinalIgnoreCase`), sonst `409 duplicate_publisher`; (b) ein freier Slug heißt nicht
   freier Name — die Namensprüfung läuft über die Collation. Begründung: `Publisher.Name` trägt seit
   B-128 bereits `NOCASE` (`Data/PuglingDbContext.cs:222`), die Prüfung kostet also **keine**
   Schemaänderung, und das fertige Vorbild steht in der Nachbardatei
   (`TextbookSeriesController.cs:141-160`). Kosten: der dritte fast wörtlich gleiche Block dieser Art —
   bewusst geduldet, siehe Entscheidung 2.
2. **Kein geteilter Helfer und kein Wächter in diesem Sprint.** Der Auslöser der Ausformulierung („erst
   bei einer vierten slug-idempotenten Ressource") ist zwar erreicht — `InterestTag` hat dieselbe Lücke —,
   aber die vierte ist kein gleichgelagerter Fall (Entscheidung 3). Ein Helfer über zwei echte und einen
   abweichenden Fall abstrahiert genau die Abweichung weg, auf die es ankommt. Kosten: die Klasse kann ein
   viertes Mal auftreten, ohne dass ein Tor es meldet — das ist der bewusst offene Rest.
3. **`InterestTag` wird eine eigene Story, nicht Teil dieser.** Zwei Unterschiede, jeder für sich
   ausreichend. (a) `InterestTag.Label` trägt **keine** `NOCASE`-Collation — die tragen nur
   `Publisher.Name`, `TextbookSeries.Name` und `Vocabulary.Word`/`Translation`
   (`Data/PuglingDbContext.cs:222,238,292-293`, nachgezählt). Eine schreibweisen-tolerante Namensprüfung
   bräuchte dort eine Schemaänderung, und die faltet die Migrationskette neu — aus `S` würde `M` mit
   `migration: ja`. (b) `InterestTags.Create` nimmt einen **ausdrücklichen** Slug entgegen, und der
   Update-Kommentar hält als bewusste Entscheidung fest, dass ein Label darum legitim vom Slug abweichen
   darf („as strong as Create's rule and no stronger"). Ob Label-Eindeutigkeit dort überhaupt gewollt ist,
   ist eine offene Frage — und keine, die beim Reparieren des Verlags nebenbei fällt. Kosten: die Lücke
   bleibt bei den Interessen-Tags bestehen, aber sichtbar als eigene Story statt als Schweigen.

## Akzeptanzkriterien

1. `POST` und `PATCH` auf einen Verlagsnamen, den ein **anderer** Verlag trägt, antworten `409
   duplicate_publisher` — auch wenn die Slugs verschieden sind.
2. Ein idempotenter Slug-Treffer, dessen Anzeigename **nicht** zum geposteten Namen passt, gibt nicht
   still die falsche Zeile heraus (die Spiegelseite, die in B-133 der Reviewer gefunden hat).
3. Die Idempotenz bleibt — **solange Name und Slug zusammenpassen**: derselbe Name liefert denselben
   Verlag. Nach einer Umbenennung liefert ein Replay des alten `POST {name}` bewusst `409` statt der Zeile;
   das ist der Kern von Kriterium 2 und keine Einschränkung nebenbei. Deckungsgleich mit
   `textbook-series` seit B-133.
4. Je ein Integrationstest, vorher rot.

## Schätzung

**Größe: S** — zwei Prüfblöcke von je fünf Zeilen, wörtlich nach einem Vorbild in der Nachbardatei, plus
drei Integrationstests. Vergleichbar mit B-01 (`childId` aus dem Testpfad ziehen).

- **`wo: backend`** — kein Bedienelement ändert sich; das Frontend zeigt den `409` über den bestehenden
  Fehlerpfad an.
- **`migration: nein`** — `Publisher.Name` trägt `NOCASE` seit B-128 (`PuglingDbContext.cs:222`). Genau
  darum ist diese Story `S` und die `InterestTag`-Schwester es nicht.
- **`vertragsbruch: nein`** — `ApiErrors.DuplicatePublisher` existiert, kein DTO ändert sich. Additiv ist
  allein ein `[ProducesResponseType(409)]` an `Create`.
- **Risiken:** 1. Der Selbstvergleich beim `PATCH` — ohne `p.Id != id` kollidiert die Zeile mit sich
  selbst und keine Umbenennung ginge mehr durch (die Falle aus B-97, im bestehenden Slug-Zweig bereits
  richtig gelöst und beim Namenszweig zu wiederholen). 2. Die bekannte Asymmetrie aus B-133: der
  `OrdinalIgnoreCase`-Vergleich in C# faltet volles Unicode, `NOCASE` in SQLite nur ASCII — die Richtung
  ist ungefährlich (NOCASE-gleich impliziert `OrdinalIgnoreCase`-gleich), aber der Kommentar muss es sagen,
  sonst rechnet der nächste Leser es neu aus.
- **Angriffsplan:** 1. `PublishersController.Create`: Slug-Treffer nur bei Namensgleichheit `Ok`, sonst
  409. 2. Derselbe Ort: freier Slug → Namensprüfung. 3. `Update`: Namensprüfung neben die bestehende
  Slug-Prüfung, beide mit `p.Id != id`. 4. Drei Integrationstests, jeder vorher rot mit genannter Zahl.
- **Testweg:** `backend/Pugling.Api.Tests` — die vorhandene Verlags-Testklasse erweitern (dieselbe, die
  B-128s Collation prüft), im Muster der `TextbookSeries`-Tests aus B-133.

## Verlauf

- **2026-08-09** — angelegt aus dem `pugling-reviewer`-Befund zu Sprint 1 des Nachtlaufs, Fund 2.
  **Bewusst nicht in B-133 mitgenommen:** dessen Ziel (die Reihe) ist ohne den Verlag erfüllt, und der
  Fehler liegt außerhalb des Diffs — er ist älter als dieser Sprint. Der Ist-Stand stammt aus dem Review
  und ist Zeile für Zeile belegt.
- **2026-08-10** — gegrillt und geschätzt im Nachtlauf (autonom nach Freigabe 1, `art: Defekt`). Der
  offene Punkt 2 ist damit **erhoben** statt vermutet, und die Antwort hat den Zuschnitt geändert:
  `InterestTag` hat dieselbe Lücke, ist aber kein gleichgelagerter Fall (fehlende Collation ⇒ Migration;
  ausdrücklicher Slug ⇒ Label-Eindeutigkeit erst zu entscheiden). Größe **S**, `wo: backend`,
  `migration: nein`, `vertragsbruch: nein`.
- **2026-08-10** — gebaut und **abgenommen** (Nachtlauf, Sprint 1). **Rote Probe: 9 von 11 rot** (gemeinsam
  mit B-135 gemessen). Ein Zwischenschritt gehört ins Protokoll, weil er sonst wie ein behobener Fehler
  aussähe: einer der drei Tests war **zunächst grün**. Die naheliegende Fassung („zwei Verlage anlegen,
  einen auf den anderen umbenennen") prüft nichts Neues — solange Name und Slug zusammenpassen, sind „Name
  vergeben" und „Slug vergeben" dieselbe Frage, und der bestehende Slug-Wächter beantwortet sie. Der Test
  baut jetzt den entkoppelten Zustand und war danach rot (8/11 vor dem Nachschärfen, 9/11 danach).
  `pugling-reviewer`: kein Blocker; sein Hinweis, dass Akzeptanzkriterium 3 zu absolut formuliert war
  (Idempotenz gilt nur, solange Name und Slug zusammenpassen), ist in die Kriterien eingearbeitet, und der
  nicht-ASCII-Restfehler steht jetzt auch im Verlags-Kommentar statt nur im Vorbild.
  **Live-Probe gegen die laufende API** statt Browser-Gang, weil die Klick-Steuerung auf dieser Seite
  unzuverlässig griff: anlegen → umbenennen → denselben Namen erneut posten ⇒ **409 `duplicate_publisher`**.
  Suite **801/801**.
- **2026-08-10** — nachgeschaut (Nachtlauf, Retro des Sprints 2). Geprüft wurde der Zweig, den die
  Integrationstests **nicht** erreichen: die Namensprüfung über die `NOCASE`-Collation. Sie ist nur nach
  einer entkoppelnden Umbenennung erreichbar — eine Schreibweisen-Variante allein leitet immer denselben
  Slug ab, also antwortet der Slug-Zweig. Live gegen die laufende API: „Nachschau Diesterweg" anlegen →
  auf „Nachschau Beltz" umbenennen → „NACHSCHAU BELTZ" anlegen ⇒ **409**. Die Collation, die B-128 gelegt
  hat und die bis zu dieser Story wirkungslos war, trägt damit belegt und schreibweisen-tolerant. Kein
  durchgekommener Defekt.
- **2026-08-10** — **nachgeschaut** im Nachtlauf (Sprint 3). Geprüft wurde die Behauptung *Namensprüfung
  in beiden Wegen, im `Update` mit `p.Id != id`* — `PublishersController.cs:107` (Create) und `:149`
  (Update, mit dem Ausschluss). Trägt unverändert. Derselbe Controller wurde im Sprint um die
  Löschsperre erweitert ([B-127](B-127-verlag-loeschen-trifft-fremde.md)); die bestehenden
  `PublishersTests` sind dabei grün geblieben, auch der Fall, der einen Verlag mit eigener Reihe löscht.
