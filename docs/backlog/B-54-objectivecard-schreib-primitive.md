---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/qualitaet]
aliases: [ObjectiveCard ohne useAction, Etappe ohne Erfolgsmeldung, Dashboard-Doppelklick]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-26-e2e-in-ci.md
---

# B-54 · Fünf Knöpfe im Vater-Web gehen an den Schreib-Primitiven vorbei

Die Ziel-Karte im Vater-Web schreibt an `useAction`/`StatusBanner` vorbei: sie baut ihr eigenes
`try/catch` mit lokalem `err`-State. Folge – **eine geglückte Mutation meldet gar nichts.** Anlegen,
Ändern und Löschen einer Etappe (Key Result) laufen wortlos durch; nur der Fehler wird sichtbar.
Aufgefallen beim Umschreiben des E2E-Abschnitts in [B-26](B-26-e2e-in-ci.md): der alte, tote Abschnitt
konnte noch auf „Lernziel angelegt." prüfen, der neue hat keine Meldung mehr, auf die er prüfen könnte.

## User Story

Als **Vater**, der eine Etappe an einem großen Ziel nachträgt, möchte ich dieselbe Rückmeldung bekommen
wie überall sonst im Vater-Web – damit ich nicht aus dem Erscheinen einer Tabellenzeile schließen muss,
ob mein Klick angekommen ist, und damit ein zweiter Klick nicht zwei Etappen anlegt.

## Ist-Stand am Code

Zeilen nachgeführt am **2026-08-01** (sie waren um ~20 verrutscht, E6 hat Importe ergänzt):

- `VaterZiele.tsx:231-234` – eigenes `act()` mit `setErr(null)` / `try … catch (e) { setErr(errorMessage(e)) }`
  statt `action.run(fn, okText)`. Ausgabe nur über `{err && <div className="banner err">}` (`:271`).
- Betroffen sind darüber drei Schreibpfade: `createKeyResult` (`:290`), `updateKeyResult` (`:259`) und
  `deleteKeyResult` (`:262`).
- Die Ebene darüber macht es richtig: `Objectives` (`:179`) nimmt `useAction` und rendert einen
  `StatusBanner` (`:197`) – Stilllegen und Löschen eines Ziels melden also, das Bearbeiten seiner Etappen
  nicht. Dieselbe Seite, zwei Verhalten.
- Regel dazu: [frontend/CLAUDE.md](../../frontend/CLAUDE.md) bzw.
  [memory/frontend-schreib-primitive](../obsidian.md) – „`useAction` + `StatusBanner` für **jede** Mutation".

### Die vollständige Liste (dritte Zählung 2026-08-01, diesmal über die Bindung)

| Datei:Zeile | Knopf | Schreibpfad |
|---|---|---|
| `VaterZiele.tsx:319` | „OK" (Zielwert einer Etappe) | `ObjectiveCard.act` (`:231`) |
| `VaterZiele.tsx:321` | „Entfernen" (Etappe) | `ObjectiveCard.act` |
| `VaterZiele.tsx:362` | „Etappe übernehmen" | `ObjectiveCard.act` – **trägt** ein `disabled`, aber an einer Eingabeprüfung (`scope.subjectId === ""`), nicht an `busy` |
| `VaterVocab.tsx:694` | `TagChip` „×" | `removeGlobal` (`:622`) / `removeChild` (`:642`), eigenes `try/catch` mit `setErr` |
| `VaterDashboard.tsx:96` | „Kind anlegen" | `addChild` (`:22`), eigenes `try/catch` mit `msg` |

Zwei Dinge, die dabei über die ursprüngliche Story hinausgehen:

- **`VaterDashboard.addChild` ist derselbe Defekt wie [B-53](B-53-wizard-doppelklick.md)**, auf dem
  Bildschirm, auf dem ein neuer Vater **zuerst** landet: kein `busy`, kein Ref-Gate, zwei Klicks im selben
  Tick legen **zwei Kinder** an. B-53 hielt den Assistenten für „den teuersten Fall der Klasse" – der
  Assistent legt mehr an, aber dieser Weg wird häufiger gegangen.
- **Der Fehler erscheint dort grün.** `:103` rendert die Meldung fest als `<div className="banner ok">`,
  und `:33` schreibt in dieselbe Variable den `errorMessage(err)`. „Kind angelegt." und „Name schon
  vergeben" sehen identisch aus. Genau das verhindert `StatusBanner` + `ActionMessage.ok`.

**Wie die Zahl zweimal falsch war** (erst „fünf fehlen", dann „genau zwei bleiben"): beide Male hat eine
Messung nur geprüft, ob am Knopf das Wort `disabled` **steht** – nicht, woran es gebunden ist. `:362` oben
ist der Beleg. Wer nachzählt, muss die Bindung lesen.

### Die dritte Zählung bestätigt die Liste – und findet zwei Randstellen

Gemessen über einen Durchgang durch **alle** `<button>`-Elemente von `src/vater/` und `src/components/`
samt ihrer `disabled`-**Bindung**, nicht ihrer Anwesenheit:

| Vater-Web (`src/vater` + `src/components`), **Stand vor der Reparatur** | Zahl |
|---|---|
| `<button>` insgesamt | 179 |
| mit `disabled` | 100 |
| ohne `disabled`, **nicht** mutierend | 75 |
| ohne `disabled`, **mutierend** | **4** (`VaterZiele:319`, `:321`, `VaterVocab:694`, `VaterDashboard:96`) |
| mit `disabled`, aber **nicht** an `busy` | **1** (`VaterZiele:362`) |

Die 75 harmlosen sind Aufklappen/Zuklappen, Tab- und Modus-Wähler, Abbrechen/Schließen, **fünf
Such-Formulare** (reine Lesepfade über `useAsync`) und lokale Zeilen-Adds in Formularen, deren Speichern
selbst gesperrt ist (`VaterKind:384` „Hinzufügen" schreibt in `setRows`, nicht zum Server).

Zusätzlich geprüft: **alle 35 `onSubmit`-Formulare.** Ein mutierendes Formular mit un-gesperrtem
Submit-Knopf ist in einer Knopf-Zählung unsichtbar – das ist `VaterDashboard`s Form, und es ist das
einzige seiner Art.

Zwei Randstellen, die die Liste oben nicht nennt:

1. **`TagEditor` hat vier Schreibpfade, nicht zwei.** `addGlobal` (`:617`) und `addChild` (`:630`) hängen am
   *selben* `err`-State wie die beiden `remove*`; ihre Knöpfe sind nur deshalb gesperrt, weil `TagAdder`
   (`:701`) ein **eigenes lokales** `busy` hält. Siehe Entscheidung 1.
2. **`KeyResultForm` steht an zwei Stellen.** In `ObjectiveCard` (`:288`) schickt sie zum Server, in
   `NewObjective` (`:451`) sammelt sie nur in ein lokales Array. Siehe Entscheidung 4.

## Die echte Lücke

Nicht „ein Banner fehlt", sondern: **kein Rückkanal für Erfolg** an der einzigen Stelle, an der ein Ziel
überhaupt erreichbar gemacht wird. Dazu kommt die Wiedereintritts-Sperre aus
[B-43](B-43-frontend-komponententests.md)/E5, die genau an `useAction` hängt – dieser Pfad bekäme sie
nicht mit. Ein Doppelklick auf „Etappe übernehmen" schickt heute **zwei POSTs** hinaus.
(Der Satz hieß bis zur Abnahme „legt zwei Etappen an" – das ist **falsch**, und die Gegenprobe hat es
gezeigt: der zweite Schreibvorgang verliert serverseitig das Rennen. Siehe Verifikation.)

## Offene Punkte

1. ~~**Zusammen mit E5 bauen oder danach?**~~ Entschieden im Verlauf vom 2026-08-01: danach. Die Sperre
   sitzt seit E5 im Primitiv, diese Story ändert nur den Aufrufer.
2. ~~Gehört das mit [B-49](B-49-sohn-app-schreib-primitive.md) in **eine** Story?~~ Nein – andere Fläche,
   andere Rolle, und B-49 ist ungeprüft.

## Entscheidungen

1. **Alle vier Schreibpfade des `TagEditor`, nicht nur die zwei benannten.** Sie teilen *einen* `err`-State;
   zwei davon umzustellen hinterließe eine Komponente mit **zwei** Fehlerkanälen – schlechter als beide
   Endzustände. *Kosten:* zwei Pfade mehr, und `TagAdder.onAdd` muss `boolean` liefern. Letzteres räumt einen
   Defekt derselben Familie mit ab: heute leert `TagAdder` sein Eingabefeld **auch nach einem
   fehlgeschlagenen** Add, der getippte Text ist also weg.
2. **Der `TagEditor` bleibt bei Erfolg stumm** (Mensch, 2026-08-01). Der Chip erscheint bzw. verschwindet –
   das *ist* die Rückmeldung; ein Banner je Chip-Klick wäre Lärm auf einem Bildschirm, auf dem man mehrere
   Tags hintereinander anfasst. `useAction` sieht den Fall ausdrücklich vor („ohne `okText` bleibt das Banner
   leer"). *Kosten:* AK 2 („meldet Erfolg") gilt damit für die drei Pfade der Karte und das Dashboard, **nicht**
   für die Tags. Das ist eine bewusste Ausnahme von der Regel in `frontend/CLAUDE.md`, keine Lücke – die
   Mechanik der Regel (Ref-Gate, `busy`, rot/grün getrennt) greift dort vollständig.
3. **AK 6 wird einmal gezählt, nicht mechanisch bewacht** (Mensch, 2026-08-01). Ein Tor bräuchte einen halben
   TSX-Parser **plus** eine gepflegte Erlaubnisliste der 75 harmlosen Knöpfe – „mutierend" ist nicht
   mechanisch entscheidbar, die Liste trüge also die Wahrheit, nicht der Test. *Kosten, ausdrücklich in Kauf
   genommen:* die Zahl verrottet, und genau das ist in dieser Story schon zweimal passiert. Der dritte
   Vorschlag (das Tor als eigene Story aufzunehmen) ist damit **nicht** gezogen worden.
4. **`KeyResultForm` bekommt `busy` als optionales Prop.** Sie steht an zwei Stellen (Ist-Stand, Randstelle 2);
   ein fest verdrahtetes `action.busy` würde das *lokale* Sammeln im Anlege-Formular sperren, während das
   Anlegen des Ziels läuft. *Kosten:* ein Prop.
5. **Die Karte bekommt ihre **eigene** `useAction`-Instanz, nicht die der Liste.** Der `StatusBanner` der
   Liste steht *über* allen Karten; „Etappe angelegt." erschiene am Seitenkopf statt an der Karte, die
   geklickt wurde. Die Karte rendert ihr Banner dort, wo heute ihr `err`-Banner steht. *Kosten:* eine Karte
   hat zwei `busy`-Quellen – die der Liste (Stilllegen/Löschen) und ihre eigene (Etappen). Das ist genau, was
   gemeint ist, und der Kommentar am `busy`-Prop sagt es schon.
6. **„+ Etappe", „Abbrechen" und die 75 anderen bleiben ohne `disabled`.** Sie schalten lokalen Zustand. AK 6
   sagt „mutierend", nicht „jeder Knopf" – ein `disabled` an einem Aufklapper wäre eine Behauptung über einen
   Server-Aufruf, den es nicht gibt.

## Akzeptanzkriterien

1. Die drei Schreibpfade der Karte (`createKeyResult`, `updateKeyResult`, `deleteKeyResult`) laufen über
   `useAction`; das eigene `try/catch` mit `err`-State ist weg.
2. Jeder der drei meldet **Erfolg** über einen `StatusBanner` – nicht nur den Fehler. (Für den `TagEditor`
   gilt Entscheidung 2: dort bleibt der Erfolg stumm.)
3. `vater-von-null.spec.ts` prüft nach „Etappe übernehmen" die Erfolgsmeldung, nicht nur das Erscheinen der
   Zeile. (Heute äußert sich ein fehlgeschlagener Aufruf als Timeout auf die Zeile statt als lesbare
   Meldung — genau die Diagnose, die der Abschnitt vor dem Umbau hatte.)
4. Ein Doppelklick auf „Etappe übernehmen" legt **eine** Etappe an — greift, sobald die Sperre aus
   [B-43](B-43-frontend-komponententests.md) im Primitiv sitzt (dort seit 2026-08-01).
5. Dasselbe für die zwei nach E5 nachgetragenen Stellen: `VaterVocab` `TagChip` und
   `VaterDashboard.addChild`. Beim Dashboard gehört dazu, dass ein **Fehler nicht mehr grün** erscheint –
   heute schreiben Erfolg und Fehler in dieselbe Variable, die fest als `banner ok` gerendert wird.
6. Danach trägt **jeder** mutierende Knopf des Vater-Webs `disabled={busy}`. Gegenprobe: nachzählen mit
   Blick auf die **Bindung** des `disabled`, nicht auf seine Anwesenheit (`VaterZiele.tsx:362` ist der Fall,
   an dem genau das zweimal schiefging).

## Schätzung

**S** · `wo: frontend` · keine Migration · kein Vertragsbruch. Kein Backend-Anteil: der Umbau tauscht
Aufrufer gegen ein bestehendes Primitiv, die API bleibt unberührt.

**Angriffsplan** (Reihenfolge, jede Stufe für sich lauffähig):

1. `VaterZiele.tsx` – eigene `useAction` in `ObjectiveCard`, `StatusBanner` statt `err`-Div, `busy` in
   `KeyResultRow` und `KeyResultForm` (Entscheidungen 4 + 5).
2. `VaterVocab.tsx` – `TagEditor` auf `useAction` (alle vier Pfade, stumm bei Erfolg), `TagChip`/`TagAdder`
   bekommen `disabled`, `onAdd` liefert `boolean` (Entscheidungen 1 + 2).
3. `VaterDashboard.tsx` – `useAction` + `StatusBanner`, Felder nur bei Erfolg leeren.
4. E2E: Erfolgsmeldung + Doppelklick in `vater-von-null.spec.ts`; die Farbe des Fehlers über eine
   abgefangene Antwort.
5. Nachzählen (AK 6) und die Zahl in die Abnahme schreiben.

**Testweg.** Regressionsträger ist Playwright, nicht Vitest: `frontend/CLAUDE.md` zieht die Grenze
„**kein nachgebauter Bildschirm mit gefälschtem `fetch`**" – ein Komponententest von `VaterDashboard`
bräuchte genau das. Die Zusicherungen des Primitivs sind in `useAction.test.tsx` schon geprüft; was hier
fehlt, ist der **Nachweis der Verdrahtung**, und der ist ein Weg durch die App.

- AK 3: `expect(getByText("Etappe angelegt."))` – vorher rot, es gibt die Meldung nicht.
- AK 4: `dblclick` auf „Etappe übernehmen", danach „0/1 Etappen". Vorher rot: ohne Gate und ohne `disabled`
  laufen beide Klicks in den POST (das Formular schließt erst *nach* dem `await`), es stünde „0/2".
- AK 5: eine per `page.route(…, { times: 1 })` erzwungene Fehlerantwort auf `POST supervisor/children`,
  danach `.banner.err` sichtbar und `.banner.ok` nicht. Vorher rot: der Text landet heute im grünen Kasten.
  Kein Server-Fehler ließe sich sonst provozieren – `ChildrenController` prüft nur den leeren Namen, und den
  fängt das Formular selbst ab.
- Unverändert grün bleiben müssen: 48 Vitest, 25 Playwright.

**Risiken.**

- Der `dblclick` könnte durch die Actionability-Wartezeit *serialisiert* werden und damit nichts beweisen.
  Deshalb prüft die Zusicherung die **Zahl** der Etappen, nicht die Zahl der Aufrufe: ob Playwright
  serialisiert oder das Ref-Gate greift, ist der Strecke gleich – „0/2" wäre in beiden Fällen rot.
- Die abgefangene Antwort darf nicht in den Rest der langen Strecke lecken (`times: 1` und danach `unroute`).
- `TagChip` steht auch als **Filter**-Chip im Store (`VaterVocab:217`), wo es nichts schreibt. Das neue
  `disabled` muss dort optional bleiben.

## Verifikation

Alles unten ist gelaufen, nicht behauptet. **48/48 Vitest · 25/25 Playwright · `tsc -b` grün · Prod-Build
585 KiB · markdownlint 0 Befunde.** Kein `.cs` berührt, das Backend-Tor blieb unangetastet.

| AK | Belegt durch | Gegenprobe (Fix zurückgenommen) |
|---|---|---|
| 1 | `VaterZiele.tsx` – `err`-State und eigenes `try/catch` sind weg, `act` ruft `krAction.run` | — (Struktur, im Diff nachlesbar) |
| 2 | `vater-von-null.spec.ts`: „Etappe angelegt." nach dem Übernehmen | **rot**: die Meldung existiert im alten Stand nicht |
| 3 | dieselbe Zusicherung | wie 2 |
| 4 | Zahl der POSTs auf `/key-results` nach einem `dblclick` = 1 | **rot**: 2 POSTs (einzeln gemessen, s. u.) |
| 5 | `.banner.err` trägt den Fehlertext, `.banner.ok` gibt es nicht; Eingabe bleibt stehen. Tag-Editor: neuer Abschnitt in `bilder.spec.ts` | **rot**: `.banner.err` existiert nicht, der Text saß im grünen Kasten, und die Eingabe war geleert |
| 6 | Nachzählung unten | — |

**AK 6, dritte Zählung, jetzt am Endstand:** 179 `<button>` · **104** mit `disabled` (96 davon an einem
`busy`; 8 an etwas anderem, alle geprüft harmlos: zwei laufende Suchen, zwei Pager-Grenzen,
`rows.length === 1`, drei Durchreicher, deren Aufrufstelle `action.busy` bindet) · **75** ohne `disabled`,
darunter **kein** Server-Schreiber. Der `frontend-reviewer` hat unabhängig nachgezählt und kam auf dieselbe
Aufteilung (bei der Randfrage „was zählt als `busy`" auf 95/9 statt 96/8 – dieselbe Substanz).

### Was die Gegenprobe an meiner eigenen Zusicherung gefunden hat

AK 4 stand zuerst als „nach dem Doppelklick steht `0/1 Etappen`" im Test. Diese Zusicherung war **mit dem
alten Stand ebenfalls grün** – sie hätte nichts bewiesen. Gemessen: der alte Stand schickt **zwei POSTs**
hinaus, es entsteht aber nur *eine* Etappe, weil der zweite Schreibvorgang serverseitig das Rennen um die
SQLite-Schreibsperre verliert (`AddKeyResultAsync` hat keine Eindeutigkeitsprüfung, es ist also wirklich das
Rennen und keine Fachregel). Die Story selbst trug die Verwechslung im Text („legt heute zwei Etappen an").

Der Defekt ist der **Doppel-POST**, nicht die Zahl der Etappen. Der Test zählt darum die abgeschickten
Anfragen. Nebenbefund für die Zukunft: was den zweiten Klick praktisch abfängt, ist das `disabled` (React
schreibt `busy` im `await`-Yield zwischen die Klicks); das Ref-Gate ist das Netz für zwei Klicks im
*selben* Tick und wird dort geprüft, wo es sitzt (`useAction.test.tsx`).

### Drei Defekte, die beim Bauen von selbst herausfielen

1. **Die `useAsync`-Falle, zweimal** (`VaterZiele:205`, `VaterVocab:238`): `{loading ? "Lade…" : zeilen}`
   hängt bei *jeder* Änderung alle Zeilen aus, weil `useAsync` `data` behält und `loading` erneut setzt.
   `frontend/CLAUDE.md` warnt wörtlich davor. In `VaterZiele` fiel es auf, weil die neue Erfolgsmeldung
   sofort wieder verschwand – die Karte, die sie hält, wurde unmontiert. In `VaterVocab` ist die Folge
   älter und schwerer: **einen Tag hinzuzufügen schloss den Tag-Editor, in dem man gerade stand**, samt
   dem Bild-Abschnitt daneben. Zwei Einzeiler; sie sind zugleich die Voraussetzung dafür, dass der neue
   E2E-Abschnitt überhaupt klicken kann.
2. **Drei Knöpfe mit demselben Namen** (`TagAdder`): das „+" trug fest `aria-label="Tag hinzufügen"`, und
   der Baustein steht dreimal gleichzeitig auf dem Bildschirm (Filter, globale Tags, Kind-Tags) – für einen
   Screenreader wie für einen Test nur über die Position unterscheidbar. Heißt jetzt nach seiner Eingabe.
3. **`TagAdder` verlor die Eingabe nach einem Fehlschlag** (Entscheidung 1 hat es mit abgeräumt): geleert
   wurde unbedingt, der getippte Text war also weg, wenn der Server ablehnte.

### Was der `frontend-reviewer` gefunden hat

Vier Punkte, alle vor dem Übernehmen am Code geprüft:

- **`frontend/CLAUDE.md` behauptete nach dem Umbau etwas Falsches** – die Ausnahmeliste der Regel nannte
  weiter „fünf Vater-Knöpfe (B-54)". Genau diese Zeile ist die kanonische Fassung der Regel. Gekürzt, und
  Entscheidung 2 steht jetzt als **benannte Ausnahme** dabei; ohne sie liest die nächste Sitzung den stummen
  Tag-Editor als Regelbruch und „repariert" ihn. (Dafür zwei Historien-Halbsätze entfernt – das Budget
  von 9000 Bytes war voll, Stand jetzt 8995.)
- **`removeGlobal` hing an einem Wettlauf, den der neue Test jedes Mal geht.** Der Chip kommt aus der
  Vokabel-Liste, seine Id aus der Tag-Liste, und `onGlobalChanged` lädt beide *nebenläufig*. Ein frisch
  angelegter Tag ist also anklickbar, bevor er in `globalTags` steht – dann sagte das „×" „bitte Seite neu
  laden", obwohl nichts fehlt. Erreichbar wurde das erst durch die Reparatur aus Punkt 1 (vorher schloss
  sich der Editor). Der E2E fährt gegen eine Temp-DB, der Tag ist dort *immer* neu; mein grüner Lauf hing
  an einer Reihenfolge, die niemand garantiert. Jetzt fragt der Pfad im Fehlfall direkt nach.
- **Die Karte sperrte nur in eine Richtung**: „Löschen" (Ziel) und „Entfernen" (Etappe) gingen zusammen
  hinaus, das DELETE der Etappe lief danach in ein 404. Alle vier Knöpfe binden jetzt beide Quellen.
- **Das geteilte `busy` sperrte die Tag-*Eingabe* mit** – ein `disabled` gewordenes Feld gibt den Fokus ab
  und bekommt ihn nicht zurück, mehrere Tags hintereinander brauchten je einen neuen Klick. Gesperrt ist
  jetzt nur noch das „+".

Dazu zwei Zahlen-Korrekturen des Reviewers, beide nachgerechnet und übernommen: es sind **fünf**
Such-Formulare (nicht sechs) und **35** echte `onSubmit`-Formulare (nicht 39 – vier der `<form>`-Treffer
sind Prosa in Kommentaren). Und meine eigene Rechnung war um eins daneben: 79 − 4 = **75** harmlose, nicht 74.

### Bewusst nicht in dieser Story

- **`VaterWizard`** hält sein `busy` weiter von Hand (`useState`, kein Ref-Gate). Das ist
  [B-53](B-53-wizard-doppelklick.md), nicht diese Story – erwähnt, weil es der letzte mutierende Pfad im
  Vater-Web ohne Primitiv ist.
- **`CatalogAdmin:180`** (leert sein Eingabefeld unbedingt) und **`VaterDashboard:48/73`** (benutzt weiter
  `{loading ? … : …}`, die Falle beißt dort nicht hart, die Kinderliste flackert aber nach jedem Anlegen):
  dieselben Familien, andere Bildschirme. Beides liegt jetzt als
  [B-61](B-61-reste-der-schreib-primitiven-runde.md), nicht als Vermerk in dieser Datei.
- **Ein mechanisches Tor für AK 6** – ausdrücklich verworfen, Entscheidung 3.

## Verlauf

- **2026-08-01** — angelegt aus dem `frontend-reviewer`-Befund zu B-26/E0 (Befund 6), am Code belegt.
- **2026-08-01** — beim Bauen von E5 ([B-43](B-43-frontend-komponententests.md)) auf **fünf Stellen**
  erweitert (Liste oben). `VaterVocab` `TagChip` fiel beim Durchgang auf, `VaterZiele:349` und
  `VaterDashboard:96` erst im `frontend-reviewer`-Lauf danach – beide, weil die erste Zählung nur die
  *Anwesenheit* eines `disabled` prüfte. Offener Punkt 1 ist damit entschieden: die Sperre sitzt seit E5 im
  Primitiv, diese Story ist ihr Nachlauf – und der Vollständigkeits-Beweis dazu, dass danach **jeder**
  mutierende Knopf im Vater-Web `disabled={busy}` trägt. Die Priorität ist dadurch gestiegen: mit
  `VaterDashboard.addChild` steckt jetzt ein Doppelklick-Defekt auf der Startseite darin, nicht nur eine
  fehlende Erfolgsmeldung.
- **2026-08-01** — **gegrillt.** Vor dem Grillen ein drittes Mal gezählt, diesmal maschinell über die
  `disabled`-**Bindung** aller 179 Knöpfe plus alle 35 `onSubmit`-Formulare: die Liste der fünf ist
  bestätigt, keine sechste Stelle. Dabei zwei Randstellen gefunden, die den Zuschnitt entscheiden
  (`TagEditor` hat vier Pfade, `KeyResultForm` steht an zwei Stellen). Sechs Entscheidungen, zwei davon vom
  Menschen (2: Tags bleiben bei Erfolg stumm; 3: AK 6 wird gezählt, nicht bewacht – die Alterung der Zahl
  ist ausdrücklich in Kauf genommen). Die Zeilennummern des Ist-Stands waren um ~20 verrutscht und sind
  nachgeführt.
- **2026-08-01** — **geschätzt** (S, frontend, keine Migration, kein Vertragsbruch) und in Arbeit genommen.
- **2026-08-01** — **abgenommen.** Die fünf Stellen laufen ueber das Primitiv; alle sechs
  Akzeptanzkriterien sind belegt, drei davon mit einzeln gemessener Gegenprobe (Verifikation oben). Der
  wichtigste Fund war eine **eigene** wertlose Zusicherung: AK 4 als „0/1 Etappen" war vor der Reparatur
  ebenso grün und prüfte nichts – der Defekt ist der Doppel-POST, nicht die Zahl der Etappen. Dazu drei
  Defekte, die beim Bauen von selbst herausfielen (die `useAsync`-Falle an zwei Stellen, drei Knöpfe mit
  demselben Namen, verlorene Eingabe nach einem Fehlschlag) und vier Reviewer-Befunde, darunter ein
  Wettlauf, den der neue Test jedes Mal geht. Zwei Reste sind als
  [B-61](B-61-reste-der-schreib-primitiven-runde.md) abgelegt.
