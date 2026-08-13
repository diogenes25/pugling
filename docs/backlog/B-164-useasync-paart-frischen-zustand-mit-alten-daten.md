---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [useAsync behält alte Daten, Fachwechsel zeigt Arten des vorigen Fachs]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-157 (2026-08-13, Fund außerhalb des Diffs)
unverifiziert: false
grund: ""
ersetzt_durch: []
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

1. **Am Aufrufer heilen oder in `useAsync`?** Empfehlung: **in `useAsync`**, weil 67 Aufrufstellen die
   Bedingung tragen könnten und eine Regel je Aufrufer nach der Erfahrung dieses Repos an der zweiten Stelle
   vergessen wird. Denkbar ist ein zusätzliches Feld, das sagt, zu welchen Abhängigkeiten die vorliegenden
   Daten gehören („`stale`" o. Ä.) — ohne `data` zu leeren, damit die begründete Nicht-Flacker-Eigenschaft
   erhalten bleibt.
2. **Wenn `stale`: was tut der Aufrufer damit?** Empfehlung: Bedienelemente sperren statt Inhalte
   ausblenden — Ausblenden wäre genau das Flackern, das die heutige Bauform vermeidet. Für `CatalogAdmin`
   heißt das: Zeilen und Namen stehen lassen, „OK"/„Löschen" und die Rechte-Aussage aussetzen.
3. **Wie viele Aufrufstellen sind wirklich betroffen?** Vor jeder Regel auszählen (`CLAUDE.md`: „Neue Regel
   scharf stellen? Erst messen."). Die 67 sind die Obergrenze, nicht der Befund.
4. **Lohnt ein Wächter?** Empfehlung: erst nach Punkt 3. Eine reflexive Regel „`useAsync` mit Deps darf sein
   `data` nicht neben einem synchron abgeleiteten Wert zeigen" ist wahrscheinlich nicht trennscharf
   formulierbar; das gehört gemessen, nicht geraten.

## Akzeptanzkriterien (Entwurf)

1. Nach einem Fachwechsel bietet die Katalogseite keine Bedienelemente an, die sich auf das vorige Fach
   beziehen.
2. Die Rechte-Aussage („gehört zum Grundbestand" / „hat jemand anderes angelegt") erscheint nie über einer
   Liste, die zu einem anderen Fach gehört.
3. Die Liste **flackert nicht**: die bisherigen Namen bleiben sichtbar, solange die neuen laden.
4. Ein Test deckt den Übergang ab; die rote Probe belegt, dass er den heutigen Stand fängt.
5. Falls die Behebung in `useAsync` landet: die Nicht-Flacker-Eigenschaft beim `reload` bleibt erhalten (der
   Grund steht als Kommentar in `VaterKatalog.tsx`) — ein Test hält beides.

## Verlauf

- **2026-08-13** — angelegt aus dem `frontend-reviewer`-Befund zu B-157 (Fund außerhalb des Diffs).
  **Bewusst nicht in B-157 mitgenommen:** dessen Ziel — die Oberfläche bietet nichts an, was der Server
  verweigert — ist ohne diese Story erfüllt, und die Paarung ist älter als ihr Diff. **Bewusst auch nicht mit
  [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) zusammengelegt**, obwohl dieselbe
  Familie: dort ist es der `VaterWizard` und ein irreführender Text, hier eine andere Datei und ein
  *fehlgeleiteter Aufruf*. Der Ist-Stand ist **selbst am Code nachgesehen** (`useAsync.ts:25-39` gelesen, die
  drei Wege durchgespielt, die Server-Skopierung gegengeprüft) und die Breite **gemessen**: 110
  Aufrufstellen, davon 67 mit Deps.
