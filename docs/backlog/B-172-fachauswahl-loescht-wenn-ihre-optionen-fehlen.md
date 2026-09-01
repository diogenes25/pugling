---
tags: [typ/story, status/gegrillt, bereich/frontend, rolle/creator]
aliases: [leeres Fach-Select loescht, subjects.data ?? [], Anzeige und Formularzustand driften]
status: gegrillt
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-123
grund: ""
ersetzt_durch: []
entgangen_bei: [B-123, B-148]
---

# B-172 · Die Fachauswahl löscht das Fach, wenn ihre eigenen Optionen nicht geladen sind

`?? []` zieht „noch nicht geladen" und „das ist die vollständige Liste" zusammen. Vor B-123 war das
harmlos — das Fach-Auswahlfeld gab es nur beim **Anlegen**, und „keine Auswahl" hieß dort „ohne Fach
anlegen". B-123 und B-148 haben aus demselben Feld ein **löschendes** Bedienelement gemacht, dreimal.

## User Story

Als **Creator** möchte ich, dass ein Auswahlfeld mir nie einen Wert anzeigt, den ich nicht gewählt habe,
damit ich das Fach einer geteilten Reihe nicht in dem Moment zerstöre, in dem ich es zu reparieren
versuche.

## Ist-Stand am Code

Die Kette der Aufnahme war an vier Gliedern behauptet. **Drei stimmen, das vierte nicht** — und die
Wirklichkeit ist an einer Stelle harmloser und an zwei Stellen schlimmer als die Behauptung. Gemessen am
2026-08-31 mit einer Wegwerf-Probe gegen `SeriesForm` (fünf Fälle, danach gelöscht).

### Was stimmt

- `frontend/src/vater/VaterLehrwerke.tsx:164` und `:178` reichen `subjects.data ?? []` und
  `publishers.data ?? []` durch; die Abrufe stehen auf `:113`/`:114`.
- `useAsync` lässt `data` bei einem Fehler auf `null` und hat keinen erneuten Versuch
  (`frontend/src/lib/useAsync.ts:18`, `:29-36`).
- **Kein Fehler ist sichtbar.** Die Seite rendert `list.error` (`:157`) und `units.error` (`:458`) —
  `subjects.error` und `publishers.error` **nirgends**. Dasselbe in den beiden Schwesterseiten:
  `VaterKind.tsx` zeigt `child.error` (`:44`) und `interests.error` (`:83`), nicht `subjects.error`
  (`:35`); `VaterFachlehrer.tsx` zeigt nur `list.error` (`:60`), nicht `subjects.error`/`series.error`
  (`:38`/`:39`); `ChildMaterialSection.tsx` zeigt `books.error` (`:48`) und `matches.error` (`:340`),
  nicht `series.error` (`:36`) und nicht `units.error` (`:154`). **Fünf ungerenderte Fehlerzustände.**
- Der Server räumt beim Schalter beide Spalten: `TextbookSeriesController.cs:236`,
  `TextbooksController.cs:113`, `CreatorProfilesController.cs:148`.

### Was nicht stimmt: das Feld steht nicht leer da, es behauptet einen Wert

Die Aufnahme nahm an, der `<select>` stehe bei fehlender Option leer. Er tut es nicht. Gemessen:

| Probe | Aufbau | Ergebnis |
|---|---|---|
| A | `subjects={[]}`, `series.subjectId = 1` | `value === ""`, `selectedIndex === 0`, angezeigt: **„– keine Angabe –"** |
| B | nur den Namen ändern, Fach-Feld nie anfassen | `{"name":"Access 2"}` — **kein** `clearSubject` |
| C | Liste ohne das eigene Fach; Nutzer probiert „Mathe" und geht zurück | **`{"clearSubject":true}`** |
| D | derselbe Umweg beim Verlag | **`{"clearPublisherId":true}`** |
| E | Liste kommt nach der Montage nach | Anzeige springt von selbst auf **„Englisch"** zurück |

Ein `<select>` mit Anzeigegröße 1 und ohne ausgewählte Option wählt die erste nicht gesperrte selbst —
Standardverhalten, nicht jsdom-Eigenart. Der React-Zustand `form.subjectId` bleibt dabei `"1"`.
**Anzeige und Formularzustand driften auseinander**, und der Diff in `seriesPatch` rechnet gegen den
Zustand, nicht gegen die Anzeige.

Drei Folgen, jede gemessen:

1. **Probe B entlastet den Normalfall.** Ein Speichern, das das Feld nicht anfasst, schickt kein
   `clearSubject`. Der in der Aufnahme behauptete Schaden („Gespeichert." — und die Reihe hat ihr Fach
   verloren) folgt so **nicht**.
2. **Probe C belastet den reparierenden Nutzer.** Der in der Aufnahme genannte Auslöser („wählt die
   einzige Option") feuert **kein** `change` — die Option ist ja schon gewählt. Der echte Auslöser ist ein
   **Umweg**: irgendeine andere Option, dann zurück auf „– keine Angabe –". Genau das tut jemand, dem das
   Feld gerade „kein Fach" anzeigt.
3. **Probe E schließt das Ladefenster aus.** Trifft die Liste später ein, springt die Anzeige von selbst
   auf den richtigen Wert. Der Schaden braucht eine Liste, die **leer bleibt** — also den Fehlerfall.

### Die Fläche ist sieben Felder, nicht eins

`subjectField.ts:6-9` begründet, warum nur das Fach eine geteilte Regel bekam: von sieben Schaltern seien
sechs heil, denn „kaputt wird es erst bei einem Feld, dessen **Ladezustand das Formular nicht abbilden
kann**, und das ist heute nur `subjectId`". **Der zweite Halbsatz ist widerlegt** (Probe D). Jedes
Referenzfeld, dessen Optionen aus einem eigenen, unabhängig scheiternden Abruf kommen, trägt denselben
Zustand:

| Formular | Felder | Optionen aus | Schalter |
|---|---|---|---|
| `SeriesForm` (`VaterLehrwerke.tsx:355-380`) | Verlag, Fach | `:113`/`:114` → `:164` | `clearPublisherId` (`seriesPatch.ts:79-82`), `clearSubject` (`:84`) |
| Lehrbuch am Kind (`ChildMaterialSection.tsx:225-262`) | Fach, Reihe, Unit | `VaterKind.tsx:69`, `ChildMaterialSection.tsx:86`, `:154` | `clearSubject`, `clearSeries`, `clearUnit` (`textbookPatch.ts:65`, `:71-74`, `:77-80`) |
| Fachlehrer-Profil (`VaterFachlehrer.tsx:305-315`) | Fach, Reihe | `:93`/`:114` | `clearSubject`, `clearSeries` (`profilePatch.ts:76`, `:78-81`) |

Die Unit ist der siebte Fall und beim Grillen dazugekommen: `units.data?.map` (`:257`) liefert bei einem
gescheiterten Abruf gar keine Optionen, und der Hinweis daneben (`:263`) prüft `units.data?.length === 0`
— bei `null` schweigt also auch er. Beim Lehrbuch hängt zusätzlich ein Folgeschaden dran: die Reihe zu
wechseln räumt die aktuelle Unit (`:163`), und `clearSeries` tut serverseitig dasselbe.

Gemessen ist `SeriesForm` (Proben A–E). Die beiden anderen Formulare sind **strukturell** belegt — gleiche
Feldform, gleicher `subjectPatch`, gleiches `?? []`, kein gerenderter Fehler — aber nicht gefahren.

### Warum kein Fach-Löschen den Zustand erzeugt

Nachgesehen, weil es die Größe entschieden hätte: `creator/subjects` liefert **alle** Fächer, ungefiltert
und ohne Paging (`SubjectsController.cs:36-37`). Und ein gelöschtes Fach setzt an allen drei Trägern nur
den Fremdschlüssel auf `null` (`PuglingDbContext.cs:198`, `:241`, `:272` — je `SetNull`), während
`SubjectName` als **gespeicherte Spalte** stehen bleibt: dann greift der Freitext-Zustand aus B-143, und
das Feld sagt die Wahrheit. **Es gibt also genau einen Weg in den Zustand — der gescheiterte Abruf.**

## Die echte Lücke

Nicht „ein leeres Feld löscht", sondern: **das Formular zeigt einen Wert an, den niemand gewählt hat, und
sagt nicht, dass es die Grundlage seiner Anzeige nie bekommen hat.** Der Nutzer trifft danach jede
Entscheidung auf falscher Information — und *beide* Auswege zerstören: eine andere Option überschreibt das
Fach, „– keine Angabe –" räumt Id **und** Rückfall-Namen.

Zwei Umstände machen daraus einen P1 statt eines Schönheitsfehlers:

- **Die Reihe ist geteilt.** Der Schaden trifft nicht den Verursacher, sondern jeden, der die Reihe nutzt:
  ohne Fach trägt sie keine Übungen mehr (`series_without_subject`, `VaterLehrwerke.tsx:397`).
- **Er ist nicht rekonstruierbar.** Mit dem Namen fällt der letzte Zeuge dessen, was dastand.

Die Zeile daneben weiß es besser: `VaterLehrwerke.tsx:207` zeigt den Fachnamen aus `series.subjectName`,
wenn die Liste ihn nicht kennt. Der Rückfall existiert also schon — er steht nur in der Liste und nicht im
Formular.

**Warum keine Testebene das sah:** `seriesPatch`/`subjectPatch` kennen die Options-Liste nicht, und
`SeriesForm.test.tsx:34` übergibt immer ein gefülltes `SUBJECTS`. Die Fälle in `subjectField.test.ts:39-76`
prüfen die Regel — und die Regel ist richtig. Falsch ist die Eingabe, die das Formular ihr liefert.

## Offene Punkte

1. ~~Trifft die Kette wirklich zu?~~ **Gemessen** (Proben A–E, siehe Ist-Stand): ja im Kern, nein im
   Auslöser. Der Schaden braucht einen Umweg über eine andere Option, nicht einen einzelnen Griff.
2. ~~Gilt dasselbe an weiteren Stellen?~~ **Gemessen**: drei Formulare, sieben Referenzfelder, fünf
   ungerenderte Fehlerzustände.
3. ~~Sperren oder Fehler zeigen?~~ → Entscheidung 1. Keine Sperre.
4. ~~Wie unterscheidet das Formular „lädt noch" von „ist fehlgeschlagen"?~~ **Gegenstandslos** durch
   Entscheidungen 1 und 3: die Rückfall-Option ist in beiden Fällen richtig, das Formular braucht die
   Unterscheidung also nicht. Nur die Seite braucht `error` fürs Banner — und das hat sie schon. `?? []`
   bleibt, der Signaturwechsel an sieben Übergabestellen entfällt.
5. ~~Reicht die Sperre, oder braucht das Feld einen Rückfall wie bei B-143?~~ → Entscheidungen 1, 3, 4.
   Der Rückfall ist der ganze Kern; die Sperre entfällt. Die Empfehlung der Ausformulierung („Sperre
   genügt") war **falsch** und ist beim Grillen gekippt.
6. ~~Eine Story oder drei?~~ → Entscheidung 6. Eine.
7. ~~Zieht die Begründung in `subjectField.ts:6-9` nach?~~ → Entscheidung 2, zwingend: die Regel wandert
   ins Bauteil, also muss der Kommentar sie dort begründen statt an der alten Stelle das Gegenteil.

## Entscheidungen

Gegrillt am 2026-09-01. Zwei Fakten wurden während der Runde nachgesehen und haben zwei Empfehlungen
gekippt — sie stehen bei den betroffenen Entscheidungen.

### 1 · Die Behebung zielt auf die Lüge, nicht auf die Zerstörung

Das Feld zeigt den bekannten Namen als **gewählte Option** und die Seite meldet den gescheiterten Abruf.
**Keine Sperre.**

*Begründung:* Die Drift verschwindet an der Wurzel — `form.subjectId` findet wieder eine passende Option,
der DOM-Rückfall auf Index 0 entfällt, und Leeren wird wieder eine bewusste Handlung. Eine Sperre nimmt
nur die Folge weg: der `<select>` zeigte weiter „– keine Angabe –", also einen Wert, den niemand gewählt
hat, und das Feld wäre für die Dauer des Ausfalls unbenutzbar.

*Kosten:* Eine Bedingung je Feld (sieben) plus der Name, wo er noch nicht im Formular liegt — **nachgesehen:
er liegt überall**, siehe Entscheidung 3.

### 2 · Die Regel bekommt ein geteiltes Bauteil, kein Tor

Ein `ReferenceSelect` nimmt Id, Name und Optionsliste und erzeugt die Rückfall-Option selbst. Dazu ein
Vitest **über das Bauteil**, nicht über die Quellen.

*Begründung:* Dasselbe Argument, mit dem `subjectField.ts:11-12` heute schon den PATCH-Rumpf an einer
Stelle hält („die hält man nicht dreimal unabhängig richtig"), eine Ebene höher für die Anzeige. Der achte
Fall erbt die Regel, statt bei null anzufangen — und siebenmal von sieben hat die Disziplin hier verloren,
das Messargument für eine Vereinheitlichung ist also erbracht.

*Warum kein quelltextlesendes Tor:* **Nachgesehen** — im Frontend liest heute **kein** Test Quelltext; die
Wächter dieses Repos (`ConventionGuardTests`, `ClientRouteGuardTests`) sitzen alle im Backend und greifen
nie ins Frontend. Ein Tor wäre eine Neuerfindung, und ein Regex über TSX ist der halbe Parser, der laut
Erfahrung dieses Repos eine Rot-Liste braucht, um mehr zu beweisen als „es hat sich nichts geändert".

*Kosten:* Sieben Aufrufstellen umbauen, drei davon in Formularen, die B-143/B-148 gerade angefasst haben.
Wer am Bauteil vorbei einen rohen `<select>` schreibt, bleibt ungefangen — das ist der bewusst gezahlte
Preis dafür, kein halbes Parser-Tor zu bauen.

### 3 · Das Bauteil trägt beide Sonderzustände

„Freitext-Fach" (B-143: Id ist `null`, Name steht) und „nicht geladen" (Id steht, Liste kennt sie nicht)
leitet das Bauteil **selbst** aus `id`, `name` und `options` ab. `FREETEXT_SUBJECT` bleibt in
`subjectField.ts`, weil der Sentinel zum PATCH-Rumpf gehört, nicht zur Anzeige.

*Begründung:* Es sind zwei Fälle einer Frage — „der Wert ist da, aber nicht in der Liste". Zwei Quellen
für Sonderoptionen im selben `<select>` sind die Ausgangslage dieses Defekts, eine Windung weiter.

*Nachgesehen, was das Risiko deckelt:* **Alle sieben Felder tragen den Namen neben der Id in der Antwort**
(`PublisherName`, `SubjectName`, `SeriesName`, `CurrentUnitLabel`) — die Rückfall-Option ist überall
beschriftbar, **ohne Vertragsänderung**. Und die B-143-Zusicherungen in `SeriesForm.test.tsx:37-52` prüfen
durch das Formular hindurch (`getByLabelText`, `option.disabled`), nicht das Innere: sie werden beim Umbau
zum **Netz**, nicht zum Hindernis.

*Kosten:* Das Bauteil kennt zwei Sonderfälle statt einen und muss beide begründen.

### 4 · Nur die Freitext-Option ist gesperrt — „nicht geladen" ist wählbar

*Begründung:* Die beiden Zustände sehen gleich aus, sind es aber nicht. „Freitext" darf der Nutzer nicht
**herstellen** (darum sperrt B-143 ihn); „nicht geladen" ist ein völlig reguläres Katalog-Fach, das nur
gerade fehlt. Gesperrt würde Probe C zur **Einbahnstraße**: wer „Mathe" probiert und den Fehler merkt,
käme nie zu „Englisch" zurück — nur übers Zuklappen des Formulars, und das wirft laut
`VaterLehrwerke.tsx:309` alle anderen Eingaben mit weg. Der Defekt wäre dann entschärft, aber nicht
behoben: die Zerstörung bräuchte einen Griff statt zweier.

*Kosten:* Das Bauteil trägt zwei Formen (gesperrt/wählbar) und muss den Unterschied im Kommentar tragen.

### 5 · Das Banner bekommt keinen „Erneut versuchen" — das wird eine eigene Story

*Begründung:* **Nachgesehen** — von 68 `banner err` im Frontend bietet **keines** einen erneuten Versuch;
`reload` wird 75-mal genutzt, aber nie zur Erholung von einem gescheiterten Lesen. Ein Retry hier wäre ein
neues Muster an fünf Stellen und fehlte an 68 — genau die Zufalls-Inkonsistenz, die B-178 und B-126
erzeugt haben. Das Ziel von B-172 ist ohne ihn erfüllt, also gilt die Backlog-Regel „ein Fund beim Bauen
wird eine eigene Story".

*Kosten:* Bis zu jener Story hilft sich der Nutzer mit F5 — was er heute auch tut, nur ohne zu wissen,
warum. Abgelegt als [B-183](B-183-gescheitertes-lesen-ohne-erneuten-versuch.md).

*Anmerkung zur Redlichkeit:* Mein Mockup zu Entscheidung 1 zeichnete diesen Knopf, bevor ich nachgesehen
hatte, ob es ihn gibt. Er war kein Teil dessen, was zur Wahl stand.

### 6 · Eine Story, nicht geteilt

*Begründung:* Der Wert des Bauteils liegt darin, dass die Regel nur noch an **einer** Stelle steht.
Solange vier Felder ihren rohen `<select>` behalten, ist die Regel weiter siebenfach und der achte Fall
erbt nichts — ein halb übernommenes Bauteil ist schlechter als keins, weil es behauptet, die Frage sei
geklärt. Die Naht „Lüge / Schweigen" wäre echt, aber die zweite Hälfte sind fünf Zeilen und bräuchte die
volle Kette aus Grillen, Schätzen, Reviewer und Rollengang.

*Kosten:* Ein Diff über drei Formular-Dateien, drei Seiten und ein neues Bauteil. Größe geschätzt M–L,
also innerhalb dessen, was der Bereich zulässt.

## Akzeptanzkriterien

1. Es gibt ein Bauteil, das aus `id`, `name` und `options` die Sonderoption selbst ableitet. Die drei
   Formulare (`SeriesForm`, Lehrbuch am Kind, Fachlehrer-Profil) nutzen es für **alle sieben** Felder:
   Verlag, Fach, Reihe, Unit.
2. Enthält die Optionsliste den geladenen Wert nicht, zeigt das Feld ihn als **gewählte, nicht gesperrte**
   Option mit dem bekannten Namen. Es zeigt **nie** „– keine Angabe –", ohne dass der Nutzer das gewählt
   hat.
3. Der Freitext-Zustand aus B-143 bleibt eine **gesperrte** Option und bleibt für den Nutzer nicht
   herstellbar. `SeriesForm.test.tsx:37-52` bleibt unverändert grün.
4. Der Umweg aus Probe C ist wieder ein Rückweg: eine andere Option wählen und zur ursprünglichen
   zurückkehren ist möglich, ohne das Formular zuzuklappen.
5. Die fünf ungerenderten Fehlerzustände werden sichtbar: `subjects.error` und `publishers.error` in
   `VaterLehrwerke`, `subjects.error` in `VaterKind`, `series.error` und `units.error` in
   `ChildMaterialSection`, `subjects.error` und `series.error` in `VaterFachlehrer` — als `banner err`,
   **ohne** Retry-Knopf.
6. Ein Regressionstest, der **vorher rot** ist: Probe C in Testform (unvollständige Liste, Umweg über eine
   andere Option, Erwartung „kein `clearSubject` im Rumpf"). Beim Schreiben je Zusicherung benennen, welche
   Änderung sie rot macht.
7. Die Begründung in `subjectField.ts` sagt, was gilt: die Anzeige-Regel steht im Bauteil, der Satz „das
   ist heute nur `subjectId`" ist weg.
8. Alles bisher Grüne bleibt grün (831 Backend, Vitest, E2E).

**Zum Rollengang:** Dieser Defekt braucht einen künstlich kaputten Server — laut
[README](README.md#der-rollengang-fällt-am-leichtesten-weg--und-kostet-am-meisten) genau der argumentierte
Fall, in dem der Komponententest an die Stelle der E2E tritt. Der Gang an der laufenden App prüft dafür die
Gegenprobe: dass die Felder mit **funktionierender** Liste unverändert bedienbar sind.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-123. Bleibt `unverifiziert`: Die Fundorte sind
  benannt, aber ich habe die Kette nicht selbst gefahren — und eine Kette mit vier Gliedern ist genau die
  Sorte Behauptung, die man nicht abschreibt.
- 2026-08-31 · `idee → ausformuliert`. Kette mit einer Wegwerf-Probe gegen `SeriesForm` gefahren (fünf
  Fälle, danach gelöscht). **Die Prämisse fiel**: der `<select>` steht nicht leer, er zeigt
  „– keine Angabe –" an und driftet damit vom Formularzustand ab; der behauptete Auslöser feuert kein
  `change`, der echte ist ein Umweg; ein Speichern ohne Griff ins Feld ist harmlos. Dafür ist die Fläche
  **sechsmal so groß** wie behauptet, und die Begründung in `subjectField.ts:6-9` ist widerlegt.
  `prio` bleibt **P1**.
- 2026-09-01 · `ausformuliert → gegrillt`, sechs Entscheidungen. **Zwei meiner Empfehlungen sind
  gekippt**, beide an einem Fakt, den ich in der Runde nachgesehen habe: „Sperre genügt" fiel, weil alle
  sieben Felder ihren Namen in der Antwort tragen — die Rückfall-Option kostet keine Vertragsänderung und
  behebt die Lüge statt nur ihre Folge; und der „Erneut versuchen"-Knopf aus meinem eigenen Mockup fiel,
  weil keines der 68 vorhandenen Fehlerbanner einen hat. Dazu ein **siebter** betroffener Fall gefunden
  (die Unit im Lehrbuch) und ein Sonderfall, den die Ausformulierung nicht sah: eine gesperrte
  Rückfall-Option hätte den Umweg zur Einbahnstraße gemacht. Punkt 4 der offenen Punkte ist
  gegenstandslos geworden, `?? []` bleibt.
