---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [useGemerkterFehler Dep-Wechsel, alter Fehler bei neuer Suche]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-216]
wartet_auf: ""
nachgeschaut: "2026-10-18: Zustandsmaschine mit Schluessel durchgespielt (A-Fehler -> reload; -> B; -> B scheitert gleichlautend; A->B->A waehrend B laedt; Fehler im selben Render wie Wechsel), alle Konsumenten mit wechselnden useAsync-Deps auf Schluessel geprueft (Trefferbereich, AddPosition; Arten-ListenAusfall durch Schritt-Wechsel demontiert, VaterWizard :463 vs :543), Trim-Asymmetrie contentSearch roh/filterKey getrimmt; Begruendung von Entscheidung 2 durch Fund 1 entkraeftet -> Docblock berichtigt; kein Defekt"
---

# B-227 · Das Fehler-Gedächtnis überlebt auch den Start einer *anderen* Abfrage

B-216 hat die Regel „der letzte Fehler überlebt den Start der Wiederholung" in `useGemerkterFehler`
gefasst. Der Hook weiß aber nicht, ob dieselbe Abfrage wiederholt wird oder eine andere startet – und
`useAsync` setzt bei einem Wechsel der Abhängigkeiten ebenfalls `error = null, loading = true`.

## User Story

Als **Vater** möchte ich nach einer gescheiterten Übungssuche beim Ändern eines Filters sehen, dass eine
*neue* Suche läuft – nicht den alten Fehlertext mit einem „Lädt…"-Knopf, der zu einer Abfrage gehört, die
es nicht mehr gibt.

## Ist-Stand am Code

Nachgesehen am 2026-10-17 (Nachschau durch einen frischen Reviewer-Agenten, von mir gegengelesen).

- `frontend/src/lib/useGemerkterFehler.ts:22-29`: Rückgabe `fehler ?? (laedt ? letzter : null)` – ohne
  Kenntnis, welche Abfrage gerade lädt.
- `frontend/src/vater/VaterWizard.tsx` (Trefferbereich) und `frontend/src/vater/PlanPositions.tsx`
  (`AddPosition`): Nach einer gescheiterten Suche ändert der Vater einen Filter → `useAsync` startet neu
  (`error = null`, `loading = true`) → das Banner „Die Übungssuche ist gescheitert: <alter Fehler>" mit
  Knopf „Lädt…" bleibt stehen, und die Zählzeile sagt „Suche gescheitert" statt „Suche läuft…", weil
  `gescheitert` dort vor `zeilenVeraltet` ausgewertet wird (`VaterWizard.tsx:702`, `PlanPositions.tsx:534`).
- Vor `d834a8e` verschwand das Banner in genau diesem Fall (richtig); neu ist, dass es bleibt. Für
  `ListenAusfall` und die Shop-Karte ist das folgenlos – ihre Abfragen haben keine wechselnden Abhängigkeiten.
- Das Hook-Doc behauptete „den Start seiner **Wiederholung**"; nachgezogen auf das, was der Code tut, mit
  Verweis hierher.
- Beide Aufrufer kennen den Schlüssel der laufenden Abfrage schon: `filterKey` (`VaterWizard.tsx:175`,
  im Elternteil des Trefferbereichs; `PlanPositions.tsx:444`, in `AddPosition` selbst). Er trägt dort bereits
  `zeilenVeraltet` und das Generationen-Gate von „Alle wählen".

## Die echte Lücke

Das Gedächtnis braucht einen Schlüssel, der sagt, *wofür* es gilt. Zwei Wege: `useGemerkterFehler(fehler,
laedt, schluessel)` räumt `letzter` bei einem Schlüsselwechsel (im Trefferbereich und im Planbau gibt es ihn
schon: `filterKey`); oder die Aufrufer prüfen `zeilenVeraltet` vor `gescheitert`. Der erste Weg behebt die
Ursache, der zweite nur die Zählzeile – das Banner bliebe.

## Offene Punkte

1. ~~Schlüssel-Parameter am Hook (empfohlen) oder Reihenfolge in den Aufrufern?~~ → Entscheidung 1.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **Der Hook bekommt einen optionalen dritten Parameter `schluessel`; ein gemerkter Fehler gilt nur für
   den Schlüssel, unter dem er auftrat.** *Begründung:* Das Banner ist das Symptom, die Zählzeile nur seine
   Folge – die Reihenfolge in den Aufrufern zu drehen ließe das Banner stehen. Und im Planbau bleibt
   `zeilenVeraltet` nach einem Fehler **dauerhaft** wahr (Kommentar `PlanPositions.tsx:537-541`); stünde es
   vor `gescheitert`, sagte die Zählzeile nach einem Fehlschlag für immer „Suche läuft…". *Kosten:* zwei
   Aufrufer reichen `filterKey` durch, der Trefferbereich bekommt dafür eine neue Prop; `ListenAusfall`,
   Shop-Karte und Manifest lassen den Parameter weg und bleiben unverändert.
2. **Der gemerkte Fehler trägt seinen Schlüssel; verglichen wird am Rückgabepunkt, nicht per
   Effekt-Reset.** *Begründung:* Ein Effekt, der bei Schlüsselwechsel `setLetzter(null)` ruft, läuft erst
   **nach** dem Render – ein Render lang stünde der alte Text noch da (dieselbe Falle, aus der
   `seitenSchluessel` ein `useState` statt `useRef` ist, `VaterWizard.tsx:193-197`). *Kosten:* keine; der
   Zustand wird ein Paar statt eines Strings.
3. **`useAsync` bleibt unangetastet** – wie B-216, Entscheidung 2: die Frage „Feld im Hook" gehört B-164 und
   dem Nutzer. *Kosten:* der Schlüssel muss von Hand gereicht werden, wo `useAsync` ihn kennt.
4. **Die Zählzeile bleibt in ihrer Reihenfolge** (`gescheitert` vor `zeilenVeraltet`): mit dem Schlüssel ist
   `gescheitert` beim Filterwechsel `null`, die Zeile sagt also von selbst „Suche läuft…". *Kosten:* keine.

## Akzeptanzkriterien

1. Der Hook liefert nach `("Netz weg", laedt=false, "a")` → `(null, laedt=true, "b")` **`null`**; nach
   `(null, laedt=true, "a")` weiter `"Netz weg"`. Ohne dritten Parameter verhält er sich wie bisher (die fünf
   bestehenden Fälle bleiben grün). *Beleg:* `useGemerkterFehler.test.ts`.
2. Trefferbereich (Assistent) und Planbau: Nach einer gescheiterten Suche räumt ein Filterwechsel das Banner
   ab, die Zählzeile sagt „Suche läuft…" bzw. „(Suche läuft…)"; scheitert auch die neue Suche, steht ihr
   Fehler. *Beleg:* Vitest `VaterWizard.test.tsx` (Trefferbereich mit wechselndem `schluessel`); E2E
   `filterwechsel-raeumt-den-fehler.spec.ts` mit verzögerter zweiter Suche, gemessen **im Fenster** (Fristen
   kürzer als die Verzögerung, Muster `planbau-ladefenster.spec.ts`).
3. Die Wiederholung derselben Abfrage verhält sich unverändert: alle B-216-Fälle (Vitest) und
   `wiederholknopf-bleibt.spec.ts`, `shop-verlauf-wiederholen.spec.ts` bleiben grün.
4. `useAsync.ts` ist im Diff nicht enthalten.

## Schätzung

**Größe: S** — Hook um ~6 Zeilen (Paar statt String, Vergleich), zwei Aufrufstellen mit je einer Zeile plus
einer neuen Prop am Trefferbereich, zwei Vitest-Fälle, eine E2E mit zwei Fällen (Assistent, Planbau). Anker:
B-01 (S), kleiner als B-216.

**Risiken:** Die E2E muss **im Fenster** messen: Nach der verzögerten Antwort verschwindet das Banner auch
ohne Fix – eine `toHaveCount(0)`-Zusicherung mit Standardfrist pollt bis dahin und wäre grün. Die tragende
Zeile ist darum „(Suche läuft…)" sichtbar mit Frist < Verzögerung; vor dem Fix steht dort „(Suche
gescheitert)" bis zur Antwort und danach „(N Treffer)" – nie „läuft". Im Assistenten ist der Schlüssel `""`,
solange kein Fach gewählt ist – die E2E wechselt den Typ-Filter (`#wiz-type`) bei gesetztem Fach.

**Angriffsplan:** (1) Vitest-Fälle rot (Hook, Trefferbereich), (2) Hook, (3) Trefferbereich-Prop +
`VaterWizard.tsx`, (4) `PlanPositions.tsx`, (5) E2E, (6) Suite.

**Testweg:** `vitest run` (erwartet rot: Hook 1/1, Trefferbereich 1/1), `playwright test
e2e/filterwechsel-raeumt-den-fehler.spec.ts` (erwartet rot 2/2 an der „läuft"-Zeile), danach die ganze Suite.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 1 des Nachtlaufs (Commit `d834a8e`),
  `entgangen_bei: B-216`. Direkt `ausformuliert`: Ist-Stand mit `Datei:Zeile` belegt.
- **2026-10-17** — **ausformuliert → gegrillt**, autonom unter Freigabe 1 im zweiten Lauf der Nacht: vier
  Entscheidungen; Schlüssel am Hook, Vergleich am Rückgabepunkt, `useAsync` unberührt.
- **2026-10-17** — **gegrillt → geschaetzt**: S, frontend, keine Migration, kein Vertragsbruch; Testweg
  zwei Vitest-Fälle + eine E2E mit Fenster-Messung.
- **2026-10-18** — **geschaetzt → abgenommen** im Nachtlauf (zweiter Lauf der Nacht 2026-10-17, Sprint 3).
  **Rote Proben (erwartet/gemessen):** `useGemerkterFehler.test.ts:64` 1/1 (erwartet `null`, gemessen „Netz
  weg"), `VaterWizard.test.tsx:89` 1/1 („Suche läuft…" nicht im DOM), E2E `filterwechsel-raeumt-den-fehler`
  2/2 an Zeile 37 (`(Suche läuft…)` bzw. `Suche läuft…` binnen 1,2 s nicht sichtbar). **Fund der eigenen
  E2E nach dem ersten Fix (Zähler 1 von 5):** Vitest grün, Browser rot – der Filterwechsel rendert zuerst mit
  dem alten Fehler und dem neuen Schlüssel, `useAsync` räumt `error` erst im Effekt; der Effekt des Hooks
  merkte den alten Fehler unter dem neuen Schlüssel. Fix: `schluessel` ist keine Effekt-Abhängigkeit mehr
  (gemerkt wird nur, wenn `fehler` erscheint). Dafür zwei weitere Vitest-Fälle, der tragende
  (`useGemerkterFehler.test.ts:83`) rot gemessen 1/1 („Netz weg" statt `null`) – die vier Fälle, nicht zwei wie
  geschätzt. Belege: `tsc -b` sauber · `vitest run` 389/389 (47 Dateien) · die sechs betroffenen Specs 6/6 ·
  volle Playwright-Suite siehe Protokoll · `frontend-reviewer` (zweiter Anlauf nach Netzfehler): **kein
  Blocker, kein Should-fix**, vier Nits – Hook-Doc zu `ListenAusfall` berichtigt (Arten-Liste ist durch den
  Schritt-Wechsel abgebaut, bevor das Fach sich ändern kann), `#wiz-type` per Label statt Index, zwei
  Fenster-Fristen – zwei davon ändern Tests (Zähler 3 von 5). **Browser-Rollengang in Chrome (Freigabe 6)
  als Vater im Planbau:** Suche per Fetch-Wrapper abgeschossen → Banner + „(Suche gescheitert)"; Wrapper auf
  2,5 s Verzögerung, Fach-Filter auf Englisch → im Fenster gemessen: kein Banner, kein „Lädt…"-Knopf,
  Zählzeile „(Suche läuft…)"; danach „(6 Treffer)". Assistent per E2E (derselbe Baustein). Commit `5a91d27`
  (Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`, „Zweiter Lauf").
- **2026-10-18** — Nachschau (zweiter Lauf, „Retrospektive Sprint 4"): Zustandsmaschine und alle Konsumenten
  halten, **kein Defekt**. Berichtigt: Entscheidung 2 begründete den Vergleich am Rückgabepunkt damit, ein
  Reset-Effekt käme einen Render zu spät – Fund 1 der Bau-Runde zeigt, dass der Schlüssel ohnehin einen Render
  **vor** `error = null` wechselt; die Bauart ist richtig, die Begründung war es nicht (Docblock nachgezogen,
  zwei bewusste Grenzen des Hooks dort benannt: A→B→A während B lädt zeigt den alten A-Fehler; ein Fehler im
  selben Render wie ein Schlüsselwechsel würde unter dem neuen gemerkt – in der App durch `cancelled`
  unerreichbar). Nuancen: Schlüssel-Typ `unknown` mit `===` (Strings reichen heute), `VaterWizard.test.tsx`
  modelliert den Übergang weiter atomar, Trim-Asymmetrie zwischen `useAsync`-Deps und `filterKey` im
  Assistenten (konsistent mit „getrimmte Eingaben sind dieselbe Suche").
