---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/tests, rolle/creator]
aliases: [rot und gruen nebeneinander, Zeile hat zwei Meldungsplaetze, vierter Weg in B-197]
status: abgenommen
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
wartet_auf: ""
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

1. ~~**Beide Umschalter bekommen `action.clear()`.**~~ **Überholt von Entscheidung 5** — der Ansatz war
   richtig gegen die vier bekannten Türen und falsch gegen die Frage dahinter. Er bleibt hier stehen, weil
   die rote Probe zu 6a auf ihm beruht: mit `action.clear()` und Zeilen-Platz ist der *gemeldete* Weg grün
   und nur der fünfte rot — das ist der Beleg, dass Entscheidung 5 mehr behebt als 1.
2. ~~**Das Räumen sitzt in `SeriesRow`, nicht im Aufrufer.**~~ **Überholt von Entscheidung 5.** Die
   Entscheidung beantwortete „wer räumt" und ließ „warum zwei Plätze" ungefragt — so vom
   `frontend-reviewer` benannt, und er hat recht.
3. **Der Beleg ist eine E2E, kein Komponententest.** Begründung: Der Widerspruch entsteht erst aus
   *zwei* Server-Antworten in Folge (`409`, dann `200`) und zwei Meldungsplätzen im DOM. Ein
   Komponententest bräuchte ein gefälschtes `fetch`, das dieses Frontend nirgends hat. *Kosten:* Die
   Zusicherung läuft nur im E2E-Lauf, nicht in der schnellen Suite.
4. **Der Fall hängt am Ende von `creator-lehrwerk-weg.spec.ts`, nicht in
   `bestaetigung-ueberlebt.spec.ts`.** Begründung: Er braucht den Zustand, in dem jene Spec **ohnehin
   endet** — eine eigene Reihe, deren Übung in einem Lehrplan steckt; erst das macht das Löschen zum `409`.
   Ihn am fachlich passenderen Ort nachzubauen hieße, vier Bildschirme ein zweites Mal zu fahren.
   *Kosten:* Ein Abstrich an der Regel „eine Fläche je Spec" aus `frontend/CLAUDE.md`. Gemildert dadurch,
   dass er der **letzte** Schritt ist und darum nichts verdeckt. **Zwei Kosten hat diese Zeile zunächst
   verschwiegen, beide vom `frontend-reviewer` benannt:** Die Regel hat auch eine andere Richtung — *jedes*
   Rot in den Schritten 1–5 legt die neue Zusicherung still (B-109); und es gab eine **dritte Option**, die
   die Story nicht nannte: die Suite baut Zustand längst über HTTP auf (`lehrer-konto.spec.ts:50`,
   `uebungstypen.spec.ts:125` nutzen `page.request`), eine eigene Spec hätte also nicht „vier Bildschirme
   ein zweites Mal" bedeutet. Die Entscheidung bleibt (der Zustandsbedarf ist echt, der Schritt kostet
   ~1 s), aber sie ist jetzt vollständig bezahlt statt günstig gerechnet.
5. **Es gibt nur noch EINEN Meldungsplatz: auch der Fehlschlag von `remove` meldet nach oben.** Begründung:
   Solange die Zeile einen eigenen Platz hält, kann kein Aufrufer ihn räumen — und eine **andere** Zeile
   schon gar nicht. Damit war jede Räum-Regel pro Tür ein Nachlaufen hinter der Zahl der Türen; es waren
   fünf, nicht vier. Mit einem Platz gilt B-197s Regel **konstruktiv** statt über einen Aufruf je Tür.
   `action` bleibt für die Wiedereintritts-Sperre und `busy` (Konventionspflicht aus `frontend/CLAUDE.md`);
   nur seine Meldung wird nicht mehr gerendert. Der Fehlertext wird beim Durchfliegen abgegriffen, weil
   `action.message` im selben Tick noch der alte Zustand ist. *Kosten:* Sechs Zeilen in `remove` und ein
   `catch`, das die Ausnahme weiterwirft, damit `useAction` seinen Lauf zu Ende führt. Dafür fallen
   `unitsUmschalten` **und** beide `action.clear()` wieder weg — der Umbau ist netto kaum größer als
   Entscheidung 1 und deckt einen Weg mehr.
6. **Ein von der Sperre verworfener Aufruf bleibt stumm.** `action.run` liefert auch dann `false`, hat aber
   nichts getan; gemeldet wird nur, wenn wirklich ein Fehlertext anfiel. Begründung: `useAction` sichert
   „ohne Meldung, ohne Server-Aufruf" ausdrücklich zu. *Kosten:* eine `if`-Bedingung, die aussieht wie
   Vorsicht und eine Zusicherung des Primitivs ist — darum steht der Grund daneben.

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
- 2026-09-02 · **`frontend-reviewer` gelaufen** (Freigabe des Nutzers). **Kein Blocker**, aber er hat einen
  **fünften Weg** in denselben Widerspruch gefunden, und der macht Kriterium 2 im allgemeinen Fall unwahr:
  Der Zeilen-Platz wurde nur von den Knöpfen **derselben** Zeile geräumt. Zwei eigene Reihen, A in
  Verwendung, B nicht — „Löschen" an A scheitert (rot unter A, und der Text nennt die Zeile nicht),
  „Löschen" an B gelingt (grün unter der Tabelle), das Rot an A steht weiter. Ein Kriterium, das nur im
  gemeldeten Sonderfall hält, ist kein Kriterium; es zu verengen, damit es passt, wäre genau der stille
  Griff, gegen den dieser Bereich gebaut ist.
  **Darum strukturell repariert statt gepatcht** (Entscheidungen 5 und 6, der Vorschlag des Reviewers): Es
  gibt nur noch **einen** Meldungsplatz. `unitsUmschalten` und beide `action.clear()` sind damit wieder
  weg — die Reparatur ist netto kaum größer und deckt einen Weg mehr.
  **Das erledigt seinen a11y-Fund mit, und zwar echt:** Der Zeilen-Banner entstand samt seinem Text und
  blieb darum stumm (dieselbe Klasse wie B-132/B-134). Nachgesehen statt gehofft — der Seitenplatz rendert
  `<StatusBanner message={meldung} />` **unbedingt** (`VaterLehrwerke.tsx:210`), und `StatusBanner` hält
  seine Live-Region auch im Leerfall im DOM. Der `409`, den die Spec zusichert, ist für einen Screenreader
  ab jetzt vorhanden.
  **Rote Probe, neu und schärfer:** Zeilen-Platz wiederhergestellt **inklusive** `action.clear()` am
  Bearbeiten-Knopf, also genau der Zustand nach Entscheidung 1. Ergebnis: **6a grün, 6b rot** an
  `creator-lehrwerk-weg.spec.ts:164` (`toHaveCount(0)` gegen `unexpected value "1"`). Das belegt zweierlei
  in einem Lauf — der fünfte Weg war wirklich offen, und Entscheidung 1 hätte ihn nicht geschlossen.
  **Verifikation nach dem Umbau:** **8/8** E2E der vier Specs dieser Fläche
  (`creator-lehrwerk-weg`, `bestaetigung-ueberlebt`, `lehrwerke`, `lehrwerk-bearbeiten`) — die B-197/B-196-Spec
  läuft also unverändert grün gegen den neuen Aufbau. Dazu **329/329** vitest, `tsc` sauber, `dotnet format`
  auf allen geänderten Dateien sauber.
  **Ein Nebenbefund als eigene Story statt als Vermerk:** `exercise_in_use` erfüllt beide Hälften des
  Aufnahmetests, den `GERMAN_PROBLEM_TEXT` sich selbst gibt, und seine Geschwister `subject_in_use` und
  `publisher_in_use` stehen genau darum drin — der Nebenbefund ist also „dritter Geschwisterfall, offen"
  und keine i18n-Entscheidung, wie diese Story vorher schrieb. Er liegt jetzt als
  [B-205](B-205-exercise-in-use-ohne-deutsche-fassung.md). Die Kopplung steht als Satz in der Spec: kommt
  die deutsche Zeile, muss die Zusicherung mitwandern.
- 2026-09-02 · `in-arbeit` → **`abgenommen`**. Kriterium 2 gilt jetzt im **allgemeinen** Fall, beide Wege
  sind zugesichert (6a der gemeldete, 6b der offene). Reviewer gelaufen und eingearbeitet, Commit `81eb60c`
  plus der Nachtrag dieser Runde. Der **Rollengang ist die E2E** — echter Browser, echter Server, die
  Reihenfolge, in der der Fehler auftrat.
