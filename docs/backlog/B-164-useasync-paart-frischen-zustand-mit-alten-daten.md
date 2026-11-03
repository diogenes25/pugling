---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [useAsync behält alte Daten, Fachwechsel zeigt Arten des vorigen Fachs]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
wartet_auf: ""
quelle: frontend-reviewer zu B-157 (2026-08-13, Fund außerhalb des Diffs)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: []
---

# B-164 · Nach einem Wechsel zeigt der Bildschirm für einen Moment frischen Zustand mit alten Daten

`useAsync` behält seine `data` über einen Wechsel der Abhängigkeiten hinweg. Wo daneben ein Wert **synchron**
aus einer Prop abgeleitet wird, gilt für die Zeit einer Abfrage: neuer Zustand, alte Antwort. Im Katalog
heißt das, dass ein Klick die Art des *vorigen* Fachs treffen will.

## User Story

Als *Creator* möchte ich, dass die Liste vor mir zu dem Fach gehört, das im Auswahlfeld steht, damit ein
Klick nicht ins Leere geht und die Rechte-Anzeige nicht kurz das Falsche behauptet.

## Ist-Stand am Code

- **`useAsync` setzt bei einem Dep-Wechsel nur `loading`, nicht `data`**:
  `frontend/src/lib/useAsync.ts:25-39` — `setLoading(true)` (`:27`), `setData` erst im `then` (`:30`). Es gibt
  kein `setData(null)`, und das ist Absicht: `VaterKatalog.tsx:33-35` und `VaterWizard`
  verlassen sich darauf, dass die Liste beim `reload` nach einer Änderung **nicht** ausgehängt wird
  (sonst verlöre `CatalogAdmin` sein eigenes `useState` — der Grund steht dort als Kommentar).
- **Die Paarung im Katalog**: `CatalogAdmin.tsx:35` leitet `subject` **synchron** aus der Prop `subjects` ab,
  `:36-37` lädt `categories` **asynchron** an `[subjectId]`. Zwischen dem Wechsel und der Antwort gehört das
  eine zum neuen, das andere zum alten Fach.
- **Was daraus folgt, gemessen an drei Wegen:**
  1. *eigen → eigen*: die alten Art-Zeilen stehen mit „OK"/„Löschen" da; ein Klick ruft
     `api.updateCategory(neueFachId, alteArtId, …)`. Der Server sucht
     `c.Id == alteArtId && c.SubjectId == neueFachId` (`ExerciseCategoriesController.cs:104-105`) → **404**.
     **Keine falsche Zeile wird geändert** — die Fach-Skopierung der Route fängt das ab. Der Schaden ist ein
     unerklärlicher Fehler, nicht ein Datenverlust.
  2. *fremd → eigen*: die Arten des **fremden** Fachs erscheinen kurz **mit** Bedienelementen, weil das neue
     `subject.isMine` schon `true` ist.
  3. *eigen → fremd*: die eigenen Arten erscheinen kurz als schreibgeschützte Namensliste unter dem Satz
     über ein fremdes Fach.
- **Die Überschrift zählt mit**: `Arten (N)` steht unbedingt und nennt in diesem Moment die alte Zahl.
- **Die Breite ist gemessen, nicht geschätzt:** **110** `useAsync`-Aufrufstellen im Frontend (ohne Tests),
  davon **67** mit einer nicht-leeren Abhängigkeitsliste — die können also mitten im Leben wechseln. Wie
  viele davon zusätzlich einen synchron abgeleiteten Wert daneben zeigen, ist **nicht** ausgezählt; sichtbare
  Kandidaten sind `ChildMaterialSection.tsx:184`, `VaterFachlehrer.tsx:216,253`, `VaterClassTests.tsx:145`
  und `VaterLehrwerke.tsx:157`.

**Alt, nicht neu:** Die Paarung gab es vor [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md)
schon — die Arten wurden auch vorher mit den alten Daten gerendert. B-157 hat den Fall nur *sichtbarer*
gemacht, weil jetzt zusätzlich eine Rechte-Aussage daran hängt. Darum kein `entgangen_bei`.

## Die echte Lücke

Nicht `useAsync` selbst: dass es die Daten hält, ist eine begründete Entscheidung (sonst flackert jede Liste
bei jedem `reload`). Die Lücke ist, dass **kein Aufrufer erfährt, ob die Daten zu den aktuellen
Abhängigkeiten gehören**. `loading` beantwortet „läuft eine Abfrage", nicht „läuft eine **andere**" — genau
die Unterscheidung, an der [B-116](B-116-blaettern-ohne-rueckmeldung.md) schon einmal hing, dort für den
Pager. Dieselbe Familie wie [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md): ein Zustand,
der „aktuell" und „von vorhin" nicht trennt.

## Offene Punkte

1. ~~**Am Aufrufer heilen oder in `useAsync`?**~~ → Entscheidung 1: in `useAsync`, als neues Feld `stale`.
2. ~~**Wenn `stale`: was tut der Aufrufer damit?**~~ → Entscheidung 4: sperren, nicht ausblenden.
3. ~~**Wie viele Aufrufstellen sind wirklich betroffen?**~~ → **gemessen** (siehe unten): 111 Aufrufstellen,
   75 mit nicht-leerer Abhängigkeitsliste, **73** mit Abhängigkeiten, die im Betrieb wechseln können (die
   übrigen 2 hängen allein an der Sitzungs-Id). Die Schätzung der Ausformulierung (110/67) ist damit in der
   Größenordnung bestätigt.
4. ~~**Lohnt ein Wächter?**~~ → Entscheidung 5: nein, und der Grund steht dort.

## Die Messung zu Punkt 3

Gezählt am 2026-09-03 mit einem Klammern-balancierenden Parser über `frontend/src` (ohne Tests und ohne
`useAsync.ts` selbst) — nicht per `grep` auf Zeilen, weil die Mehrzahl der Aufrufe über mehrere Zeilen
geht und eine Zeilenzählung 142 ergibt statt 111.

| | Anzahl |
| --- | --- |
| `useAsync`-Aufrufstellen | 111 |
| davon mit nicht-leerer Abhängigkeitsliste | 75 |
| davon mit Abhängigkeiten, die **im Betrieb** wechseln können | **73** |

**Was die Zahl nicht sagt** — und das ist der Grund für Entscheidung 5: Wie viele dieser 73 daneben
zusätzlich einen *synchron abgeleiteten* Wert zeigen, ist **nicht** mechanisch entscheidbar. 73 ist die
Kandidatenmenge, nicht der Befund. Diese Story schließt drei davon namentlich; die übrigen 70 bleiben
Kandidaten und sind ausdrücklich **nicht** als erledigt zu lesen.

## Angehalten: die Entscheidung, die ich gebraucht hätte

**Der Nachtlauf hat diese Story bewusst zurückgesetzt** (`geschaetzt` → `ausformuliert`), und der Grund ist
eine falsche Prämisse in ihrer eigenen, in derselben Nacht autonom getroffenen Entscheidung 1.

Der Ablauf, damit er nachprüfbar ist statt geglaubt werden zu müssen:

1. Autonom gegrillt und geschätzt (Freigabe 1, `art: Defekt`). Entscheidung 1 wählte „die Heilung liegt in
   `useAsync`, als Feld `stale`" — begründet damit, dass eine Regel je Aufrufer an der zweiten Stelle
   vergessen wird.
2. Gebaut: `stale` samt Dep-Signatur-Vergleich, dazu drei Fälle in `useAsync.test.tsx`. Alle acht Fälle
   grün, zwei rote Proben gemessen (`stale = false` → 2 Fälle fallen; `stale = loading` → **2**, erwartet
   war 1: auch der Fehlerfall trennt, weil `loading` nach dem Fehlschlag zurückfällt).
3. **Dann der Fund**, beim Suchen des ersten Abnehmers: `VaterWizard.tsx:183-201` und `:673-689` tragen
   für exakt diese Frage seit [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md) ein
   **entschiedenes, getestetes und begründetes** Muster — `seitenSchluessel`/`zeilenVeraltet` über einen
   Schlüsselvergleich, mit ausformulierter Begründung, warum Schlüssel und nicht Ladezustände und warum
   das den Fehlerzweig mit deckt. [B-195](B-195-unit-liste-bleibt-waehrend-des-reihenwechsels-waehlbar.md)
   hatte in seinem offenen Punkt sogar darauf hingewiesen; beim Grillen war das nicht geprüft worden.
4. **Der Umbau wurde vollständig zurückgenommen** (`git checkout` auf `useAsync.ts` und seinen Test,
   danach wieder 5/5 grün). Es steht kein Code aus dieser Story im Baum.

**Die offene Frage, und sie ist keine Defekt-Frage mehr:**

> Soll `useAsync` eine querschnittliche Aussage „die Daten gehören zu anderen Abhängigkeiten" tragen —
> oder bleibt es beim Muster am Aufrufer (B-169), das an jeder Stelle einzeln stehen muss?

Beides ist vertretbar, und die Wahl formt den Baum:

| | für den Hook (`stale`) | für das Muster am Aufrufer (B-169) |
| --- | --- | --- |
| Reichweite | 73 Aufrufstellen mit lebendigen Deps könnten es lesen | jede Stelle einzeln, und die zweite wird vergessen — gemessen an B-172 → B-194 |
| Genauigkeit | Dep-**Identität**: ein Leerschlag im Suchfeld zählt als Wechsel | **normalisierter** Schlüssel: `"abc "` ist dieselbe Suche |
| Kosten | ein fünftes Feld an dem Hook, den 111 Stellen benutzen | dieselbe Regel mehrfach ausformuliert |
| Stand heute | gebaut und zurückgenommen | gebaut, getestet, an drei Flächen im Einsatz |

**Warum der Lauf das nicht selbst entscheidet:** „Richtig herstellen" ist bei einem Defekt keine
Produktentscheidung — deshalb darf er ihn grillen. *Hier* verengt der Code die Antwort aber **nicht**: es
gibt zwei gebaute, begründete Wege, und die Wahl ist Architektur. Nach der Regel des Nachtlaufs ist eine
Schätzung, die eine Entscheidung braucht, die ihre `## Entscheidungen` nicht hergeben, der Beweis, dass sie
zu früh entstand ([nachtlauf.md](../nachtlauf.md), Freigabe 8) — also anhalten und notieren.

**Was der Lauf trotzdem geliefert hat**, und es steht unten: die Messung zu offenem Punkt 3 (111/75/73),
den Nachweis, dass der eigene Anlassfall längst zu ist, und mit
[B-167](B-167-katalogliste-zeigt-beim-filterwechsel-alte-treffer.md)/B-195 zwei geschlossene Defekte
derselben Familie — beide ohne diese Entscheidung.

## Der eigene Anlass ist inzwischen zu — nachgesehen, nicht angenommen

Beim Bauen gegen den Code geprüft (2026-09-03), und das Ergebnis verschiebt den Umfang dieser Story:
**Der Katalog-Fall, an dem sie aufgehängt ist, existiert nicht mehr.**

- `CatalogAdmin.tsx:164` filtert die Zeilen inzwischen über `c.subjectId === subjectId`. Im Ladefenster
  stehen die Arten des vorigen Fachs also gar nicht mehr da — nicht „gesperrt", sondern nicht gerendert.
  Damit sind **Akzeptanzkriterium 1 und 2 erfüllt**, und zwar durch einen *besseren* Mechanismus als den
  hier entworfenen: Wo die Zeile ihren Schlüssel selbst trägt, ist der Vergleich an der Zeile schärfer als
  jede Aussage des Hooks.
- Die Überschrift zählt mit demselben Filter (`:167-169`), nennt also nicht mehr die alte Zahl.
- Die Rechte-Aussage über die ganze Liste ist mit B-170 **entfallen** — Rechte hängen seither je Zeile an
  `isOwn`. Ein Satz, der über einer fremden Liste stehen könnte, existiert nicht mehr.
- Und es ist **kein Zufallsstand, sondern gedeckt**: `CatalogAdmin.test.tsx:240` („rendert keine Art, die
  zu einem anderen Fach gehört") fällt, wenn der Filter verschwindet.

**Akzeptanzkriterium 3 ist damit für diese Stelle überholt**, nicht erfüllt: Die Liste flackert dort
sehr wohl — sie ist im Ladefenster leer. Das ist hier aber **richtig**, und der Unterschied ist der Grund,
warum diese Story trotzdem etwas liefert: Beim Fachwechsel gehören die alten Zeilen zu einer *anderen
Sache*; beim `reload` gehören sie zur *selben Sache in einer älteren Fassung*. Nur der zweite Fall
verlangt Nicht-Flackern, und den betrifft der Filter nicht.

**Was bleibt, ist der Anlass ohne den Anlassfall:** die Lücke „kein Aufrufer erfährt, ob die Daten noch
zum Zustand gehören" besteht überall dort weiter, wo die Zeile **keinen** Schlüssel trägt, gegen den man
filtern könnte. Der gemessene Fall dafür ist
[B-167](B-167-katalogliste-zeigt-beim-filterwechsel-alte-treffer.md): Die Treffer einer Filtersuche sind
die Antwort auf eine beliebige Kombination und tragen sie nicht bei sich. Diese Story liefert darum das
**Signal**, B-167 ist sein erster und in diesem Sprint einziger Abnehmer.

**Kein `verworfen`, und das ist eine bewusste Wahl.** Gegenstandslos wäre sie, wenn die Lücke weg wäre;
weg ist nur ihr Beispiel. Ein Signal ohne Abnehmer wäre allerdings toter Code — deshalb ist B-167 in
denselben Sprint gebunden, und ohne ihn wäre diese Story `verworfen` statt gebaut.

## Entscheidungen (Lauf R, 2026-11-03 – auf der Entscheidung des Nutzers vom 2026-10-23)

Der Nutzer hat die Architekturfrage aus „Angehalten" entschieden: „veraltet" lebt **im Hook**, die Prüfungen am Aufrufer
werden darauf zurückgeführt, wo es passt. Was der Lauf daraus autonom entschieden hat (Freigabe 1, `Defekt`):

1. **`useAsync` liefert `stale`**: wahr, solange das Gezeigte zu anderen Abhängigkeiten gehört als den aktuellen – nach einem
   Wechsel bis zur neuen Antwort und weiter, wenn sie scheitert. Ein `reload` macht es nicht wahr. Vergleich je Element
   (`Object.is`). *Kosten:* ein Zustand mehr an allen 113 Aufrufstellen (nachgezählt in B-368; die 111 stammen vom 2026-09-03) (die zwei Setter im selben Rückruf werden
   gebündelt, ein zusätzlicher Render entsteht nicht – Review).
2. **Anfangs gelten die Abhängigkeiten des ersten Renders**, nicht „ohne Daten nie veraltet". Gemessen: die erste Fassung
   (ohne Daten nie veraltet) ließ `filterwechsel-raeumt-den-fehler.spec.ts` (Planbau) fallen – scheitert die erste Suche und
   wechselt danach der Filter, läuft eine *andere* Suche, und der Planbau sagt „Suche läuft…", nicht „0 Treffer". Das alte
   Muster am Aufrufer startete genau so (`useState(filterKey)`).
3. **`PlanPositions` und der Assistent lesen `exercises.stale`**; ihre eigenen `seitenSchluessel`-Zustände samt Effekt
   entfallen. Der Assistent übergibt dafür den normalisierten `filterKey` als Abhängigkeit statt der Rohfelder – „abc " ist
   dieselbe Suche, löst keine Abfrage aus und sperrt nichts (wie im Planbau schon). *Kosten:* `filterKey` muss jedes Feld der
   Abfrage enthalten; er entsteht aus `wizardSearchParams`, also aus der Abfrage selbst.
4. **`useGemerkterFehler` bleibt, wie er ist.** Er beantwortet eine andere Frage (welcher Fehler die Wiederholung
   überlebt), nicht, ob Daten zu den Abhängigkeiten gehören – „wo es passt" passt dort nicht.
5. **Keine weiteren Aufrufer in diesem Sprint**; die übrigen ~70 Kandidaten (oben) bleiben Kandidaten. Kein Tor (Entscheidung
   5 unten gilt fort).

## Entscheidungen (ZURÜCKGEZOGEN — siehe „Angehalten")

Die folgenden fünf Entscheidungen stammen aus dem autonomen Grillen derselben Nacht und stehen hier
**unverändert**, damit die nächste Runde sie nicht neu herleiten muss. Sie gelten aber **nicht**: ihre
erste ruht auf der falschen Prämisse, dass es für diese Frage noch kein entschiedenes Muster gibt.

1. **Die Heilung liegt in `useAsync`, als zusätzliches Feld `stale` — nicht am Aufrufer.**
   Begründung: 73 gemessene Aufrufstellen könnten die Bedingung tragen, und eine Regel je Aufrufer wird in
   diesem Repo nachweislich an der zweiten Stelle vergessen — B-172 hat sieben `<select>` umgestellt und
   den achten übersehen ([B-194](B-194-plandetail-kinderauswahl-ohne-rueckfall.md)).
   **Kosten:** `useAsync` bekommt ein fünftes Feld, das für sich genommen **nichts repariert** — jede
   Aufrufstelle muss es selbst lesen. Der Hook wird also breiter, ohne dass die Breite allein wirkt.
2. **`stale` heißt „die vorliegenden Daten gehören zu anderen Abhängigkeiten", nicht „es lädt".** Verglichen
   wird die Abhängigkeitsliste, aus der die aktuellen Daten stammen, mit der aktuellen (`Object.is` je
   Element, gleiche Länge vorausgesetzt).
   Begründung: `loading` beantwortet „läuft eine Abfrage", `stale` „läuft eine **andere**" — genau die
   Unterscheidung, an der [B-116](B-116-blaettern-ohne-rueckmeldung.md) schon einmal hing. Ein `reload`
   (gleiche Deps, nur `nonce`) ist damit ausdrücklich **nicht** `stale`, und die begründete
   Nicht-Flacker-Eigenschaft bleibt unangetastet.
   **Kosten:** ein zusätzlicher Zustand je Aufrufstelle (die Dep-Liste der geladenen Daten) und ein
   Render mehr je abgeschlossener Abfrage.
3. **`data` wird weiterhin nicht geleert, und `showPlaceholder`/`settled` bleiben unverändert.**
   Begründung: Akzeptanzkriterium 3 verlangt ausdrücklich Nicht-Flackern, und `settled` beantwortet „war
   überhaupt schon ein Versuch durch" — für den Dep-Wechsel die falsche Frage. Die im Verlauf vermerkte
   Kopplung ist damit aufgelöst, ohne `settled` anzufassen: `stale` ist das zweite, getrennte Signal.
   **Kosten:** Der Aufrufer trägt die Verantwortung, `stale` auch zu lesen; ein Tor dafür gibt es nicht.
4. **Der Aufrufer sperrt Bedienelemente und Aussagen, statt Inhalte auszublenden.** Für `CatalogAdmin`:
   Zeilen und Namen bleiben stehen, „OK"/„Löschen" und die Rechte-Aussage setzen aus, und die Überschrift
   nennt keine Zahl, solange sie zum vorigen Fach gehört.
   Begründung: Ausblenden wäre genau das Flackern, das die heutige Bauform vermeidet.
   **Kosten:** ein kurzer Moment, in dem sichtbare Zeilen nicht bedienbar sind — das ist sichtbar gemachte
   Unsicherheit statt lautlos falscher Bedienbarkeit, aber es ist ein Zustand mehr, den die Oberfläche
   zeigen muss.
5. **Kein reflexives Tor in diesem Sprint.**
   Begründung: Die Regel, die ein Tor prüfen müsste, lautet „`useAsync` mit Deps zeigt sein `data` neben
   einem synchron abgeleiteten Wert" — das ist über den Syntaxbaum nicht trennscharf zu fassen, und ein Tor
   darauf wäre entweder blind oder eine Ausnahmeliste, die ihre eigene Regel auffrisst. `CLAUDE.md` verlangt
   „erst messen": gemessen ist die Kandidatenmenge (73), nicht die Befundmenge.
   **Kosten:** Die Regel hängt an Disziplin, und das ist in diesem Repo der teurere Weg. Der Preis wird
   ausdrücklich in Kauf genommen und **benannt**: die 70 nicht angefassten Kandidaten stehen oben, damit
   die nächste Sichtung nicht bei null anfängt.

## Akzeptanzkriterien

1. ~~Nach einem Fachwechsel bietet die Katalogseite keine Bedienelemente an, die sich auf das vorige Fach
   beziehen.~~ → **bereits erfüllt** durch `CatalogAdmin.tsx:164`, gedeckt von `CatalogAdmin.test.tsx:240`.
2. ~~Die Rechte-Aussage erscheint nie über einer Liste, die zu einem anderen Fach gehört.~~ → **entfallen**:
   die Aussage über die ganze Liste gibt es seit B-170 nicht mehr, Rechte hängen je Zeile an `isOwn`.
3. ~~Die Liste flackert nicht.~~ → **überholt für diese Stelle** (Begründung oben); als Regel gilt sie
   weiterhin für das `reload` und steht darum als Kriterium 5.
4. `useAsync` liefert ein Feld, das „die vorliegenden Daten gehören zu anderen Abhängigkeiten" von „es
   läuft eine Abfrage" trennt, und ein Test belegt beide Richtungen einzeln.
5. Die Nicht-Flacker-Eigenschaft beim `reload` bleibt erhalten (der Grund steht als Kommentar in
   `VaterKatalog.tsx`): `reload` ist **nicht** `stale`, ein Dep-Wechsel schon.
6. `showPlaceholder` verhält sich unverändert; die bestehenden fünf Fälle in `useAsync.test.tsx` bleiben
   grün, ohne angefasst zu werden.
7. Das Signal hat in diesem Sprint echte Abnehmer – `PlanPositions` (Zwilling von B-167) und der Assistent (B-169) –, und
   ihre E2E-Specs zum Ladefenster bleiben grün.
8. Scheitert die erste Abfrage und wechseln danach die Abhängigkeiten, gilt das Gezeigte als veraltet (Lauf R,
   Entscheidung 2).

## Schätzung

**Größe: M** — ein neues Feld im meistgenutzten Hook des Frontends plus drei Aufrufstellen. Nicht S, weil
der Hook 111 Aufrufstellen trägt und ein Fehler dort überall wirkt; nicht L, weil der Umfang bewusst auf
drei gemessene Stellen begrenzt ist (Entscheidung 5).

- **`migration: nein`** · **`vertragsbruch: nein`** — reines Frontend, kein Server, kein DTO.
- **`wo: frontend`** — Brille beim Bauen `frontend/CLAUDE.md`, vor der Abnahme `frontend-reviewer`.
- **Risiken:**
  - Der Dep-Vergleich muss `reload` (gleiche Deps, anderer `nonce`) von einem echten Wechsel trennen —
    verwechselt er beides, kehrt genau das Flackern zurück, das die heutige Bauform vermeidet. Dagegen
    steht Akzeptanzkriterium 5 mit einem eigenen Fall.
  - Ein Aufrufer, der `stale` liest, aber `loading` schon anders auswertet, könnte doppelt sperren. Darum
    je Aufrufstelle die bestehende Bedingung mitlesen, nicht blind ergänzen.
- **Angriffsplan:**
  1. `useAsync`: Dep-Signatur der geladenen Daten mitführen, `stale` daraus ableiten.
  2. `useAsync.test.tsx` um die zwei tragenden Fälle erweitern (Dep-Wechsel → `stale`, `reload` → nicht).
  3. ~~`CatalogAdmin` liest `stale`~~ → entfallen, der Fall ist zu (siehe oben). Der Abnehmer ist B-167.
- **Testweg:** `frontend/src/lib/useAsync.test.tsx` (drei neue Fälle: Dep-Wechsel, `reload`, gescheiterte
  Nachfolge-Abfrage). Kein Backend-Test, kein `/smoke-test` — es ändert sich keine Server-Antwort. Der
  Rollengang läuft über die bestehende Playwright-Suite als Regressionsnachweis.

## Verlauf

- **2026-08-13** — angelegt aus dem `frontend-reviewer`-Befund zu B-157 (Fund außerhalb des Diffs).
  **Bewusst nicht in B-157 mitgenommen:** dessen Ziel — die Oberfläche bietet nichts an, was der Server
  verweigert — ist ohne diese Story erfüllt, und die Paarung ist älter als ihr Diff. **Bewusst auch nicht mit
  [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) zusammengelegt**, obwohl dieselbe
  Familie: dort ist es der `VaterWizard` und ein irreführender Text, hier eine andere Datei und ein
  *fehlgeleiteter Aufruf*. Der Ist-Stand ist **selbst am Code nachgesehen** (`useAsync.ts:25-39` gelesen, die
  drei Wege durchgespielt, die Server-Skopierung gegengeprüft) und die Breite **gemessen**: 110
  Aufrufstellen, davon 67 mit Deps.
- 2026-09-02 · **Kopplung vermerkt** (aus dem `frontend-reviewer`-Lauf zu
  [B-203](B-203-platzhalter-luegt-auch-ohne-fehler.md)): `useAsync` hält seit B-203 einen Zustand
  `settled`, der bei einem **Dep-Wechsel nicht zurückgesetzt** wird. Für diese Story ändert das nichts —
  hier ist `data` *nicht*-`null` und veraltet, und für nicht-`null` lieferten alte wie neue Bedingung
  `false`. Wer den `stale`-Gedanken dieser Story baut, muss `settled` aber **mitbedenken**: ein
  Zurücksetzen je Dep-Wechsel bräuchte denselben Dep-Signaturvergleich, den diese Story ohnehin verlangt.
  Bewusst **nicht** vorab gebaut — zwei halbe Lösungen desselben Vergleichs wären schlimmer als eine.
- **2026-09-03** — Nachtlauf: autonom gegrillt, geschätzt, **gebaut und wieder zurückgenommen**; Stufe
  von `geschaetzt` auf `ausformuliert` **zurückgesetzt**, `wartet_auf` gesetzt. Der vollständige Ablauf
  und die offene Frage stehen oben unter „Angehalten". Geliefert hat der Lauf trotzdem: die Messung zu
  offenem Punkt 3 (111 Aufrufstellen / 75 mit Deps / 73 mit lebendigen Deps) und den Nachweis, dass der
  Anlassfall im Katalog längst geschlossen und durch `CatalogAdmin.test.tsx:240` gedeckt ist.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, weiter angehalten. Inzwischen gibt es **drei** Muster statt zwei: Prüfung am Aufrufer (`VaterWizard.tsx:203-204`, `PlanPositions.tsx`), und den Schlüssel-Hook `lib/useGemerkterFehler.ts` (B-227), dessen Kopf ausdrücklich nicht vorgreift. Die Architekturfrage (querschnittliches `stale` im Hook vs. Muster am Aufrufer) wird damit dringlicher – Tagesordnung fürs Grillen.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-2 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): „veraltet“ lebt **im Hook** – `useAsync` bekommt ein querschnittliches `stale`-Feld; die Prüfungen am Aufrufer (`seitenSchluessel`/`zeilenVeraltet` in `VaterWizard.tsx`, `PlanPositions.tsx`) und der Schlüssel-Hook `useGemerkterFehler` werden darauf zurückgeführt, wo es passt. Damit ist die Story nicht mehr angehalten (`wartet_auf` geleert) und als `Defekt` autonom grill- und schätzbar. Wahrscheinlich M – vor dem Bau teilen, wenn der Rückbau der Aufrufer mehr als eine Handvoll Stellen trifft.
- **2026-11-03** — Lauf R, Teil 1: auf der Nutzer-Entscheidung vom 2026-10-23 autonom gegrillt (Freigabe 1), gebaut. Rote
  Probe: fünf neue Fälle in `useAsync.test.tsx` **5 failed** (erwartet 5, das Feld fehlte). Mutanten am Hook: „`stale` = `loading`
  mit Daten" **2 failed** (Neuladen, Fehlerzweig), „immer falsch" **2 failed**, „Liste per Identität" **3 failed**, „Deps auch
  beim Fehler nachgezogen" **1 failed**. Nach dem Umzug der Aufrufer fiel in den drei Ladefenster-Specs **1 failed**
  (`filterwechsel-raeumt-den-fehler.spec.ts`, Planbau) – die Startsemantik war falsch (Entscheidung 2); ein sechster Fall
  dafür war gegen die erste Fassung **1 failed**, danach grün. Aufrufer-Mutanten „liest `stale` nicht": Assistent **3
  failed** (`assistent.spec.ts:126`, `:165`, Filterwechsel), Planbau **2 failed** eigene (`planbau-ladefenster.spec.ts:43`,
  `:76`). Danach die drei Specs **9 passed**, Vitest voll **627/627**, `tsc -b` sauber. Die bestehenden fünf Fälle des Hooks
  unangetastet und grün (AK 6).
- **2026-11-03** — Review `frontend-reviewer`: kein 🔴. 🟡 die Wiederholung nach gescheiterter geänderter Abfrage trug keinen Fall (Mutante „`reload` zieht Deps nach" blieb grün) → Fall 3 erweitert, Mutante **1 failed**, „Deps beim Start nachgezogen" **3 failed** (zählt). 🟡 Entscheidung 3 (Leerschlag im Assistenten) ohne Zusicherung → Zwilling in `assistent.spec.ts` (Anfragen gezählt, Kästchen bedienbar), Mutante „Rohfelder als Deps" **1 failed** an „keine neue Suche" (zählt; ein erster Mutantenlauf brach an einem Server-Startfehler ab und ist nicht gewertet). 🟡 der Kommentar an Fall 1 nannte die `loading`-Mutante, die er nicht fängt → berichtigt (nur Kommentar). 🟢 kein zusätzlicher Render (Entscheidung 1 berichtigt). **Zähler Teil 1: 2.**
- **2026-11-03** — Abnahme Lauf R, Teil 1: Vitest **627/627**, `tsc -b` sauber, E2E-Gesamtlauf **108 passed** (`exit=0`, nach dem letzten Code-Edit; darin der neue Zwilling). Rollengang: Vater maschinell – `assistent.spec.ts` und `planbau-ladefenster.spec.ts` fahren das Ladefenster, den Fehlerzweig und den Leerschlag im echten Browser, `filterwechsel-raeumt-den-fehler.spec.ts` den Wechsel nach gescheiterter Suche; Creator und Sohn Regressionszeugen (ihre Specs grün, kein Pfad von ihnen geändert außer dem geteilten Hook). **Abgenommen**, Commit siehe Protokoll ([Lauf R](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf R, Teil 2 (frischer `frontend-reviewer`, `git show 66ff94d`, read-only): **Entgleitung** → [B-368](B-368-stale-kommentar-und-zaehlung.md) (🟡 ×2: Kommentar nennt den entfernten Effekt, „111" statt 113). Sonst geprüft: jede Transition mit Fall (Mount, Erfolg, Wechsel, Fehler, Wiederholung), Aufrufer-Mutanten fallen in den Ladefenster-Specs, `wizardFilterKey` deckt alle Felder (`wizardSearch.test.ts:96`), `kriterien` nur primitiv; alle 13 Dateien `i/lf`, 0 CR.
