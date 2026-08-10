---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Reihe bearbeiten, Lehrwerk-Reihe ändern, ClearSubject]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-63 (manueller Chrome-Test, 2026-08-07)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-123 · Lehrwerk-Reihe im Vater-Web bearbeiten

Beim Chrome-Test von B-63 (Verlags-Vokabular) aufgefallen: `VaterLehrwerke.tsx` bietet nur „Reihe
hinzufügen", kein Formular zum Ändern einer schon angelegten Reihe. Ein falsch zugeordneter Verlag, ein
Tippfehler in Name/Fach/Sprachen oder das nachträgliche Entfernen eines Verlags lässt sich über die
Oberfläche gar nicht korrigieren – nur über einen direkten API-Aufruf. Der Backend-Endpunkt dafür
(`PATCH creator/textbook-series/{id}` mit `UpdateTextbookSeriesDto`, inklusive des neuen
`ClearPublisherId`-Schalters) existiert bereits, hat aber keinen UI-Aufrufer.

## User Story

Als Vater (Creator) möchte ich eine bestehende Lehrwerk-Reihe bearbeiten können (Name, Verlag, Fach,
Schulart, Sprachen, Notiz – inklusive „Verlag entfernen"), damit sich ein Tippfehler oder ein falsch
zugeordneter Verlag korrigieren lässt, ohne die API direkt anzusprechen.

## Ist-Stand am Code

Alle vier Schichten unterhalb der Oberfläche sind bereits vollständig, nur der UI-Aufrufer fehlt:

- **Backend-Endpunkt vorhanden und vollständig**: `PATCH creator/textbook-series/{seriesId}`
  (`backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:154-184`). Prüft Eigentümerschaft
  (`163-164`, `NotOwner` sonst), validiert `SubjectId`/`PublisherId` über `ValidateReferencesAsync`
  (`165`), wendet PATCH-Semantik korrekt an (`167-180`) und behandelt den neuen `ClearPublisherId`-Schalter
  (`174`: `if (dto.ClearPublisherId) series.PublisherId = null;`, **nach** dem Wert angewendet – „leeren"
  gewinnt, wie es die Root-`CLAUDE.md`-Regel zur PATCH-Semantik verlangt).
- **Vertrag vorhanden**: `UpdateTextbookSeriesDto` mit `ClearPublisherId`
  (`backend/Pugling.Contracts/Creator/TextbookSeriesDtos.cs:24-30`), dokumentiert und additiv (kein
  Vertragsbruch).
- **JS/TS-Client vorhanden, aber ungenutzt**: `api.updateTextbookSeries` ist definiert
  (`frontend/src/lib/api.ts:749-750`). Eine Volltextsuche über `frontend/src` findet **genau eine**
  Fundstelle – die Definition selbst; kein einziger Aufrufer.
- **C#-Client (KI-Agenten) vorhanden**: `CreatorApi.UpdateSeriesAsync`
  (`backend/Pugling.Client/CreatorApi.cs:89`).
- **Frontend-UI fehlt vollständig**: `VaterLehrwerke.tsx` hat `SeriesRow` (`113-178`), das Name, Verlag,
  Fach, Schulart, Band nur **liest** und neben „Units“ (`151-154`) ausschließlich einen bedingten
  „Löschen“-Knopf zeigt (`156-160`, gated auf `series.isOwn`) – kein „Bearbeiten“. `NewSeries` (`386-514`)
  kann nur **anlegen**. Zum Vergleich: Auf Unit-Ebene existiert das gleiche Muster bereits gebaut –
  `UnitForm` (`261-383`) ist bewusst **ein** Formular für Anlegen *und* Ändern (Kommentar `257-260`), und
  `UnitPanel` (`181-253`) schaltet darüber per Bearbeiten-Toggle (`220-227`) um. Genau dieses Muster fehlt
  eine Ebene höher, bei der Reihe selbst.
- Kein Test deckt eine Bearbeitung der Reihe **über die UI** ab; `PatchSemanticsTests`/
  `PatchClearFieldTests` prüfen `ClearPublisherId` reflexiv nur gegen den Backend-Endpunkt.

Drei Befunde der Grill-Runde vom 2026-08-09, die diesen Ist-Stand korrigieren bzw. ergänzen:

- **Das Fach ist nicht entfernbar.** Die Textfelder der Reihe sind alle leerbar, weil `Trimmed("")` zu
  `null` wird (`TextbookSeriesController.cs:229`) – das gilt für `SubjectName`, beide Sprachen und `Notes`.
  Die beiden `int?`-Felder können das nicht: `PublisherId` hat dafür `ClearPublisherId` bekommen,
  `SubjectId` nicht (`:190` – `if (dto.SubjectId.HasValue)`, `null` heißt „unverändert"). Ein einmal
  gesetztes Fach lässt sich über die API nicht mehr ablösen. **Damit ist die Story keine reine
  Frontend-Lücke** (siehe Entscheidung 1).
- **Umbenennen ist gegen Dubletten geschützt** (seit [B-124](B-124-umbenennen-umgeht-die-eindeutigkeit.md)):
  `:181-183` weist einen Namen ab, dessen Slug eine andere Reihe schon trägt (`409`,
  `DuplicateTextbookSeries`).
- **Der Slug bleibt beim Umbenennen stehen** (`:154`, `series.Name = name` ohne Neuableitung) und ist in
  der Zeile sichtbar (`VaterLehrwerke.tsx:144`). Solange niemand über die Oberfläche umbenennen kann, hat
  das niemand gesehen; diese Story liefert es aus (siehe Entscheidung 5).

## Die echte Lücke

Der Kern ist das fehlende React-Formular in `VaterLehrwerke.tsx`, das den längst vorhandenen
`api.updateTextbookSeries` aufruft – Endpunkt, Vertrag und beide Clients stehen bereits.

**Nicht** ganz frontend-rein, anders als bis zum 2026-08-09 hier behauptet: „bearbeiten" schließt
„zurücknehmen" ein, und das Fach lässt sich mangels `ClearSubjectId` nicht zurücknehmen. Die Lücke ist
also das Formular **plus** ein additives Vertragsfeld mit seiner Controller-Zeile. Entsprechend:
`wo: beides`, `migration: nein`, `vertragsbruch: nein` (additiv) – gesetzt wird das in `geschaetzt`.

## Offene Punkte

Alle drei sind in der Grill-Runde vom 2026-08-09 entschieden (→ Entscheidungen 3, 2, 4); zwei weitere
Punkte kamen dort dazu (→ Entscheidungen 1 und 5).

1. ~~**Wo sitzt das Formular?** Inline in `SeriesRow` nach demselben Muster wie `UnitPanel`/`UnitForm`
   (Bearbeiten-Toggle, ein gemeinsames Formular für Anlegen und Ändern) oder ein separat ausklappbarer
   Bereich?~~ → Entscheidung 3.
2. ~~**Wie wird „Verlag entfernen“ von „Verlag unverändert lassen“ unterschieden?** Das Verlag-`<select>`
   in `NewSeries` (`448-452`) kennt nur „– keine Angabe –“ und die Liste der Verlage; im Edit-Formular
   müsste „– keine Angabe –“ **nicht** automatisch `ClearPublisherId` auslösen (sonst löscht ein Formular,
   das aus Versehen mit leerer Auswahl abgeschickt wird, einen bestehenden Verlag).~~ → Entscheidung 2.
   Die Begründung trug nicht: bei einem **vorausgefüllten** Formular ist „leer“ nie der Ausgangszustand,
   also auch nicht versehentlich abschickbar.
3. ~~**Umfang der Story**: nur die Reihen-Metadaten editierbar machen, oder zusätzlich die
   Inline-Verlag-Neuanlage aus `NewSeries` (`496-510`) im Edit-Formular duplizieren?~~ → Entscheidung 4.

## Entscheidungen

Gegrillt am 2026-08-09 im Dialog (`/backlog B-123 grillen`, Skills `grilling` + `domain-modeling`).

1. **`ClearSubjectId` kommt additiv dazu – das Fach ist entfernbar.** Neues Feld im
   `UpdateTextbookSeriesDto` plus eine Controller-Zeile nach dem Muster von `ClearPublisherId` (**nach**
   dem Wert angewendet, damit „leeren“ gewinnt). *Begründung*: „bearbeiten“ heißt auch „zurücknehmen“;
   ohne den Schalter hätte das Formular ein Feld, in dem „– keine Angabe –“ zu „Gespeichert.“ führt und
   der alte Wert stehen bleibt – genau der Fall, gegen den die PATCH-Regel der Root-`CLAUDE.md`
   geschrieben ist. *Kosten*: die Story ist nicht mehr frontend-rein (`wo: beides`); `PatchSemanticsTests`
   wird rot, bis der neue Schalter einen Fall in seiner Tabelle hat; das OpenAPI-Dokument muss neu erzeugt
   werden. Kein Vertragsbruch (additiv), keine Migration.
2. **„Leeren“ entsteht aus dem Dirty-Vergleich, nicht aus einer dritten Select-Option.** Der Select trägt
   dieselben zwei Optionen wie beim Anlegen und ist mit dem aktuellen Wert vorausgefüllt; beim Speichern
   wird gegen die **geladenen** Werte verglichen: unverändert → Feld gar nicht senden, Wert → „keine
   Angabe“ → `Clear…`-Schalter, Wert → anderer Wert → neue Id. Gilt für Verlag **und** Fach. *Begründung*:
   eine dritte Option gäbe „– keine Angabe –“ beim Anlegen und beim Ändern zwei verschiedene Bedeutungen,
   und die Sicherheit kommt ohnehin nicht aus der Optionsliste, sondern daraus, dass unveränderte Felder
   nicht mitgeschickt werden (AK 3). *Kosten*: das Formular führt die Ausgangswerte als zweite Referenz
   neben dem Formularstand mit – Sorgfalt, die `NewSeries` nicht braucht.
3. **Eigenes `SeriesForm` nur fürs Ändern, lokal in `SeriesRow` ausgeklappt.** Die Zustandsführung ist die
   von `UnitPanel` (`:183`, lokales `editing`), **nicht** der seiten-globale Akkordeon-Zustand `open`
   (`:48`, `:92`) – der gehört den Units und zeigt immer nur eine Zeile. `NewSeries` bleibt unangetastet.
   *Begründung*: das Vorbild `UnitForm` unterscheidet Anlegen/Ändern in zwei Zeilen, `NewSeries` hätte
   deutlich mehr Nur-beim-Anlegen-Ballast (Überschrift, Idempotenz-Hinweis `:490-493`, Verlag-Inline-Anlage
   `:496-510`) und würde zu einem Formular voller `series ? … : …`. *Kosten*: die sieben Feld-Definitionen
   stehen zweimal in derselben Datei; laufen sie auseinander, fällt es nicht auf.
4. **Kein Verlag-Anlegen im Edit-Formular.** *Begründung*: der Umweg funktioniert nachweislich – ein im
   Anlegen-Abschnitt erzeugter Verlag löst `publishers.reload` (`:409`, `:106`) aus, und weil die
   Verlagsliste auf Seitenebene liegt, erscheint er sofort im geöffneten Edit-Formular, ohne dass
   `list.reload` läuft und den Formularstand verwirft. *Kosten*: ein Sprung ans Seitenende in einen
   Abschnitt namens „Lehrwerk hinzufügen“, obwohl gerade nichts hinzugefügt wird. Dass `PublisherAdmin`
   Verlage nur korrigieren und löschen kann, aber nicht anlegen, bleibt so – das ist eine eigene Fläche.
5. **Hinweis zum eingefrorenen Slug hier, der Doppelname-Fall als eigene Story.** Das Namensfeld bekommt
   einen Satz, dass der Kurzname bleibt, wie er beim Anlegen entstand. Der am Code belegte Folgefehler –
   nach einer Umbenennung können zwei Reihen denselben Anzeigenamen tragen, weil die B-124-Vorprüfung
   Slug gegen Slug vergleicht (`:181`) und nur `Slug` unique indiziert ist
   (`PuglingDbContext.cs:220`) – wandert nach **[B-133](B-133-zwei-reihen-ein-anzeigename.md)**.
   *Begründung*: B-123s Ziel ist ohne ihn erfüllt, und seine Lösung braucht eine Produktentscheidung
   (Namensgleichheit verbieten oder den Slug mitwandern lassen – letzteres bricht die Idempotenz des
   Anlegens). *Kosten*: eine zusätzliche Story; der Fehler bleibt bis dahin bestehen – sichtbar notiert
   statt still.
6. **Der Knopf heißt „Reihe bearbeiten“.** *Begründung*: er steht direkt neben „Units“, und „Bearbeiten“
   allein (so heißt es eine Ebene tiefer, `:225`) ließe offen, welcher der beiden Knöpfe den Inhalt
   ändert. *Kosten*: die Seite benutzt weiterhin drei Wörter für eine Sache (Überschrift „Lehrwerk“
   `:441`, Feld „Reihe“ `:444`, Einleitung „Buchreihe“ `:55`) – das ist die Frage von
   [B-64](B-64-textbook-vs-series.md) und wird hier bewusst **nicht** beantwortet, weil eine Antwort auf
   dieser Ebene B-64 festlegen würde, bevor es gegrillt ist.

## Akzeptanzkriterien

1. In `VaterLehrwerke.tsx` lässt sich eine eigene Reihe (`series.isOwn`) über einen Knopf **„Reihe
   bearbeiten“** in ein Formular öffnen, das die aktuellen Werte vorausfüllt. Der Units-Bereich derselben
   Zeile bleibt davon unberührt (eigener, lokaler Zustand).
2. Speichern ruft `api.updateTextbookSeries` mit **nur den geänderten** Feldern auf; die Reihen-Liste
   aktualisiert sich danach (`list.reload`) und `PublisherAdmin`/`publishers.reload` bleiben unberührt.
3. Verlag **und** Fach lassen sich ändern *und* entfernen: wechselt ein gesetzter Wert auf „– keine
   Angabe –“, geht `ClearPublisherId: true` bzw. `ClearSubjectId: true` mit. Ein Speichern **ohne**
   Änderung an diesen Feldern schickt weder Wert noch Schalter – der Vergleich läuft gegen den
   Ladezustand, nicht gegen „leer“.
4. `UpdateTextbookSeriesDto` trägt `ClearSubjectId`; der Controller wendet es **nach** `SubjectId` an, und
   `PatchSemanticsTests` hat den zugehörigen Fall (das Tor wird durch den neuen Schalter zunächst rot).
5. Das Namensfeld erklärt, dass der Kurzname (Slug) beim Umbenennen bestehen bleibt.
6. Eine fremde Reihe (`!series.isOwn`) zeigt weiterhin keinen Bearbeiten-Knopf (Server antwortet ohnehin
   mit `not_owner`, aber die UI zeigt das gar nicht erst an – bestehendes Muster von `156`).
7. Ein Komponententest oder E2E-Test fährt den Bearbeiten-Weg mindestens einmal durch (Formular öffnen →
   Feld ändern → speichern → geänderter Wert erscheint in der Liste) und deckt dabei den Entfernen-Fall
   mindestens eines der beiden Auswahlfelder ab.

## Schätzung

**M** (`wo: beides`, `migration: nein`, `vertragsbruch: nein`). Nachgesehen, nicht vermutet: Es gibt keine
Schemaänderung – `ClearSubjectId` ist ein reines DTO-Feld, `TextbookSeries` bleibt unangetastet
(`Data/PuglingDbContext.cs:218-222`). Und der Vertrag wächst nur **additiv**: ein weiterer
`bool … = false`-Parameter hinter dem bestehenden `ClearPublisherId = false`
(`Pugling.Contracts/Creator/TextbookSeriesDtos.cs:30-32`) bricht weder JSON-Aufrufer noch die
positionellen C#-Aufrufer in `Pugling.Client`.

Gegen die Anker: mehr als **S** („`childId` aus dem Test-Pfad ziehen", B-01), weil eine neue
Formular-Komponente mit eigener Diff-Logik entsteht und Backend, Vertragsdokument und zwei Testebenen
mitziehen; vergleichbar mit **M** (vokabel-basierter Batch-Pfad im `MediaSelector`, B-03).

### Risiken

1. **Fach und Fachname sind zwei Felder für eine Sache** – der wahrscheinlichste stille Fehler dieser
   Story. `SeriesRow` zeigt das Fach über `subjectId` **mit Rückfall auf `subjectName`**
   (`VaterLehrwerke.tsx:129-131`), und `NewSeries` schickt beim Anlegen beide (`:425-427`). Wer beim
   Bearbeiten nur `ClearSubjectId` sendet, sieht in der Liste weiterhin „Englisch" – aus dem
   stehengebliebenen Namen. Das Formular muss das Fach als **Paar** behandeln: Wechsel → `subjectId` +
   `subjectName`, Entfernen → `clearSubjectId: true` + `subjectName: ""` (leer räumt ab, weil
   `Trimmed("")` zu `null` wird, `TextbookSeriesController.cs:229`).
2. **`PatchSemanticsTests` wird rot, sobald das DTO-Feld existiert** – `Jeder_ClearSchalter_Ist_Belegt`
   (`:426-443`) sammelt reflexiv alle `bool Clear…`-Parameter der Update-DTOs und verlangt für jeden einen
   Fall. Beabsichtigt; muss in derselben Änderung geschlossen werden, nicht in einem Folgeschritt.
3. **Das Vertragsdokument gehört in den Commit.** `ContractDocumentTests` schreibt `docs/openapi/v1.json`
   bei jedem Lauf, und CI wird rot, wenn die Änderung nicht mit eingecheckt ist. Daraus wird
   `frontend/src/lib/contract.ts` erzeugt (gitignored, läuft bei `predev`/`prebuild`).
4. **Das Formular muss auf Erfolg schließen** (Muster `UnitForm`/`onDone`, `:234`). Bleibt es nach
   `list.reload` offen, rechnet der Dirty-Vergleich aus Entscheidung 2 gegen einen veralteten Ladezustand
   – und das ist genau die Annahme, auf der die Sicherheit dieser Entscheidung ruht.
5. `disabled={busy}` auf jedem mutierenden Knopf bleibt Pflicht (`frontend/CLAUDE.md`), auch wenn
   `useAction` den Wiedereintritt selbst sperrt.

### Angriffsplan (Backend zuerst)

1. `UpdateTextbookSeriesDto` um `bool ClearSubjectId = false` ergänzen, `/// <summary>` auf Englisch
   nachziehen (`TextbookSeriesDtos.cs:24-32`).
2. `TextbookSeriesController.Update`: `if (dto.ClearSubjectId) series.SubjectId = null;` **nach** der
   Wertzuweisung `:190` – „leeren" gewinnt, wie bei `ClearPublisherId` (`:188`).
3. `PatchSemanticsTests` Zeile `:148` auf beide Schalter erweitern:
   `[new("clearPublisherId", "publisherId"), new("clearSubjectId", "subjectId")]`.
4. `dotnet test` laufen lassen; das neu geschriebene `docs/openapi/v1.json` mitnehmen.
5. Frontend: `npm run gen:contract`, dann die Diff-Bildung als **reine Funktion** herausziehen
   (Ladezustand + Formularstand → `UpdateTextbookSeriesDto`) – nur so ist die Zusicherung „unverändertes
   Feld wird nicht gesendet" prüfbar, ohne einen Bildschirm mit gefälschtem `fetch` nachzubauen.
6. `SeriesForm` bauen (sieben Felder, Vorbild-Layout `NewSeries:439-485`), Slug-Hinweis am Namensfeld als
   `FieldLabel` + `HelpTopic` in `lib/fieldHelp.ts` (nicht als freier Text am Feld – Konvention).
7. In `SeriesRow` verdrahten: lokaler `editing`-Zustand, Knopf „Reihe bearbeiten" nur bei `series.isOwn`
   (`:156`), `publishers` als Prop von der Seitenebene durchreichen.

### Testweg

| Ebene | Konkret |
| --- | --- |
| Backend, reflexiv | `PatchSemanticsTests.Jeder_ClearSchalter_Ist_Belegt` (rot bis Schritt 3) und `Clear_Schalter_Leert_Und_Gewinnt` mit der neuen Schalter-Zeile |
| Vertrag | `ContractDocumentTests.Vertragsdokument_WirdGeschrieben_UndIstZwischenZweiHostsByteGleich` |
| Frontend, Einheit | neuer Vitest neben `VaterLehrwerke.tsx` auf die Diff-Funktion: unverändert → nicht gesendet · Wert → „keine Angabe" → `clear…` · Fach als Paar (Risiko 1) |
| Frontend, Durchstich **und Rollengang** | neue Spec `frontend/e2e/lehrwerk-bearbeiten.spec.ts`: Reihe anlegen → „Reihe bearbeiten" → Name ändern **und** Verlag entfernen → speichern → beides in der Liste. **Eigene Datei**, nicht an `lehrwerke.spec.ts` angehängt – die trägt den Creator→Kind-Durchstich, und eine Spec bricht beim ersten Rot ab (B-109) |

`/smoke-test` ist hier nicht der richtige Beleg: der Weg ist ein UI-Weg, und die E2E fährt ihn im echten
Browser gegen einen echten Server – das *ist* der Rollengang (README, „Eine E2E, die den Weg fährt").

Vor der Abnahme beide Brillen, in dieser Reihenfolge: `pugling-reviewer`, dann `frontend-reviewer`.

## Verlauf

- **2026-08-07** — angelegt (Quelle: manueller Chrome-Test von B-63).
- **2026-08-07** — ausformuliert: Ist-Stand gegen den Code belegt (Backend/Vertrag/beide Clients
  vollständig, reine Frontend-Lücke), drei offene Punkte für die Grill-Runde formuliert.
- **2026-08-09** — gegrillt (Dialog, `grilling` + `domain-modeling`): sechs Entscheidungen, die drei
  notierten Punkte geschlossen. Der tragende Befund widerlegt den Ist-Stand vom 2026-08-07: „reine
  Frontend-Lücke“ stimmt nicht, weil `SubjectId` mangels `ClearSubjectId` nicht zurücknehmbar ist
  (`TextbookSeriesController.cs:190` gegen `:229`) — die Story wird dadurch `wo: beides`. Zweiter Befund:
  der eingefrorene Slug wird durch diese Story erstmals über die Oberfläche erreichbar; der daraus
  folgende Doppelname-Fall ist als [B-133](B-133-zwei-reihen-ein-anzeigename.md) abgespalten.
- **2026-08-09** — geschätzt: **M**, `wo: beides`. `migration`/`vertragsbruch` nachgesehen statt vermutet
  (kein Schema-Bezug in `PuglingDbContext.cs:218-222`; additiver Default-Parameter im DTO). Angriffsplan
  in sieben Schritten, Backend zuerst. Testweg benannt: `PatchSemanticsTests` (zwei Fälle),
  `ContractDocumentTests`, ein Vitest auf die herausgezogene Diff-Funktion und die neue Spec
  `e2e/lehrwerk-bearbeiten.spec.ts`, die zugleich den Rollengang trägt. Beim Schätzen gefunden: das Fach
  hängt an **zwei** Feldern (`subjectId` mit Rückfall auf `subjectName`, `VaterLehrwerke.tsx:129-131`) —
  als Risiko 1 aufgenommen, weil „Fach entfernen" sonst sichtbar folgenlos bliebe.
- **2026-08-10** — gebaut, alle sieben Schritte des Angriffsplans. Backend zuerst: `ClearSubjectId`
  additiv im DTO, im Controller **nach** dem Wert angewendet, `PatchSemanticsTests` um den zweiten
  Schalter erweitert — dabei musste der Fixture-Helfer `NeueReiheMitVerlagAsync` ein Fach mitanlegen, weil
  der reflexive Test zu Recht verweigert, wenn das zu leerende Feld beim Anlegen leer war
  (`Clear_Schalter_Leert_Und_Gewinnt`: „a switch on a field that was empty to begin with proves nothing").
  Frontend: Diff-Bildung als reine Funktion `seriesPatch.ts` (11 Vitest-Fälle), Komponente `SeriesForm`
  mit lokalem `editing`-Zustand in `SeriesRow`, Slug-Erklärung als `HelpTopic seriesName` plus dem echten
  Kurznamen als `.sub`-Zeile, Knopf „Reihe bearbeiten".
  **Der Rollengang hat einen echten Fehler gefunden**, den keine der drei Testebenen sehen konnte: Ich
  schloss das Formular bei Erfolg — und vernichtete damit den `StatusBanner`, der „Gespeichert." zeigt.
  Die E2E lief auf eine nie erscheinende Bestätigung. Behoben, indem das Formular offen bleibt und der
  **Ladezustand aus der Server-Antwort** nachgezogen wird; beides gehört zusammen, weil ein offenes
  Formular mit veraltetem Bezugspunkt den nächsten Diff falsch rechnet (Risiko 4 der Schätzung, nur
  andersherum als vermutet). Zweiter Fund derselben Spec: bei offenem Formular trägt auch dessen Zeile
  den Namen — die Spec klappt darum zu, bevor sie die Liste prüft.
  Verifikation: Backend **789/789**, Vitest **188/188** (11 neu), E2E **31/31** (2 neu),
  `tsc -b` sauber. Reviewer stehen noch aus, darum `in-arbeit` und nicht `abgenommen`.
- **2026-08-10** — beide Reviewer gelaufen, **sieben Funde**, sechs behoben, einer als eigene Story.
  **Der schwerste kam vom Backend und betraf Entscheidung 1 selbst:** die Paar-Regel für
  `(SubjectId, SubjectName)` lebte ausschließlich im Frontend. Für genau dasselbe Paar räumen
  `CreatorProfilesController:146` und `TextbooksController:111` beides in *einer* Zeile — und beide
  Schalter heißen `ClearSubject`. Jeder andere Aufrufer (`Pugling.Client`, der Creator-Agent,
  `docs/REST`) hätte mit `clearSubjectId: true` eine Reihe hinterlassen, die weiter „Englisch" anzeigt.
  Umgesetzt als **serverseitige** Variante samt Umbenennung auf `ClearSubject` — eine **Abweichung von
  Entscheidung 1**, die dort „`ClearSubjectId`" beim Namen nennt; die Substanz bleibt, und das Feld hatte
  noch keinen Nutzer. Der tragende Beleg fiel beim Nachsehen: `publisherName` ist im Projektions-Join
  berechnet, `subjectName` eine **gespeicherte** Spalte — darum braucht das Fach ein Paar und der Verlag
  keines. Dazu ein Einzelfall in `PatchClearFieldTests` (reflexiv ist die Paar-Aussage nicht
  ausdrückbar), der doppelte `<summary>` am Helfer entfernt, Helfer umbenannt, und die `<para>`/`<b>`-Tags
  aus der Vertragsdoku genommen: sie sickerten als rohes Markup ins OpenAPI-Dokument und schluckten
  Leerzeichen — bei API-First ist das Dokument das Produkt.
  **Vom Frontend-Reviewer, alles gemessen:** `**Kurzname**` erschien dem Nutzer wörtlich mit Sternchen
  (`InfoHint` rendert reinen Text, und `seriesName` war der einzige von 48 Hilfetexten mit Markdown); die
  Slug-Erklärung stand **zweimal** und die E2E prüfte die Kopie neben dem Feld statt das Popover — genau
  darum war der Sternchen-Fehler durchgerutscht; die Zeilen-Knöpfe hießen in jeder Zeile gleich, während
  beide Nachbarn (`PublisherAdmin`, `UnitPanel`) ein `aria-label` mit dem Namen tragen. Alles behoben.
  Dazu unkommentiert gewesen: die Schulart ist der **dritte** Mechanismus (Sentinel `"None"` statt
  Schalter oder leerem String) und ihr Draht-Wert war nirgends geprüft — jetzt mit Kommentar und Fall.
  **Die Lehre steht jetzt im Kopf von `seriesPatch.ts`:** die 11 Vitest-Fälle blieben beim
  Schalter-Umbenennen **grün**. Sie pinnen den Rumpf, den das Frontend *baut*, nicht den, den der Server
  *annimmt*; gegriffen haben `tsc -b` und die E2E.
  Als eigene Story abgespalten: [B-137](B-137-freitext-fach-unerreichbar.md) — ein Fachname ohne Fach-Id
  (entsteht, weil `SubjectsController.Delete` nichts prüft und der FK `SetNull` ist) ist in der Zeile
  sichtbar, im Formular unsichtbar und über die Oberfläche nicht zu entfernen. B-123s Akzeptanzkriterien
  deckten den Zustand nicht ab, und die Lösung braucht eine Produktentscheidung. Dreizehnte Fundstelle
  für [B-134](B-134-bedingte-live-regionen.md) nachgetragen.
  Verifikation nach den Korrekturen: Backend **790/790**, Vitest **189/189**, E2E **31/31**, `tsc -b`
  sauber, Vertragsdokument und `contract.ts` neu erzeugt.
- **2026-08-10** — zweite Reviewer-Runde über die Korrekturen. **Der Backend-Reviewer hat die rote Probe
  nachgemessen, die ich versäumt hatte**: Wächter im Scratchpad auf `SubjectId = null` verkürzt →
  `Reihe_ClearSubject_nimmt_den_Fachnamen_mit` **1 von 25 rot** („Expected: Null, Actual: String"). Meine
  Sorge, die Vorbedingung greife nicht, war unbegründet — `Create` speichert den Namen, `Project` liest
  die Spalte. Nebenbei belegt: die reflexive Theorie blieb grün, die Paar-Aussage ist also tatsächlich
  nur im Einzelfall ausdrückbar.
  Behoben: `subjectName` wird im Testrumpf mitgeschickt, damit „leeren gewinnt" auch gegen den **Namen**
  gepinnt ist; echter Absatz über eine leere `///`-Zeile statt `<para>` (das der Generator verwirft); ein
  Satz im Vertrag, dass der Aufrufer beim **Wechsel** beide Felder schickt — der Server leitet den Namen
  nicht aus der Id ab.
  **Vom Frontend-Reviewer der lehrreichste Fund der ganzen Story:** mein `aria-label` verletzte
  **WCAG 2.5.3 „Label in Name"**. Der sichtbare Text „Reihe bearbeiten" kam im barrierefreien Namen
  („„X" bearbeiten") gar nicht vor — eine Spracheingabe hätte den Knopf nicht ausgelöst. Ich hatte von
  zwei inkonsistenten Nachbarn die schlechtere Form kopiert. Jetzt „Sichttext zuerst, Kontext hinten"
  (`Reihe bearbeiten: „X"`), und der Umschalter heißt sichtbar wie im Label „Bearbeiten schließen".
  Weiter behoben: `duplicate_textbook_series` fehlte in `GERMAN_PROBLEM_TEXT` — der seit dieser Story
  **wahrscheinlichste** Fehlschlag im Formular kam auf Englisch heraus, obwohl die Feld-Erklärung die
  Regel bewirbt; die E2E belegt jetzt, dass das Popover per Escape zugeht (`fill()` macht keinen
  Hit-Test, ein offenes Popover wäre nicht aufgefallen) und greift das Namensfeld über `getByLabel`
  statt über einen Id-Präfix; der `?? ""`-Rückfall in `seriesPatch` ist weg (er hätte im Feuerfall genau
  den Waisen-Zustand erzeugt, um den es geht); der `useRef`-Kommentar hält jetzt fest, warum ein
  veralteter Bezugspunkt keinen Schaden anrichtet (der Diff läuft feldweise) — der Reviewer hat alle
  In-App-Auslöser durchgesehen und ihn nicht als Blocker gewertet.
  Abgespalten: [B-138](B-138-markup-sickert-in-openapi.md) (rohes `<b>`/`<i>`/`<em>` in **70**
  Beschreibungen des Vertragsdokuments — B-123 hat eine davon behoben, ein Einzelfall lässt die Regel
  wie Zufall aussehen), Punkt 4+5 und AK 5+6 an [B-137](B-137-freitext-fach-unerreichbar.md) (das Paar
  driftet auch beim **Wechsel**; und die Schulart-**Kombination** kann das neue Formular zerstören,
  weil `SchoolType` nur die einzelne Schulart kennt).
  **Ausdrücklich offen gelassen:** der Kurzname wird einem Screenreader beim Tabben ins Namensfeld nicht
  angesagt — er steht als `.sub`-Text neben dem Feld, und `aria-describedby` kommt im ganzen Frontend
  bisher nirgends vor. Ein neues Muster gehört nicht in diese Story; der Reviewer nennt als Alternative
  ein `readOnly`-Feld „Kurzname", das den Zustand selbst trägt.
  Verifikation nach allen Korrekturen: Backend **790/790**, Vitest **189/189** (27 Dateien),
  E2E **31/31**, `tsc -b` sauber, Vertragsdokument mit zwei echten Absätzen und ohne Markup.
- **2026-08-10** — **abgenommen.** Commit `e23f321`. Verifikation: Backend **790/790**, Vitest
  **189/189** (27 Dateien), E2E **31/31** — darunter die eigene Spec `lehrwerk-bearbeiten.spec.ts`, die
  zugleich der Rollengang ist. `pugling-reviewer` und `frontend-reviewer` je **zweimal** gelaufen.
  **Ein benannter Rest, damit die Beleglage nicht schöner aussieht als sie ist:** die letzte
  Korrekturrunde (WCAG-Reihenfolge der `aria-label`, deutsche Fehlermeldung, Popover-Zusicherung,
  Testrumpf, Vertragssatz) ist **nicht erneut reviewt**. Sie bestand aus wörtlich vorgeschlagenen Fixes
  beider Reviewer, und die E2E musste für die geänderten Beschriftungen mitwandern und ist grün — aber
  ein dritter Blick hat nicht stattgefunden.
