---
tags: [typ/story, status/in-arbeit, bereich/frontend]
aliases: [Lade… bleibt nach Fehler stehen, data === null ohne loading, Platzhalter widerspricht Banner]
status: in-arbeit
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-172, Fund des frontend-reviewer
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-184 · Vier Platzhalter zeigen einen Fehler dauerhaft als „Lade…"

`frontend/CLAUDE.md:110` nennt die Falle in einer Richtung: wer `{loading ? "Lade…" : rows}` schreibt, hängt
bei jedem `reload` alle Zeilen aus. Die Antwort darauf ist `loading && data === null` — und **37 von 41**
Platzhaltern im Vater-Web halten sich daran. Vier tun es nicht und prüfen allein `data === null`.

## User Story

Als **Vater** möchte ich, dass ein Bildschirm nicht gleichzeitig „das ging schief" und „ich lade noch" sagt,
damit ich weiß, ob ich warten oder handeln soll.

## Ist-Stand am Code (am 2026-09-01 selbst nachgeprüft)

Alle vier Stellen bestätigt, und in allen vier steht das Fehlerbanner **unmittelbar darüber** — der
Widerspruch ist also nicht theoretisch, sondern in einem Blickfeld:

| Fundort | Fehler wird gezeigt | Platzhalter prüft | Was dauerhaft „Lade…" zeigt |
|---|---|---|---|
| `ChildMaterialSection.tsx:54` | `:50` (`books.error`) | `books.data === null` | die Bücherliste des Kindes |
| `ChildMaterialSection.tsx:114-116` | `:50` (dieselbe Quelle) | `books.data ?` | das Fachlehrer-Treffer-Panel |
| `VaterKind.tsx:94` | `:88` (`interests.error`) | `interests.data === null` | der Interessen-Editor |
| `VaterLehrwerke.tsx:461` | `:460` (`units.error`) | `units.data === null` | die Unit-Liste einer Reihe |

**Die Begründung der vier Stellen ist richtig und bleibt bestehen.** Sie steht als Kommentar in
`VaterKind.tsx:89-93`: auf `data` zu prüfen statt auf `loading` verhindert, dass ein `reload` nach dem
Speichern das Formular kurz aus dem DOM nimmt und seinen Zustand samt Bestätigung mitreißt. Genau darum
darf der Fix diese Bedingung **nicht** durch `loading && data === null` ersetzen.

**Die offene Frage der Aufnahme ist damit beantwortet, und zwar anders als vermutet:** Ob die zwei Stellen
mit `useRef`-Bezugspunkt ihre Begründung noch brauchen, ist **gegenstandslos** — die zwei Bedingungen sind
orthogonal. Während eines `reload` ist `data` ohnehin ungleich `null`, ein zusätzliches `&& !error` ändert
dort gar nichts; es greift ausschließlich im Fall „erste Ladung gescheitert". Der Fix ist damit rein
additiv zur bestehenden Begründung, nicht ihr Ersatz.

## Die echte Lücke

`data === null` trägt zwei Bedeutungen: „es kommt noch etwas" und „es kommt nichts mehr". `useAsync` kennt
den Unterschied (`error`), aber jede Aufrufstelle muss ihn selbst herleiten — und vier von 41 haben es
nicht getan. Das ist eine Regel, die an Disziplin hängt, und ihr Bruch ist unsichtbar.

## Entscheidungen

1. **Der richtige Zustand wird von `useAsync` geliefert, nicht an 41 Stellen hergeleitet.** Der Hook bekommt
   ein abgeleitetes `showPlaceholder` (`data === null && error === null`). *Begründung:* Das ist der
   Unterschied zwischen einer Regel, die man kennen muss, und einer, die dasteht — die Haltung des Repos
   („mechanische Tore statt Disziplin"). Der Name sagt seinen Einsatzort, statt einen Zustand zu benennen,
   den der Aufrufer erneut deuten muss. *Kosten:* Ein Feld mehr an einem Typ, den 105 Aufrufstellen
   benutzen, von denen zunächst vier es lesen. Additiv, keine bricht.
2. **Das ist zugleich der einzige Weg zu einer roten Probe.** *Begründung:* Alle vier Stellen sitzen in
   Seitenkomponenten, die am Netz hängen; kein Test dieses Frontends mockt `../lib/api`. Am Hook selbst ist
   der Fehlerfall dagegen in drei Zeilen erreichbar (`renderHook` mit einer scheiternden Funktion, Vorbild
   `useOncePerKey.test.ts`). Ohne diese Entscheidung wäre der Testweg entweder ein E2E mit abgeschaltetem
   Server oder keiner. *Kosten:* Die vier Aufrufstellen selbst bleiben ungetestet — abgedeckt ist die
   *Bedingung*, nicht ihr Einbau. Das ist ausgesprochen, nicht kaschiert.
3. **`frontend/CLAUDE.md:107-110` wird nachgezogen.** Die Regel nennt heute nur `loading && data === null`
   und kennt den Formular-Fall nicht, um den es hier geht. *Begründung:* Eine Regel, die einen der beiden
   zulässigen Fälle verschweigt, erzeugt genau diese vier Abweichungen. *Kosten:* Zwei Zeilen in einer
   Datei mit Kontextbudget — im selben Durchgang gegengerechnet (`context-budget.sh`).
4. **Kein Wächter.** *Begründung:* „Platzhalter ohne Fehlerprüfung" ist syntaktisch nicht trennscharf von
   einem legitimen `data === null` (etwa dort, wo daneben gar kein Abruf steht). Ein Tor, das an 105
   Stellen raten muss, produziert Ausnahmelisten statt Sicherheit. Die Regel hängt stattdessen daran, dass
   das *richtige* Feld bequemer ist als die Herleitung. *Kosten:* Ehrlich benannt — eine fünfte Stelle
   könnte wieder danebengreifen, und nichts fängt das.

## Akzeptanzkriterien

1. `useAsync` liefert einen Zustand, der „noch keine Daten, kein Grund" von „keine Daten, weil gescheitert"
   trennt.
2. Scheitert die erste Ladung an einer der vier Stellen, steht dort **kein** „Lade…" mehr — nur das
   Fehlerbanner.
3. Ein `reload` nach dem Speichern nimmt das Formular weiterhin **nicht** aus dem DOM (die Begründung der
   vier Stellen bleibt gewahrt).
4. Ein Test am Hook deckt 1 und 3; die rote Probe nennt erwartete und gemessene Zahl.
5. `frontend/CLAUDE.md` nennt beide zulässigen Formen, und das Kontextbudget ist danach eingehalten.

## Schätzung

- **Größe: S.** Anker: ein abgeleitetes Feld plus Doku-Kommentar im Hook, vier Einzeiler an den
  Aufrufstellen, eine neue Testdatei mit drei Fällen, zwei Zeilen Doku. Vergleichbar mit B-002 plus
  Testdatei.
- **Migration: nein**, **Vertragsbruch: nein** — reine Frontend-Ableitung.
- **Testweg:** neue Datei `frontend/src/lib/useAsync.test.ts` (`renderHook` mit `<AuthProvider>`-Wrapper —
  `useAsync` ruft `useAuth`, das außerhalb des Providers wirft, `auth.tsx:97`). Fälle: erste Ladung läuft →
  Platzhalter an; Ladung scheitert → Platzhalter aus, `error` gesetzt; `reload` bei vorhandenen Daten →
  Platzhalter bleibt aus. Gefahren mit `npm run test` unter `frontend/`.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu B-172. Die Zählung (37 von 41 richtig,
  vier nicht) ist an diesem Tag gemessen; nicht geprüft ist, ob die Begründung der vier Stellen noch
  gilt — darum `unverifiziert`.
- 2026-09-01 · **`idee` → `ausformuliert`** (Nachtlauf). Alle vier Fundorte am Code bestätigt, dazu die
  Zeile des jeweils **darüberstehenden** Fehlerbanners — der Widerspruch steht in einem Blickfeld.
  Die offene Frage der Aufnahme („brauchen die vier ihre Begründung noch?") ist **gegenstandslos**: die
  beiden Bedingungen sind orthogonal, der Fix ist additiv. `unverifiziert` entfernt.
- 2026-09-01 · **`ausformuliert` → `gegrillt`** (Nachtlauf, Freigabe 1 — `art: Defekt`). Vier
  Entscheidungen mit Begründung und Kosten. Entscheidung 4 sagt ausdrücklich, was **ungedeckt** bleibt,
  statt einen Wächter zu behaupten, der raten müsste.
- 2026-09-01 · **`gegrillt` → `geschaetzt`** (Nachtlauf). S, Testweg als benannte neue Datei mit drei
  Fällen und dem `AuthProvider`-Fallstrick vorab benannt.
- 2026-09-01 · **`geschaetzt` → `in-arbeit`** (Nachtlauf, Sprint 1, Commit `e479d83`). `useAsync` liefert
  `showPlaceholder`; die vier Aufrufstellen lesen es. **Rote Probe: erwartet 1 von 3 rot, gemessen
  `1 failed | 2 passed`** — das Feld wurde dafür zuerst mit der *heutigen*, falschen Bedingung
  (`data === null`) gebaut, damit die Zusicherung an einer Verhaltensaussage fällt und nicht an einem
  fehlenden Symbol. `frontend/CLAUDE.md` nennt jetzt beide zulässigen Formen; das Kontextbudget ist im
  selben Durchgang gegengerechnet (die Datei ist netto **16 Bytes kleiner** als vorher — zwei
  Erzähl-Passagen zu B-25 und B-106 sind auf ihre Aussage gekürzt, ihre Begründung steht in `docs/`).
