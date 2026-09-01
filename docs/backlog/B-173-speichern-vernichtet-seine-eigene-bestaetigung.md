---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/creator]
aliases: [Gespeichert fuer einen Frame, Zeile faellt aus dem Filter, Speichern sieht wie Loeschen aus]
status: in-arbeit
prio: P2
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-123
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-123]
wartet_auf: ""
---

# B-173 · Ein erfolgreiches Speichern vernichtet seine eigene Bestätigung

Die Bestätigung lebt **in** der Zeile, die sie bestätigt. Ändert das Speichern einen Wert, nach dem gefiltert
oder sortiert wird, verlässt die Zeile die Liste — und nimmt „Gespeichert." mit. Das Speichern sieht dann aus
wie ein Löschen.

## User Story

Als **Creator** möchte ich, dass eine erfolgreiche Änderung als solche erkennbar bleibt, auch wenn die Zeile
danach nicht mehr in meine Filterung passt, damit ich ein geglücktes Speichern nicht für ein Löschen halte
und es ein zweites Mal versuche.

## Ist-Stand am Code (am 2026-09-01 selbst nachgeprüft)

Die Behauptung der Aufnahme hält; die Zeilennummern sind nachgezogen.

| Glied der Kette | Belegt |
|---|---|
| Die Liste ist **gefiltert** | `VaterLehrwerke.tsx:112-114`, Abhängigkeiten `[applied, subjectId, publisherId, schoolTypes, grade]` |
| Speichern löst `list.reload` aus | `:187` (`onChanged={list.reload}`) → `SeriesForm onSaved` `:311` |
| Der Banner der Zeile sitzt **in** der Zeile | `:269` — ein `<tr>` innerhalb von `SeriesRow` |
| Fällt die Zeile aus dem Ergebnis, wird `SeriesRow` unmontiert | Folge aus `:172-178` (die Liste rendert nur, was der Server zurückgab) |
| Der Leer-Satz zieht zwei Fälle zusammen | `:177` — „Noch kein Lehrwerk. Lege unten eines an." |
| Die Sortier-Variante | Server sortiert `OrderBy(s => s.Name).ThenBy(s => s.Id)`, `TextbookSeriesController.cs:93` — ein Umbenennen sortiert die Zeile um |

**Ein Unterschied zu [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md), der beim Bauen
zählt:** Hier bleibt das Formular nach dem Speichern absichtlich **offen**, und sein `useRef`-Bezugspunkt
zieht aus der Server-Antwort nach (`:313-318`). Der Verlust entsteht nicht durch das Schließen des
Formulars, sondern durch das Verschwinden der **Zeile**. Beide Stories haben dieselbe Wurzel („die
Bestätigung lebt in dem, was verschwindet"), aber nicht dieselbe Ursache — und darum je eine eigene Story.

### Die zweite Hälfte, und warum sie hierher gehört

„Noch kein Lehrwerk. **Lege unten eines an.**" ist im Filterfall nicht bloß unscharf, sondern eine falsche
**Handlungsanweisung**: Die Reihe existiert, sie ist nur ausgefiltert — wer der Anweisung folgt, legt eine
Dublette an. Dieselbe Klasse wie B-162 und B-111. Sie sitzt in denselben zwanzig Zeilen wie die erste
Hälfte und hebt den Schaden von „unklar" auf „irreführend".

## Die echte Lücke

Zwei Zustände tragen je zwei Bedeutungen: Die Bestätigung sagt „gespeichert" **oder** „die Zeile ist weg",
und der Leer-Satz sagt „es gibt keine" **oder** „keine passt zum Filter". Beide Male fehlt der Zustand
daneben, der sie trennen würde — und beide Male existiert er schon (die Meldung; die eingestellten Filter).

## Entscheidungen

1. **Die Bestätigung wandert auf Seitenebene** — dieselbe Bewegung wie in B-151, Entscheidung 1.
   `SeriesRow` und `SeriesForm` melden ihren Text nach oben; die Seite hält ihn und rendert **einen**
   `StatusBanner` an ihrem eigenen Rand. *Begründung:* Ein Ort, der nicht mitverschwindet, ist die einzige
   Fassung, die auch die Sortier-Variante (Umbenennen) und das Löschen abdeckt — „Formular zuerst
   schließen" hinge weiter an der Lebensdauer der Zeile. Das Muster steht in derselben Datei bereits
   (`NewSeries`, `:747`). *Kosten:* Die Meldung steht weiter von der Zeile entfernt, auf die sie sich
   bezieht; bei mehreren schnell aufeinanderfolgenden Aktionen sieht man nur die letzte.
2. **Der Leer-Satz unterscheidet gefiltert von leer.** Steht mindestens ein Filter, sagt er, dass die
   Filterung nichts trifft, und bietet **nicht** das Anlegen an. *Begründung:* Ohne das bliebe die falsche
   Handlungsanweisung stehen, die eine Dublette im geteilten Katalog erzeugt. *Kosten:* Die Bedingung
   „steht ein Filter?" muss aus fünf Zustandswerten abgeleitet werden — das wird eine **reine Funktion**,
   siehe Testweg, statt eines Ausdrucks im JSX.
3. **Der Fund gilt als eine Story, nicht als zwei.** *Begründung:* Beide Hälften sitzen in denselben zwanzig
   Zeilen und werden in einem Schnitt behoben; die Trennung von B-151 läuft an der **Ursache** (Zeile vs.
   Formular), nicht an der Datei. *Kosten:* Die Story ist M statt zweimal S.
4. **Keine weitere Liste wird in dieser Story vermessen** (offener Punkt 4 der Aufnahme). *Begründung:*
   Für die *Lese*-Hälfte dieser Familie liegt die Zahl bereits vor (41 von 105, siehe B-174); für die
   *Schreib*-Hälfte wäre eine eigene Erhebung nötig, und sie würde diese Story an eine Messung binden,
   deren Ergebnis sie nicht braucht. *Kosten:* Eine dritte Liste mit zeileninterner Bestätigung könnte
   existieren und bliebe unentdeckt — ausgesprochen, nicht kaschiert.

## Akzeptanzkriterien

1. Ändert ein Speichern einen Wert so, dass die Zeile aus der eingestellten Filterung fällt, bleibt die
   Erfolgsmeldung sichtbar.
2. Dasselbe gilt für ein Umbenennen, das die Zeile umsortiert, und für das Löschen einer Reihe.
3. Ist die Liste leer, **weil** gefiltert wird, sagt der Text das und fordert nicht zum Anlegen auf.
4. Ist die Liste ohne jeden Filter leer, bleibt der heutige Satz samt Anweisung.
5. Ein E2E deckt 1 und 3; eine reine Funktion für „steht ein Filter?" ist einzeln getestet. Die rote Probe
   nennt erwartete und gemessene Zahl.

## Schätzung

- **Größe: M.** Anker: eine Datei, ein hochgezogener Meldungszustand über drei Meldewege (Zeile, Formular,
  Löschen), ein zweiter Renderzweig für den Leer-Fall, eine exportierte reine Funktion, dazu ein E2E-Spec
  und ein Unit-Block. Größer als B-151 (S) genau um die zweite Hälfte und die drei Meldewege.
- **Migration: nein**, **Vertragsbruch: nein** — reine Oberfläche; die Sortierung des Servers bleibt.
- **Testweg:**
  - `frontend/src/vater/VaterLehrwerke.test.tsx` — neuer Block für die exportierte reine Funktion:
    jeder der fünf Filter einzeln, und der filterlose Fall.
  - `frontend/e2e/lehrwerk-bearbeiten.spec.ts` (bestehende Datei) — neuer Fall: Verlagsfilter setzen,
    Reihe bearbeiten, Verlag entfernen, speichern → „Gespeichert." ist sichtbar **und** der Leer-Text
    nennt die Filterung. Das ist zugleich der **Rollengang** (`pm-loop` Step 6).

## Offene Punkte (aus der Aufnahme — hier beantwortet)

1. ~~Reproduzieren, bevor gebaut wird.~~ → Der Ist-Stand ist am 2026-09-01 Glied für Glied am Code belegt;
   die Klickfolge fährt der E2E-Fall des Testwegs, und er ist zugleich die rote Probe.
2. ~~Bestätigung nach oben oder Formular zuerst schließen?~~ → **Entscheidung 1** (Empfehlung bestätigt).
3. ~~Gehört die leere Liste in dieselbe Story?~~ → **Entscheidung 3** (Empfehlung bestätigt).
4. ~~Trifft es weitere Listen mit zeileninterner Bestätigung?~~ → **Entscheidung 4**: bewusst
   zurückgestellt, mit Kosten.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-123. Bleibt `unverifiziert`: Die Fundorte sind benannt,
  gefahren habe ich die Klickfolge nicht.
- 2026-09-01 · **`idee` → `ausformuliert`** (Nachtlauf). Alle sechs Glieder der Kette am Code belegt,
  Zeilennummern nachgezogen (Banner `:269`, Leer-Satz `:177`, Filter `:112-114`), die Server-Sortierung in
  `TextbookSeriesController.cs:93` gegengeprüft. **Ein Unterschied zur Schwester-Story B-151 herausgearbeitet,
  der beim Bauen zählt:** hier bleibt das Formular offen und sein Bezugspunkt zieht nach — die Ursache ist
  die verschwindende *Zeile*, nicht das schließende Formular. `unverifiziert` entfernt.
- 2026-09-01 · **`ausformuliert` → `gegrillt`** (Nachtlauf, Freigabe 1 — `art: Defekt`). Vier Entscheidungen
  mit Begründung und Kosten, alle vier offenen Punkte durchgestrichen statt gelöscht.
- 2026-09-01 · **`gegrillt` → `geschaetzt`** (Nachtlauf). M, Testweg zweigeteilt (reine Funktion als Unit,
  Klickfolge als E2E in einer bestehenden Spec-Datei).
- 2026-09-01 · **`geschaetzt` → `in-arbeit`** (Nachtlauf, Sprint 1, Commit `e479d83`). Meldungsplatz auf
  Seitenebene, `istGefiltert` als exportierte reine Funktion, filterbewusster Leer-Text.
  **Rote Probe: erwartet rot, gemessen rot** — mit derselben Korrektur an der Reihenfolge wie bei B-151
  (die erste Fassung konnte nicht fallen).
  **Entscheidung 1 hat beim Bauen einen Umweg gebraucht, und der gehört ins Protokoll:** Der erste Entwurf
  spiegelte *jede* Meldung des Formulars nach oben (`useReportedMessage`). Der `frontend-reviewer` hat
  daran zu Recht einen Blocker gesetzt — damit wanderten auch „Der Name der Reihe fehlt." und „Nichts
  geändert." ans Seitenende, obwohl sie die Zeile nie verlassen. Der zweite Entwurf zeigte sie lokal
  **und** oben; daran brachen **vier bestehende E2E** (`strict mode violation: getByText('Gespeichert.')
  resolved to 2 elements`). Die tragende Fassung ist die dritte und einfachste: **der Erfolg reist nach
  oben statt lokal, Fehler und Eingabeprüfungen bleiben am Formular.** `useReportedMessage` ist damit
  wieder entfallen — es gibt nur **einen** Mechanismus, wie Entscheidung 1 es wollte.
