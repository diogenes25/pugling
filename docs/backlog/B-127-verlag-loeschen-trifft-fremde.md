---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Verlag löschen ohne Eigentum, SetNull auf fremde Reihen, geteilte Zeile ohne Schutz]
status: abgenommen
prio: P3
art: Frage
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
unverifiziert: false
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 2)
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-08-11
wartet_auf: ""
---

# B-127 · Jeder Creator darf einen Verlag löschen, den alle benutzen

`DELETE creator/publishers/{id}` steht jedem Konto mit der Creator-Rolle offen — auf einer Zeile, die
**global geteilt** ist. Der Fremdschlüssel räumt danach still auf: jede Reihe, die auf den Verlag zeigte,
verliert ihre Zuordnung (`SetNull`), auch die Reihen aller anderen Creator. Ein Lehrer-Konto kann damit
in einem Aufruf die Verlagszuordnung des gesamten Katalogs entfernen.

Das ist **keine Nachlässigkeit, sondern eine ausdrückliche Entscheidung** aus
[B-63](B-63-lehrwerk-hierarchie.md) — die Frage ist, ob sie mit dem heutigen Löschradius noch trägt.

## User Story

Als **Creator**, dessen Reihen auf geteilte Verlage zeigen, möchte ich wissen, ob ein fremdes Konto meine
Verlagszuordnung entfernen kann — damit ich mich auf den Katalog verlassen kann oder zumindest weiß,
dass ich es nicht kann.

## Ist-Stand am Code

- `Controllers/Creator/PublishersController.cs:21` gated die **ganze** Klasse nur mit
  `[Authorize(Roles = Roles.Creator)]`; `Delete` (`:111-119`) prüft kein Eigentum, weil es keines gibt.
- Der Klassenkommentar (`:11-15`) sagt das ausdrücklich zu: *„Global and child-neutral like the
  vocabulary store: naming a publisher is not authorship, so – unlike the series itself – there is no
  owner and no write restriction."*
- Der Methodenkommentar (`:106-109`) beziffert die Kosten: *„a publisher carries no content, its loss
  only costs a filter/display value."* Das stimmt **pro Reihe**, verschweigt aber, dass der Verlust alle
  Reihen aller Creator gleichzeitig trifft.
- Zum Vergleich: `TextbookSeriesController.Delete` (`:196-200`) ist eigentümergebunden **und** trägt eine
  Nutzungssperre (`409`, solange eine Übung daran in einem Plan hängt).
- Die Oberfläche formuliert es als Randnotiz: `frontend/src/vater/PublisherAdmin.tsx:153` fragt
  „… N Reihe(n) verlieren nur die Zuordnung" — ohne zu sagen, dass es fremde Reihen sein können.

## Die echte Lücke

Nicht „das Löschen ist ungeschützt" — das ist es absichtlich, und das Argument dafür ist gut: ein
Verlagsname ist keine Autorschaft, und eine Eigentümerbindung an „Klett" wäre absurd.

Die Lücke liegt zwischen der **Begründung** und der **Reichweite**: „kostet nur einen Anzeigewert" ist
für den Löschenden wahr und für alle anderen unvollständig. Es gibt keinen Weg zurück (die Zuordnung ist
danach weg, nicht wiederherstellbar) und keine Warnung, dass fremde Daten betroffen sind. Dieselbe
Freiheit hat der Vokabelspeicher — aber dort **löscht** niemand fremde Verknüpfungen mit einem Aufruf.

## Der Ist-Stand, am 2026-08-10 nachgemessen

Anders als bei der Schwester-Story [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md) hat die Messung
den Ist-Stand **bestätigt** statt ihn umzuwerfen — und das ist selbst ein Ergebnis:

- **Genau eine Beziehung** zeigt auf `Publisher`: `TextbookSeries.PublisherId`, nullable, `SetNull`
  (`PuglingDbContext.cs:239`). **Keine Cascade, es verschwindet keine Zeile.** Der Unterschied zu B-144
  ist damit grundsätzlich: dort wird zerstört, hier wird nur entkoppelt.
- **Die Zahl existiert bereits und steht schon im Dialog:** `PublisherAdmin.tsx:60-61` sagt
  *„Verlag „X" löschen? N Reihe(n) verlieren nur die Zuordnung"*. Sie kommt aus
  `PublisherResponse.SeriesCount` und wird **ohne Eigentumsfilter** gezählt
  (`PublishersController.cs:153`).

Damit ist die Lücke exakt die behauptete und keine andere: Die Zahl ist global, der Text sagt aber nicht,
**wessen** Reihen das sind.

**Zwei Begriffe, die auseinandergehalten gehören.** „Verlag" gibt es zweimal: die katalogisierte
`Publisher`-Entität und ein freies Textfeld `Textbook.Publisher` am Lehrbuch des Kindes
(`AdminEntities.cs:161`). Das Löschen berührt das Textfeld nicht. Und der eigentliche Bruch ist eine
Eigentums-Asymmetrie: **die Reihe hat einen Eigentümer, der Verlag bewusst keinen** — eine eigentümerlose
Ressource greift beim Löschen in eigentumsgebundene hinein.

## Offene Punkte

1. ~~**Gilt die B-63-Entscheidung weiter?**~~ → Entscheidung 1 (ja, unverändert).
2. ~~**Falls ja — welche Bremse?**~~ → Entscheidung 1: Variante (b), Sperre nur gegen fremde Reihen.
3. ~~**Sicherheits- oder Bedienfrage?**~~ → Entscheidung 4 (Bedienfrage, `prio` bleibt P3).

## Entscheidungen

1. **B-63 bleibt, und die Bremse ist Variante (b): Sperre nur gegen *fremde* Reihen.** Ein Verlag bleibt
   eigentümerlos — einen Namen zu vergeben ist keine Autorschaft, und eine Eigentümerbindung an „Klett"
   wäre absurd. Gelöscht werden darf er, solange nur **eigene** Reihen daran hängen; sobald eine fremde
   dabei ist, `409`. Begründung: Der Kopfkommentar von `PublisherAdmin.tsx` sagt selbst, wofür die Seite
   da ist — einen Tippfehler („Coernelsen") aufräumen. Dieser Fall hat null oder nur eigene Reihen; ein
   Verlag in echtem geteiltem Gebrauch hat fremde. Die Sperre trifft damit **genau den schädlichen Fall
   und lässt genau den gemeinten zu**, was eine Bestätigungsfrage nicht leistet — die klickt man weg.
   **Kosten:** gering, und ausdrücklich **keine Migration**: `TextbookSeries.OwnerAdultId` existiert, die
   Vorprüfung ist ein `AnyAsync(s => s.PublisherId == id && s.OwnerAdultId != ich)`. Am Löschverhalten im
   Schema ändert sich nichts — der Unterschied zu B-144, wo `Restrict` nötig wird.
2. **Die Admin-Rolle überschreibt die Sperre.** Begründung: Entscheidung 1 stellt eine Falle auf — sobald
   **zwei** Creator je eine Reihe an denselben Verlag hängen, kann ihn niemand mehr löschen, weil jeder
   die Reihe des anderen als fremd sieht. Ein vertippter Verlag, den zwei Leute erwischt haben, bliebe
   dann für immer im geteilten Katalog. Das Muster liegt fertig daneben
   (`Auth/ExercisePermissionService.cs:24,34,46` — dreimal `if (user.IsAdmin()) return true;`).
   **Kosten:** Der Admin kann weiterhin fremde Verlagszuordnungen entfernen. Das ist der bewusste Rest —
   aber mit einer Rolle, die es dafür gibt, statt als Nebenwirkung eines gewöhnlichen Creator-Kontos.
3. **Der `409` ist der ganze Mechanismus — die Oberfläche erfährt nichts vorab.** Kein aufgeteilter
   Zähler (eigene/fremde) im Vertrag, kein vorab gesperrter Knopf. Begründung: Das Feld müsste jede
   Verlagsliste bei jedem Aufruf mitschleppen, damit eine seltene Bestätigungsfrage vorab Bescheid weiß —
   das Verhältnis stimmt nicht, und es ist dieselbe Form wie in B-144 (Entscheidungen 4 und 5). Der Code
   folgt dem Registry-Muster und ist keine eigene Entscheidung: **`publisher_in_use`**, neben
   `exercise_in_use`, `vocabulary_in_use`, `subject_in_use`. **Kosten:** ein vergeblicher Klick — der
   Grund kommt erst nach dem Bestätigen.
4. **Bedienfrage, nicht Sicherheitsfrage — `prio` bleibt P3.** Begründung: Die Creator-Rolle bekommt nur,
   wer ein Konto anlegt; das ist kein anonymer Angriffsweg. Mit Entscheidung 1 ist der Schaden zudem auf
   „Zuordnung weg" begrenzt und für den Löschenden gesperrt. **Kosten:** Die Story bleibt hinter den drei
   offenen P2-Wünschen — der heutige Zustand hält also noch eine Weile.

**Ein Nebeneffekt, der gratis anfällt:** Sobald die Sperre steht, wird der **bestehende**
Bestätigungstext („N Reihe(n) verlieren nur die Zuordnung") von selbst wahr — wenn gelöscht werden darf,
gehören alle gezählten Reihen dem Löschenden. Die Story wollte diesen Text ehrlich machen; Entscheidung 1
erledigt das, ohne ihn anzufassen.

## Akzeptanzkriterien

1. `DELETE creator/publishers/{id}` antwortet `409 publisher_in_use`, sobald eine Reihe **eines anderen
   Kontos** auf den Verlag zeigt.
2. Hängen nur eigene Reihen daran — oder gar keine —, bleibt das Löschen möglich wie heute.
3. Ein Konto mit der **Admin**-Rolle löscht auch dann, wenn fremde Reihen daran hängen.
4. Die betroffenen Reihen verlieren weiterhin nur ihre Zuordnung (`SetNull`); es verschwindet keine Zeile.
5. Der Klassen- und der Methodenkommentar sagen die tatsächliche Reichweite — heute beziffern sie die
   Kosten *pro Reihe* und verschweigen, dass alle Creator gleichzeitig betroffen sind.
6. Je ein Integrationstest, vorher rot: gesperrt bei fremder Reihe, erlaubt bei eigener, erlaubt für den
   Admin.

## Schätzung

**Größe `S`** — Anker B-01 (`childId` aus dem Test-Pfad ziehen): eine Vorprüfung, ein Fehlercode, ein
Admin-Zweig nach fertigem Muster, drei Integrationstests. Kein neues Konzept.

**`wo: backend` — nicht `beides`, wie die Grill-Runde vermutet hatte.** Nachgemessen: `PublisherAdmin.tsx`
hängt an `useAction` + `StatusBanner` (`:20,76`), zeigt das `detail` eines `409` also ohne eine Zeile
Frontend. Und der bestehende Bestätigungstext wird nach Entscheidung 1 von selbst wahr, ohne angefasst zu
werden. Es bleibt **nichts** fürs Frontend übrig.

**`migration: nein`**, **`vertragsbruch: nein`** — nachgesehen: `TextbookSeries.OwnerAdultId` existiert
(`CurriculumEntities.cs:53`), am Schema und am `SetNull` ändert sich nichts. `publisher_in_use` ist ein
additiver Eintrag in `ApiErrors` (nicht in `Pugling.Contracts`), neben `exercise_in_use` und
`vocabulary_in_use`.

### Was Entscheidung 1 offenlässt: zählt eine eigentümerlose Reihe als fremd?

Entscheidung 1 gibt das Prädikat wörtlich vor:
`AnyAsync(s => s.PublisherId == id && s.OwnerAdultId != ich)`. Sie sagt aber nicht, was mit einer Reihe
**ohne** Eigentümer geschieht — und das ist kein Randfall: `OwnerAdultId` ist nullable und heißt laut
Feldkommentar *„`null` = seeded, owned by nobody"*; der Seed legt genau solche Reihen an
(`Seed.cs:1033-1034`). Der geteilte Katalog besteht aus ihnen.

**Aufgelöst durch Präzedenz, nicht durch eine neue Produktentscheidung:** `AuthAccess.IsOwnedBy`
(`:90-91`) liest eine fehlende Autorschaft ausdrücklich **fail-closed**, *„otherwise a missing claim
would wrongly unlock system exercises"*. Nach derselben Regel ist eine eigentümerlose Reihe **nicht
meine** und sperrt. Das deckt sich mit der Absicht von Entscheidung 1 („ein Verlag in echtem geteiltem
Gebrauch hat fremde Reihen") — eine geseedete Reihe *ist* geteilter Gebrauch. Entscheidung 2
(Admin-Ventil) trägt die Folge bereits.

> **Korrektur, 2026-08-10 nach dem Review.** Die erste Fassung dieses Abschnitts hieß „Das Prädikat aus
> Entscheidung 1 ist so nicht baubar" und begründete das damit, dass `NULL != 1` in SQL nicht `true` sei,
> die naive Form den Hauptfall also verfehle. **Das stimmt nicht**, und der `pugling-reviewer` hat es
> nachgemessen (EF 10 + SQLite, erzeugtes SQL verglichen): EF Core kompensiert die C#-Null-Semantik
> standardmäßig — `UseRelationalNulls` ist im Repo nirgends gesetzt —, das naive `!=` erzeugt also
> `("OwnerAdultId" <> @fid OR "OwnerAdultId" IS NULL)` und fängt die eigentümerlose Reihe sehr wohl.
>
> Die **Schlussfolgerung** trägt trotzdem: das Prädikat wird ausgeschrieben. Der Grund ist nur ein
> anderer und kleinerer — die beiden Formen unterscheiden sich in genau einem Fall, nämlich einem
> fehlenden `fid`-Claim, wo die kurze Form die Sperre ganz abschaltete. Hinter `Roles.Creator` kann das
> heute nicht eintreten; ausgeschrieben bleibt es harmlos, falls es je eintritt.
>
> Was die offene Frage **nicht** war: eine SQL-Falle. Was sie war: eine Produktentscheidung, die
> Entscheidung 1 nicht getroffen hatte.

**Risiko, das daraus folgt:** Der Testfall „erlaubt bei eigener Reihe" muss seine Reihe **über den
Endpunkt** anlegen, nicht über den `DbContext` — sonst fehlt der `OwnerAdultId`, die Reihe gilt als fremd,
und der Test ist aus dem falschen Grund rot.

**Und ein Testfall, der weniger belegt als er aussieht:** „gesperrt bei eigentümerloser Reihe" pinnt die
Produktentscheidung (eigentümerlos = fremd), die man kippen könnte, ohne dass ein anderer Test rot wird.
Er belegt **nicht**, dass die ausgeschriebene Form nötig war — vor dem Fix war er rot, weil es gar keine
Sperre gab. Das gehört in seine Doku, sonst liest ihn der Nächste als Beleg für die Korrektur oben.

**Angriffsplan** (Backend zuerst, hier ausschließlich Backend):

1. `ApiErrors` — `PublisherInUse` additiv ergänzen (`publisher_in_use`, 409).
2. `PublishersController.Delete` — Vorprüfung nach Guard-Clause-Muster, mit dem Prädikat oben; davor
   `if (User.IsAdmin())` überspringen (Muster: `ExercisePermissionService.cs:24,34,46`).
3. Klassen- und Methodenkommentar auf die tatsächliche Reichweite ziehen (Kriterium 5) — der heutige
   beziffert die Kosten *pro Reihe*.
4. Tests.

**Testweg:** `backend/Pugling.Api.Tests/PublishersTests.cs` (existiert, wurde in B-136 erweitert) bekommt
die drei Fälle aus Kriterium 6 — gesperrt bei fremder Reihe, erlaubt bei eigener, erlaubt für den Admin —
und einen vierten, den die Messung oben erzwingt: **gesperrt bei eigentümerloser Reihe**. Rote Probe vor
dem Fix, mit Zahl.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Code nachgeprüft
  (`PublishersController.cs:21,106-119`). Als **`Frage`** eingestuft und nicht autonom entschieden: der
  Zustand ist eine dokumentierte B-63-Entscheidung, und sie zu revidieren ist eine Wertentscheidung.
  `entgangen_bei` bleibt **leer** — es ist kein durchgekommener Defekt, sondern eine bewusste
  Entscheidung, deren Begründung sich als zu eng erwiesen hat.
- **2026-08-10** — **gegrillt** im Dialog mit dem Nutzer (vier Entscheidungen). Die Messung hat den
  Ist-Stand diesmal **bestätigt** statt ihn umzuwerfen: genau eine Beziehung, `SetNull`, keine Cascade —
  der Unterschied zur Schwester-Story [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md) ist damit
  grundsätzlich, und ihre Entscheidungen sind hier **kein** Präzedenzfall. Die Story endet nicht in
  `verworfen`: der Zustand ist real, aber die Bremse fällt schlanker aus als befürchtet (keine Migration).
  Voraussichtlich `S`, `wo: beides`, `migration: nein` — zu bestätigen beim Schätzen.
- **2026-08-10** — **geschätzt** (`S`, `backend`, `migration: nein`, `vertragsbruch: nein`). Zwei
  Korrekturen an dem, was die Grill-Runde angenommen hatte, beide gemessen: `wo` ist **backend**, nicht
  `beides` (`PublisherAdmin.tsx` zeigt den `409` bereits über `useAction`+`StatusBanner`), und
  Entscheidung 1 lässt offen, ob eine **eigentümerlose** Reihe als fremd zählt — `OwnerAdultId` ist
  nullable und heißt „geseedet, gehört niemandem"; der geteilte Katalog besteht aus solchen Zeilen.
  Aufgelöst über die bestehende Präzedenz `AuthAccess.IsOwnedBy` (fail-closed), also ohne neue
  Produktentscheidung. Ein vierter Testfall kommt dadurch dazu.
  **Die Begründung dieses Punktes war zunächst falsch** — sie behauptete eine SQL-NULL-Falle, die EF Core
  gar nicht durchlässt. Korrigiert nach dem Review; siehe den Kasten im Schätzabschnitt.
- **2026-08-10** — **gebaut** im Nachtlauf (Sprint 3). Rote Probe vorher: **2 von 4 rot**, beide mit
  `Expected: Conflict / Actual: NoContent`. Die anderen zwei waren vorher grün — `…MitNurEigenenReihen…`
  als Gegenprobe gegen Übersperren, und `Admin_LoeschtAuchMitFremderReihe` **trivial**, weil ohne Sperre
  nichts blockierte; das steht jetzt im Test selbst, damit er nicht mehr aussieht, als belege er das
  Ventil für sich allein. Der beim Schätzen gefundene vierte Fall (eigentümerlose Reihe) war rot und ist
  grün. Danach: Backend **813/813**, die bestehenden `PublishersTests` halten mit — auch der Fall, der
  einen Verlag mit **eigener** Reihe löscht.
- **2026-08-10** — **abgenommen** (Commit `f29ee1c`, Rollengang-Nachtrag `d4d3595`).
  Belegt: Backend **813/813**, Vitest **204/204**, Playwright **33/33**, `pugling-reviewer`
  und `frontend-reviewer` gelaufen, ihre Funde behoben oder als eigene Story abgelegt.
  **Rollengang teils im echten Browser** (Anmeldung als Papa, Vater-Web, Katalogseite),
  teils per dokumentiertem Ersatz: Alle Löschpfade hängen an `confirmAction`, und ein
  `window.confirm` blockiert die Chrome-Extension — ein injizierter Ersatz greift nicht, weil
  er in einer isolierten Welt läuft. Dafür stehen die Playwright-Spec (echter Browser, echter
  Dialog) und eine Live-Probe gegen die laufende API. Protokoll:
  [pm-sitzung-2026-08-10.md](../pm-sitzung-2026-08-10.md) → Nachtlauf, Sprint 3.
- **2026-08-11** — **Nachtrag aus dem Code-Review** (`/code-review` über `1867cfd..HEAD`), vier Funde an
  dieser Story, alle behoben:
  1. Die Sperre war für den Vater ein **stummer Dauerzustand**: Der Seed hängt die eigentümerlose
     „Green Line 1" an „Klett", das Ventil `Adult.IsAdmin` setzt aber weder ein Endpunkt noch ein DTO.
     Der Verlag ist damit **absichtlich** unlöschbar (Löschen zöge ihn dem geteilten Katalog weg) — das
     stand nur nirgends, und die Doku behauptete mit dem Admin-Ventil einen Ausweg, den es über das
     Produkt nicht gibt. Jetzt benannt statt behauptet.
  2. `PublisherResponse.ForeignSeriesCount` **neu**: die Teilmenge der Reihen, die dem Aufrufer *nicht*
     gehören (fremd **oder** herrenlos) — also genau das, was die Sperre entscheidet. `SeriesCount` konnte
     das nie tragen, es zählt fremde Zeilen mit und sagt nichts über Eigentum. Damit zeigt die Oberfläche
     die Sperre **vorher**, statt den Vater hineinlaufen zu lassen.
  3. Der Bestätigungsdialog in `PublisherAdmin.tsx` versprach weiter den Zustand *vor* dieser Story
     („N Reihe(n) verlieren nur die Zuordnung, keine Sperre nötig") — der Vater bestätigte und bekam
     einen 409. Er nennt jetzt nur die **eigenen** Reihen; der Knopf ist bei `foreignSeriesCount > 0`
     gesperrt und sagt im `title`, warum. Das Gegenstück in `CatalogAdmin.tsx` war im selben Nachtlauf
     nachgezogen worden, dieses hier war übersehen.
  4. Meldungstext (englisch **und** deutsch) nannte „Reihe eines anderen Kontos" — auf einer geseedeten
     Datenbank der **einzige Fall, der nicht zutrifft**. Beide Fälle stehen jetzt drin; `errorMessage.test.ts`
     hält den zweiten fest.
  Dazu eine Transaktion um Prüfung und Löschen (vorher zwei Runden über zwei Verbindungen; eine dazwischen
  angelegte fremde Reihe wäre still per `SetNull` entkoppelt worden — anders als beim Fach fängt das hier
  **kein** `Restrict`, weil `SetNull` gerade die eigenen Reihen retten soll).
  Belegt: Backend **814/814**, Vitest **204/204**, `tsc -b` sauber.
- **2026-08-11** — **nachgeschaut** (`nachgeschaut: 2026-08-11`). Die drei Funde oben saßen in Arbeit, die
  seit dem 2026-08-10 `abgenommen` war — sie zählen damit als **durchgekommen** und tragen ihre eigene
  Story: [B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) mit
  `entgangen_bei: [B-127]`. Die Verlaufszeile oben allein hätte sie aus der Messung fallen lassen
  (README → „Warum der Defekt eine eigene Story braucht"); nachgeholt statt stehengelassen.
