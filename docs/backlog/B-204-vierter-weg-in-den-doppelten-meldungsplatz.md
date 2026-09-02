---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/tests, rolle/creator]
aliases: [rot und gruen nebeneinander, Zeile hat zwei Meldungsplaetze, vierter Weg in B-197]
status: in-arbeit
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-09-02 zu den 19 Commits vor origin/main
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-197]
wartet_auf: frontend-reviewer
---

# B-204 · Der vierte Weg in denselben Widerspruch: die Reihen-Zeile hat zwei Meldungsplätze

B-197 hat aufgeräumt, dass „was am Meldungsplatz steht, zu der Handlung gehört, die gerade getan wurde", und
dafür in `SeriesRow` das Prop `onMeldung` zur **Pflicht** gemacht — mit dem Satz „Ein Prop mit `null` als
Wert kann man nicht halb verdrahten" als Begründung im Code.

Man kann es halb verdrahten, wenn es **zwei** Plätze gibt. Die Zeile hält neben dem Platz der Seite einen
eigenen (`action.message`), und in den schreibt nur `remove`. Beide Umschalter räumten bloß den der Seite.

## User Story

Als *Vater* möchte ich nie zwei widersprechende Meldungen gleichzeitig sehen, damit ich weiß, ob meine
letzte Handlung geklappt hat.

## Ist-Stand am Code (vor der Reparatur)

| Stelle | Räumt | Schreibt in |
|---|---|---|
| `bearbeitenUmschalten` | `onMeldung(null)` — Platz der **Seite** | — |
| `onToggle` (Knopf „Units") | `setMeldung(null)` beim Aufrufer, `VaterLehrwerke.tsx:196` | — |
| `remove` | `onMeldung(null)` + `action.run` räumt intern | Platz der **Zeile** (`action.message`) |
| `SeriesForm` (Speichern) | — | Platz der **Seite** |

Gerendert wird der Zeilen-Platz in einer eigenen Tabellenzeile (`VaterLehrwerke.tsx:315`). Ein
gescheitertes Löschen bleibt dort also stehen, während ein späteres erfolgreiches Speichern sein grünes
„Gespeichert." oben ablegt — **rot und grün in einem Blickfeld**, genau der Schaden, gegen den der Platz
gebaut wurde.

Erreichbar ist der Fehlerfall: `DELETE` einer Reihe liefert `409 exercise_in_use`, sobald ihr Inhalt in
einem Lehrplan, einer Klassenarbeit oder einem Objective-Meilenstein steckt
(`TextbookSeriesController.cs:268-273`).

## Die echte Lücke

Nicht ein vergessener Aufruf, sondern eine **Annahme**: B-197 hat den Meldungsplatz als *einen* Ort gedacht
und darum das Räumen beim Aufrufer verankert. Solange eine Komponente einen zweiten, eigenen Platz hält, kann
kein Aufrufer ihn räumen — er kennt ihn nicht.

Der Review nannte `bearbeitenUmschalten`. Der Knopf „Units" daneben führt **einen Klick weiter** in denselben
Widerspruch, weil `UnitPanel` und `UnitForm` ebenfalls in den Platz der Seite melden. Das ist derselbe Fund,
nicht ein zweiter — und es zu übergehen wäre die stille Auslassung, die B-197s eigener Reviewer-Blocker
angemahnt hat.

## Akzeptanzkriterien

1. Nach einem gescheiterten Löschen räumt **jeder** Weg, der die Zeile in eine neue Handlung führt, auch
   ihren eigenen Platz — geprüft über „Bearbeiten" und über „Units".
2. Ein späteres erfolgreiches Speichern zeigt sein „Gespeichert." **allein**; die rote Meldung von vorher
   ist weg.
3. Die Regel sitzt in der Zeile, nicht beim Aufrufer.
4. Der Beleg fährt den echten `409`, nicht einen nachgebauten Fehlerzustand.

## Entscheidungen

1. **Beide Umschalter bekommen `action.clear()`, nicht nur der vom Review genannte.** Begründung: eine
   Wurzel, zwei Türen; die zweite wegzulassen wäre genau die Auslassung, die B-197s Nachprüfung angemahnt
   hat. *Kosten:* Eine zusätzliche kleine Funktion (`unitsUmschalten`), weil `onToggle` von außen kommt.
2. **Das Räumen sitzt in `SeriesRow`, nicht im Aufrufer.** Begründung: Der Aufrufer kennt den lokalen Platz
   der Zeile nicht, und ihn darüber zu informieren wäre ein zweites Prop für eine Regel, die eine Zeile
   hoch gehört. *Kosten:* `onToggle` wird lokal umwickelt statt direkt verdrahtet — eine Indirektion, die
   ein Leser erst beim zweiten Blick sieht; darum steht der Grund als Kommentar daneben.
3. **Der Beleg ist eine E2E, kein Komponententest.** Begründung: Der Widerspruch entsteht erst aus
   *zwei* Server-Antworten in Folge (`409`, dann `200`) und zwei Meldungsplätzen im DOM. Ein
   Komponententest bräuchte ein gefälschtes `fetch`, das dieses Frontend nirgends hat. *Kosten:* Die
   Zusicherung läuft nur im E2E-Lauf, nicht in der schnellen Suite.
4. **Der Fall hängt am Ende von `creator-lehrwerk-weg.spec.ts`, nicht in
   `bestaetigung-ueberlebt.spec.ts`.** Begründung: Er braucht den Zustand, in dem jene Spec **ohnehin
   endet** — eine eigene Reihe, deren Übung in einem Lehrplan steckt; erst das macht das Löschen zum `409`.
   Ihn am fachlich passenderen Ort nachzubauen hieße, vier Bildschirme ein zweites Mal zu fahren.
   *Kosten:* Ein Abstrich an der Regel „eine Fläche je Spec" aus `frontend/CLAUDE.md`. Gemildert dadurch,
   dass er der **letzte** Schritt ist und darum nichts verdeckt; als Kommentar in der Spec begründet.

## Schätzung

**Größe: XS** — zwei Aufrufe und eine dreizeilige Funktion.

- **`wo: frontend`** — nur `vater/VaterLehrwerke.tsx` plus die E2E.
- **`migration: nein`**, **`vertragsbruch: nein`**.

**Testweg**: `frontend/e2e/creator-lehrwerk-weg.spec.ts`, Schritt 6.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem **Code-Review** der 19 Commits vor `origin/main` und im selben Zug
  **gebaut**. Zuordnung belegt: `git log -S bearbeitenUmschalten` nennt `6688ff5` (Sprint 1, Nachprüfung des
  Reviewers), der Meldungsplatz selbst ist B-197 — daher `entgangen_bei: [B-197]`.
  **Verifikation:** `creator-lehrwerk-weg.spec.ts` grün in 43 s; der Serverlog belegt den echten Weg
  (`DELETE /api/v1/creator/textbook-series/5 → 409`, danach `PATCH → 200`), Kriterium 4 ist also gemessen.
  Dazu 329/329 vitest, `tsc` sauber. Commit `81eb60c`.
  **Rote Probe mit Zahl:** `action.clear()` aus `bearbeitenUmschalten` entfernt → Spec fällt an Zeile 139,
  `toHaveCount(0)` gegen `unexpected value "1"` — die rote Meldung stand also wirklich noch da.
  **Ein Nebenbefund, der nicht behoben wurde:** `exercise_in_use` hat keine Zeile in
  `GERMAN_PROBLEM_TEXT` (`api.ts:80`), der Vater liest dort den englischen Servertext. Die Zusicherung nimmt
  darum den englischen Text, statt eine Übersetzung zu erhoffen. Das ist eine i18n-Entscheidung und keine
  Folge dieses Defekts.
- 2026-09-02 · Bleibt auf `in-arbeit`: Der `frontend-reviewer` ist **nicht gelaufen** (diese Sitzung startet
  keine Agenten unaufgefordert). Alles andere ist belegt, `wartet_auf` benennt es.
  **Der Rollengang ist die E2E** — sie fährt eine echte Oberfläche in einem echten Browser gegen einen
  echten Server, in genau der Reihenfolge, in der der Fehler auftrat (Bereichs-README: „Eine E2E, die den
  Weg fährt, ist der Rollengang").
