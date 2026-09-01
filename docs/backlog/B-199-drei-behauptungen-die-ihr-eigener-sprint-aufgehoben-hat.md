---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/doku]
aliases: [Kommentar begruendet mit aufgehobener Praemisse, Testkommentar verspricht Deckung]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
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

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf). Punkt 3 ist der einzige
  der drei, der eine Entscheidung des Menschen braucht; die anderen beiden sind Handwerk.
