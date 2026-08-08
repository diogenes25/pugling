---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Zwei Verlage mit demselben Namen, Umbenennen umgeht den Slug, PATCH ohne Eindeutigkeitsprüfung]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 1)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
nachgeschaut: ""
wartet_auf: ""
---

# B-124 · Anlegen schützt die Eindeutigkeit, Umbenennen umgeht sie

Beim **Anlegen** eines Verlags oder einer Lehrwerk-Reihe verhindert der abgeleitete Slug eine Dublette –
genau das war der Zweck von [B-63](B-63-lehrwerk-hierarchie.md) („ein Vokabular statt fünf
Schreibweisen"). Beim **Umbenennen** prüft niemand etwas: ein `PATCH` auf den Namen kommt an der Regel
vorbei, die der `POST` durchsetzt. Danach stehen zwei Zeilen mit demselben Anzeigenamen in einer Liste,
die nur den Namen zeigt.

## User Story

Als **Creator** möchte ich, dass eine Umbenennung derselben Eindeutigkeitsregel unterliegt wie das
Anlegen — damit ich im Reihen-Formular nicht zwischen zwei gleich beschrifteten Verlagen raten muss.

## Ist-Stand am Code

**Drei** Schreibpfade, dieselbe Lücke:

1. **Verlag.** `Controllers/Creator/PublishersController.cs:69-72` leitet beim Anlegen den Slug ab und
   gibt bei Kollision den bestehenden Verlag zurück (idempotent). `:92-97` (`Update`) prüft nur
   `name.Length == 0` und schreibt dann `publisher.Name = name` — kein Vergleich gegen den Bestand.
2. **Lehrwerk-Reihe.** `Controllers/Creator/TextbookSeriesController.cs:125-131` (`Create`) ebenso über
   den Slug abgesichert; `:167-172` (`Update`) ebenso nur mit Leer-Prüfung.
3. **Interessen-Tag.** `Controllers/Creator/InterestTagsController.cs:82-86` (`Create`) ebenso;
   `:115-120` (`Update`) ebenso. Erst beim Bauen gefunden (siehe `## Verlauf`) — die geteilte
   Interessen-Taxonomie, an der Bilder *und* Kinder hängen.

Alle drei Entitäten tragen einen Unique-Index auf `Slug` (`PuglingDbContext.cs:213,220,318`), **keinen**
auf `Name`/`Label` — die Datenbank fängt es also nicht auf, und der Slug bleibt auf einem PATCH bewusst
unverändert (alle drei XML-Docs sagen das ausdrücklich: „The slug stays fixed" / „remains immutable" /
„deliberately immutable").

**Sichtbar wird es in der Oberfläche, die nur den Namen rendert:** `frontend/src/vater/NewSeries`
(Verlags-`<select>`), der Verlags-Filter und `PublisherAdmin.tsx` zeigen `p.name` ohne Slug.

## Die echte Lücke

Nicht „die Identität ist kaputt" — die Identität ist der Slug, und der bleibt eindeutig. Die Lücke ist,
dass die **Unterscheidbarkeit** für den Menschen an einem Feld hängt, das keine Regel schützt. Der
`POST` etabliert eine Invariante, die der `PATCH` still wieder aufheben darf; das ist genau der
Zustand, den B-63 beenden sollte, nur eine Tür weiter.

Reichweite heute: kein Datenverlust, keine falsche Zuordnung — der Schaden ist eine Auswahlliste, in der
zwei Einträge gleich heißen und man nicht sagen kann, welcher gemeint ist.

## Entscheidungen

1. **Der abgeleitete Slug ist die Prüfgröße, nicht der Name.** Beim `PATCH` wird aus dem *neuen* Namen
   ein Slug abgeleitet und abgelehnt, wenn ihn bereits ein **anderer** Datensatz trägt (Selbstausschluss
   über die Id). Begründung: das ist wortgleich die Regel, die `Create` schon durchsetzt — dieselbe
   Funktion (`DeriveRequiredSlug`), derselbe Vergleich, kein zweiter Eindeutigkeitsbegriff. Nebenwirkung
   und gewollt: Groß-/Kleinschreibung und Satzzeichen fallen dabei zusammen („klett" kollidiert mit
   „Klett"), weil genau das die Aufgabe eines Slugs ist. **Kosten:** eine zusätzliche `AnyAsync`-Abfrage
   je PATCH mit Namensänderung.
2. **Der Slug selbst bleibt unveränderlich.** Er wird nur als Kollisionstest benutzt, nicht neu
   geschrieben. Begründung: beide XML-Docs sagen das zu, und Agenten referenzieren Reihen über den Slug
   — ihn beim Umbenennen mitwandern zu lassen bräche stabile Verweise. **Kosten:** nach einer
   Umbenennung können Name und Slug auseinanderlaufen („Klett" mit Slug `klett` wird zu „Klett Verlag"
   mit Slug `klett`). Das ist hinnehmbar, weil der Slug ohnehin nirgends angezeigt wird.
3. **Alle Schreibpfade in einer Story**, nicht in dreien. Begründung: es ist **eine** Regel an mehreren
   Stellen (Muster [B-98](B-98-idempotenter-link-post-luegt.md), das drei Controller als eine Regel
   behandelt hat); getrennt gebaut müsste jede Folge-Story die Formulierung der ersten wiederholen, und
   die drei liefen auseinander. **Kosten:** der Diff berührt drei Controller statt einen — und die Regel
   muss beim nächsten slug-idempotenten `Create` mitgedacht werden, ohne dass ein Tor daran erinnert
   (siehe Entscheidung 5).
4. **Ein eigener Fehlercode je Ressource, additiv** — `duplicate_publisher`, `duplicate_series_slug`,
   `duplicate_interest_tag`. Begründung: [B-101](B-101-fehlercodes-und-drei-waechter.md) hat den
   generischen `Conflict` unter `Controllers/**` verboten und hält das mit einem Wächter; ein Aufrufer
   muss die Fälle maschinell unterscheiden können, weil sie an verschiedenen Formularen entstehen.
   **Kosten:** drei Zeilen in `ApiErrors`, kein Vertragsbruch (additiv).
5. **Kein Tor über „jedes slug-idempotente Create braucht ein geschütztes Update".** Zurückgestellt,
   nicht verworfen: die Zuordnung Create↔Update ist reflexiv nicht sauber entscheidbar (dieselbe
   Begründung wie [B-97](B-97-unique-index-ohne-vorpruefung.md), Entscheidung 3, zu den 47 Unique-Indizes).
   **Kosten:** die Regel hängt weiter an Disziplin — und genau daran ist sie dreimal gescheitert. Wenn
   ein vierter Fall auftaucht, ist das der Anlass, es doch mechanisch zu versuchen.

## Akzeptanzkriterien

1. `PATCH creator/publishers/{id}` auf einen Namen, dessen Slug bereits ein anderer Verlag trägt,
   antwortet `409` mit `code: duplicate_publisher` — und der Name bleibt unverändert.
2. `PATCH creator/textbook-series/{id}` auf einen Namen, dessen Slug bereits eine andere Reihe trägt,
   antwortet `409` mit `code: duplicate_series_slug` — und der Name bleibt unverändert.
3. `PATCH creator/interest-tags/{id}` auf ein Label, dessen Slug bereits ein anderer Tag trägt,
   antwortet `409` mit `code: duplicate_interest_tag` — und das Label bleibt unverändert.
4. Eine Umbenennung, deren Slug gleich bleibt (reine Schreibweise: „klett" → „Klett"), geht durch —
   keine Selbstkollision.
5. Eine Umbenennung auf einen freien Namen geht unverändert durch.
6. Je Fall ein Integrationstest, der **vor** der Änderung rot war (Abnahmeform `art: Defekt`).

## Schätzung

**Größe: S** (Anker: B-01, „`childId` aus dem Test-Pfad ziehen" — hier zwei Controller-Zweige plus zwei
`ApiErrors`-Zeilen und vier Testfälle). `wo: backend`, `migration: nein` (die Unique-Indizes auf `Slug`
existieren seit B-63), `vertragsbruch: nein` (zwei additive Fehlercodes, ein zusätzlicher `409` an zwei
Actions).

**Risiko:** Der Selbstausschluss muss über die Id laufen, nicht über den Slug — sonst kollidiert eine
Zeile mit sich selbst und jede Umbenennung schlägt fehl. Genau dieselbe Falle wie in
[B-97](B-97-unique-index-ohne-vorpruefung.md) („beim PATCH die Zeile selbst ausschließen").

**Bewusst nicht gelöst:** Die Vorprüfung ist nicht atomar — zwei exakt parallele Umbenennungen auf
denselben Namen könnten beide durchkommen. Dieselbe akzeptierte Entscheidung wie in B-97/B-104: kein
globaler `DbUpdateException`→409-Handler. Hier ohnehin folgenlos, weil auf `Name` kein Index liegt.

**Bewusst nicht gelöst, und wichtiger: die Regel schützt den Slug, nicht den Namen.** Vom
`pugling-reviewer` gefunden (Befund 1); es beschneidet die Reichweite dieser Story ehrlich. Weil der Slug
beim Umbenennen stehen bleibt (Entscheidung 2), löst sich der Name nach einer Umbenennung von ihm — und
dann bleibt ein **zweistufiger** Weg zur Namensdublette offen:

```text
POST  {name:"Klett"}          → id 1, Slug "klett"
PATCH id 1 {name:"Cornelsen"} → 200   (Slug "cornelsen" ist frei) → Name "Cornelsen", Slug "klett"
POST  {name:"Cornelsen"}      → 201   (Slug "cornelsen" immer noch frei) → zweite Zeile "Cornelsen"
```

Warum es trotzdem so bleibt: Den Weg zu schließen hieße, die Eindeutigkeit auf den **Namen** zu heben —
entweder über einen zweiten Unique-Index (Schemaänderung, Migration und eine zweite
Eindeutigkeits-Semantik neben dem Slug) oder indem `Create` seine Idempotenz zusätzlich am Namen
festmacht, was seine dokumentierte Zusage („derselbe Katalogaufbau darf wiederholt werden") für Agenten
ändert. Beides ist eine Produktentscheidung und nicht das Ziel dieser Story; sie schließt den
**direkten** Weg, auf dem eine Dublette heute mit einem einzigen Aufruf entsteht.

Aus derselben Einsicht sagen die `detail`-Texte „the slug this name derives to" statt „this name"
(Reviewer-Befund 2): nach einer Umbenennung — und bei einem Interessen-Tag mit ausdrücklich gesetztem
Slug ohnehin — trägt die kollidierende Zeile den genannten Namen womöglich gar nicht, und der Aufrufer
suchte ihn vergeblich in seiner Liste.

**Angriffsplan:** `ApiErrors` additiv → beide `Update`-Methoden um die Ableitung + `AnyAsync`-Prüfung →
`ProducesResponseType(409)`-Annotationen. **Testweg:** vier Integrationstests (zwei je Ressource:
Kollision und Selbst-Umbenennung), rot gegen den Vorzustand per `git stash` verifiziert.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`. Beide Fundstellen selbst
  am Code nachgeprüft (`PublishersController.cs:92-97`, `TextbookSeriesController.cs:167-172`); die
  zweite ist ein **Nachbar-Fund beim Verifizieren des ersten** — der Review nannte nur den Verlag.
  `entgangen_bei: [B-63]`: beide Controller sind in dieser Story entstanden und waren `abgenommen`.
- **2026-08-07** — gegrillt, geschätzt und gebaut (autonom, `art: Defekt`). **Rote Probe zuerst**, neue
  `SlugRenameGuardTests.cs` gegen den Vorzustand: **2 Failed / 2 Passed** — beide Kollisionsfälle
  `Assert.Equal() Failure … Expected: Conflict, Actual: OK` (die Umbenennung ging schlicht durch), die
  beiden Positivfälle (eigene Schreibweise, freier Name) waren schon grün. Genau die richtige Form: der
  Test bestraft nur das, was fehlt.
  Umgesetzt wie im Angriffsplan: `ApiErrors.DuplicatePublisher`/`DuplicateSeriesSlug` additiv, in beiden
  `Update`-Methoden `DeriveRequiredSlug` auf den neuen Namen plus `AnyAsync`-Prüfung mit Selbstausschluss
  über die Id, `[ProducesResponseType(409)]` an beiden Actions. Danach **4/4** grün, volle Suite
  **767/767** (761 vor diesem Sprint, +4 hier, +2 aus B-125).
  **Ein eigener Fehler beim Testschreiben**, festgehalten weil er lehrreich ist: der erste Lauf zeigte
  *drei* rote Tests statt zwei — der dritte scheiterte an `ObjectDisposedException: Cannot access a
  closed Stream`, weil `TestApi.IdAsync` den Content-Stream konsumiert und ich ihn danach ein zweites
  Mal für den Slug las. Kein Produktdefekt; der Test liest den Rumpf jetzt einmal.
  `docs/openapi/v1.json` gewachsen (neue Fehlercodes im Enum, `409`-Antworten), `docs/api-examples/index.md`
  führt die Codes als „von DocsCaptureTests nicht mitgeschnitten" — die B-84-Formulierung, die nicht mehr
  behauptet, als sie belegt.
- **2026-08-07** — **dritter Schreibpfad beim Bauen gefunden**, nicht vom Review: `InterestTagsController`
  (`Create` slug-idempotent, `Update` ungeschützt) — dieselbe Regel an einer dritten Ressource. Nach
  Entscheidung 3 mitgenommen statt als eigene Story geführt; Ist-Stand, Entscheidungen und AK sind auf
  drei Pfade gezogen. **Rote Probe auch dafür** (`git stash` nur auf den Controller):
  `Assert.Equal() Failure … Expected: Conflict, Actual: OK`, 1 Failed / 0 Passed. Danach volle Suite
  **768/768**.
- **2026-08-07** — `pugling-reviewer` gefahren: **kein Blocker**, fünf „Sollte", vier „Nice-to-have" —
  alle übernommen bis auf einen, der bewusst zur Dokumentation statt zum Code wurde:
  - **Befund 1 (die Regel schützt den Slug, nicht den Namen):** der zweistufige Weg `PATCH` → `POST`
    erzeugt weiter zwei gleich heißende Zeilen. **Nicht gebaut, sondern benannt** — siehe „Bewusst nicht
    gelöst" in der Schätzung; ihn zu schließen wäre eine Produktentscheidung über die Idempotenz von
    `Create`.
  - **Befund 2 (`detail` behauptete mehr als geprüft wird):** alle drei Meldungen sagen jetzt „the slug
    this name derives to". Der `code` bleibt, also kein Vertragsbruch.
  - **Befund 3 (der Selbstausschluss war nur auf 1 von 3 Pfaden getestet):** `SlugRenameGuardTests` als
    `[Theory]` über `(root, field, code)` neu gebaut — deckt Kollision, Selbst-Umbenennung und freien
    Namen für alle drei Ressourcen ab. **9/9 grün** statt vorher 5 Tests mit einer Blindstelle je Kopie.
  - **Befund 4 (dritter Pfad nicht dokumentiert):** war zum Review-Zeitpunkt richtig, ist mit dem Eintrag
    oben erledigt.
  - **Befund 5** betrifft B-125 und ist dort eingearbeitet.
  - **Nice-to-have 6** (ungeprüfte Vorbedingung) und **7** (`duplicate_series_slug` benannte das Feld,
    die anderen die Entität) übernommen: der Code heißt jetzt `duplicate_textbook_series`, noch additiv
    und unveröffentlicht. **8** (`<see cref="Create"/>` zerlegt sich im OpenAPI-Text zu einer vollen
    Methodensignatur) an allen drei Stellen auf `<c>Create</c>` umgestellt.
  - **Ein transientes Rot** im Review-Lauf (mein neuer Interessen-Tag-Test, `POST` antwortete einmal 400)
    war **nicht reproduzierbar** und fiel in ein Zeitfenster, in dem ich den Arbeitsbaum parallel
    editierte — der Reviewer verlangt zu Recht einen sauberen Voll-Lauf auf stehendem Baum vor dem
    Commit.
- **2026-08-07** — **abgenommen.** Sauberer Voll-Lauf auf stehendem Baum: `dotnet test Pugling.sln -c
  Release` → **772/772 grün** (761 vor diesem Sprint, +9 aus der Theory, +2 aus B-125); die Theory allein
  ein zweites Mal gefahren, **9/9 grün** — das transiente Rot des Reviews reproduziert nicht.
  **Rollengang-Ersatz:** kein UI-Kandidat im engeren Sinn — die Änderung ist eine zusätzliche Ablehnung
  auf drei PATCH-Pfaden, kein neuer Weg durch die Oberfläche. Ersatz nach `docs/nachtlauf.md`: die drei
  rot→grün-Belege über die echte HTTP-Schicht (die Tests fahren den Weg, den das Vater-Web fährt), die
  volle Suite und der Reviewer. Die Chrome-Extension ist in dieser Sitzung nicht verbunden.
