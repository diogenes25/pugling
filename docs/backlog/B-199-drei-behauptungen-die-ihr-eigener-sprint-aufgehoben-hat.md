---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/doku]
aliases: [Kommentar begruendet mit aufgehobener Praemisse, Testkommentar verspricht Deckung]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-162, B-184, B-174]
wartet_auf: ""
---

# B-199 · Drei Behauptungen, die ihr eigener Sprint aufgehoben hat

Alle drei sind dieselbe Klasse wie der Kommentar, den `58cca00` schon behoben hat: **eine Begründung, die
auf einen Zustand zeigt, den es nicht mehr gibt.** Sie stehen hier zusammen, weil sie eine Wurzel und
einen Arbeitsgang haben.

## User Story

Als **Entwickler** möchte ich, dass ein Kommentar eine Entscheidung mit etwas begründet, das noch gilt,
damit ich nicht auf eine Freigabe baue, die zurückgenommen wurde.

## Ist-Stand am Code

### 1. `VaterWizard.tsx:195-197` — die gefährlichste, weil sie wie eine Freigabe klingt

> „`exercises.loading && data !== null` wäre heute gleichwertig (die Abfrage ruft nirgends `reload()`)"

Derselbe Commit `e479d83` hat den „Erneut suchen"-Knopf gebaut und `onRetry={exercises.reload}` verdrahtet
(`:550`). Der Nebensatz ist damit **falsch**, und tragend: B-162s eigener Ist-Stand hält fest, dass die
verworfene Variante „mit einem `reload()` das Gate fälschlich geschlossen" hätte. Das Verhalten ist
korrekt (nachgerechnet) — aber die nächste Änderung liest hier eine Erlaubnis, die die Story selbst
aufgehoben hat.

### 2. `useAsync.test.tsx:60-73` — der Kommentar verspricht Deckung, die der Fall nicht hat

Er sagt: „Ein `reload` nach dem Speichern darf das Formular NICHT aus dem DOM nehmen … dieser Fall belegt
es." Belegt wird es nicht: Die Zusicherung läuft **nach** dem `act`, in dem `Promise.resolve([++n])` schon
aufgelöst ist. Eine Fassung von `useAsync`, die beim `reload` `setData(null)` täte — also genau der Ausbau,
der das Formular aushängt — bliebe grün, weil der Endzustand wieder Daten trägt. Für echte Deckung müsste
**zwischen** `reload()` und der Auflösung gemessen werden (`deferred`, wie in den Fällen 1 und 2).

### 3. `CatalogAdmin.tsx` — B-174s Entscheidung 3 ist stillschweigend entfallen

Entscheidung 3 versprach: „Der Leer-Zweig bleibt, wird aber nachgeschärft — ‚Keine Arten' darf nur greifen,
wenn wirklich geladen wurde", samt angekündigtem Preis „ein dritter sichtbarer Zustand". Gebaut wurde nur
der Banner; `CategoryRows` steht unverändert auf `if (!categories?.length) return null`.

**Das Akzeptanzkriterium hält trotzdem** — aber über die Überschriftenzahl (`Arten (n)` erscheint bei
`error` nicht), die die Story nirgends als tragendes Merkmal nennt. Der Verlauf protokolliert die
Abweichung von Entscheidung 1 ausdrücklich, die von Entscheidung 3 gar nicht.

## Die echte Lücke

Ein Sprint, der Kommentare als Träger seiner Begründungen benutzt, muss sie mitziehen, wenn er ihre
Prämisse aufhebt. Kein Tor kann das prüfen — es ist genau die Sorte Aussage, die niemand nachliest, weil
sie als Begründung daherkommt und nicht als Behauptung.

## Offene Punkte

1. Fall 2 nur im Kommentar korrigieren oder den Testfall wirklich decken? Empfehlung: **decken** — das
   `deferred`-Muster steht in derselben Datei, es kostet vier Zeilen, und die Begründung der vier
   `showPlaceholder`-Aufrufstellen hängt daran.
2. Fall 3: Entscheidung 3 nachbauen oder sie ausdrücklich zurücknehmen? Das ist eine
   **Produktentscheidung** über einen sichtbaren Zustand — sie gehört in den Dialog, nicht in diesen Lauf.

## Entscheidungen

Autonom gegrillt am 2026-10-18 unter Freigabe 1 (`art: Aufräumen`). Vorher gegen den Code nachgesehen:

- **Fall 2 ist seit `e9bc135` erledigt** („Reviewer eingearbeitet; zwei Behauptungen widerlegt" – der Reviewer
  dieses Sprints hat den zuerst genannten Hash `81eb60c` als falsch nachgewiesen: dort stand noch die
  schwache Fassung): `useAsync.test.tsx` misst beim
  Fall „bringt ihn beim Neuladen vorhandener Daten nicht zurück" inzwischen **zwischen** `reload()` und der
  Auflösung mit zwei `deferred` (`loading` wahr, `data` nicht `null`, `showPlaceholder` falsch) – genau die
  Deckung, die dieser Fall verlangte. Der Ist-Stand oben beschreibt den Stand vom 2026-09-02.

1. **Fall 1: der Kommentar in `VaterWizard.tsx` sagt jetzt, dass es die Wiederholung derselben Suche gibt** („Erneut
   suchen" ruft `exercises.reload()`, B-162) und dass die Ladezustands-Variante genau dort fälschlich sperrte.
   *Begründung:* Die alte Fassung las sich als Freigabe für die Variante, die B-162 selbst verworfen hat.
   *Kosten:* keine; Kommentar.
2. **Fall 2: nichts mehr zu tun** – siehe oben; Verlauf hält es fest.
3. **Fall 3 wird herausgelöst** → [B-234](B-234-dritter-zustand-der-arten-liste.md) (`Frage`): ob ein dritter
   sichtbarer Zustand nachgebaut oder B-174s Entscheidung 3 zurückgenommen wird, ist eine Produktentscheidung
   (Offener Punkt 2) und unter Freigabe 1 nicht Sache des Laufs.

## Akzeptanzkriterien

1. `VaterWizard.tsx` behauptet nicht mehr, die Abfrage rufe nirgends `reload()`. *Beleg:* Lesebeleg (`grep
   "ruft nirgends"` leer).
2. Fall 2 ist als erledigt belegt (Commit genannt), Fall 3 als eigene Frage abgelegt.
3. Keine rote Probe – Kommentar und Doku; ausdrücklich so beschriftet.

## Schätzung

**Größe: XS** – ein Kommentar, eine neue Story. Anker: B-02. **Testweg:** `tsc -b`, `vitest run` unverändert grün.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf). Punkt 3 ist der einzige
  der drei, der eine Entscheidung des Menschen braucht; die anderen beiden sind Handwerk.
- **2026-10-18** — **ausformuliert → gegrillt → geschaetzt** im Nachtlauf (zweiter Lauf der Nacht 2026-10-17,
  Sprint 4), autonom unter Freigabe 1: Fall 2 am Code als erledigt vorgefunden (`e9bc135`), Fall 3 →
  [B-234](B-234-dritter-zustand-der-arten-liste.md); bleibt Fall 1, XS.
- **2026-10-18** — **geschaetzt → abgenommen** (Sprint 4). Fall 1: Kommentar in `VaterWizard.tsx` berichtigt
  (`grep "ruft nirgends"` leer). Keine rote Probe (Kommentar; so beschriftet). Belege: `tsc -b` sauber, `vitest run`
  389/389 · `frontend-reviewer`: **ein Should-fix** – der zuerst genannte Hash für Fall 2 (`81eb60c`) war falsch,
  richtig ist `e9bc135` (an drei Stellen berichtigt; Doku, kein Zähler) – und ein Nit zur Wortwahl („könnte …
  fälschlich sperren", übernommen); die Begründung des neuen Kommentars am Code bestätigt (`useAsync.ts:47/50`,
  `onRetry` `:576`). **Rollengang:** kein sichtbares Verhalten – Regressionszeugen. Commit: siehe Protokoll
  `docs/pm-sitzung-2026-10-17-nacht.md`, „Sprint 4".
