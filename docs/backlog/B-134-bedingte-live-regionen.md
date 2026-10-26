---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/a11y]
aliases: [Live-Regionen hinter einer Bedingung, aria-live-Sweep, Wächter für Live-Regionen]
status: in-arbeit
prio: P3
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-132 (Nachtlauf 2026-08-09, Entscheidung 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-134 · Dreizehn Live-Regionen entstehen zusammen mit ihrem Text — und schweigen darum

Abgespalten von [B-132](B-132-hinweis-live-region-haengt-aus.md) (Entscheidung 2). B-132 hat die drei
Hinweise im Fachlehrer-Formular repariert; beim Bauen zeigte die Messung, dass es kein Einzelfall ist.
Der Sweep über zehn Dateien und ein Wächter darüber sind eine andere Arbeit als jener Fix — deshalb eine
eigene Story, wie es `docs/backlog/README.md` für einen Fund beim Bauen verlangt.

## User Story

Als **Nutzer eines Screenreaders** möchte ich Rückmeldungen der App tatsächlich hören — Fehlermeldungen,
Bestätigungen, Toasts —, statt dass sie stumm erscheinen, weil ihre Live-Region im selben Moment
entsteht wie ihr Text.

## Ist-Stand am Code

Die Regel steht im Repo bereits ausformuliert, an der Komponente, die es richtig macht:
`frontend/src/components/StatusBanner.tsx:9-12` — „Die Live-Region steht **immer** im DOM, auch ohne
Meldung: viele Screenreader sagen nur an, was in eine *bereits vorhandene* Region hineinwächst."

Gemessen am 2026-08-09 (`grep` über `frontend/src` nach `aria-live` und `role="status"`, jede Fundstelle
einzeln angesehen — nicht hochgerechnet): **dreizehn Regionen stehen hinter einer Bedingung**, in zehn
Dateien. Die dreizehnte kam am 2026-08-10 dazu, gefunden vom `frontend-reviewer` beim Review von B-123 —
sie hebelt denselben `StatusBanner` aus, dessen Kopfkommentar die Regel begründet.

| Datei:Zeile | Bedingung davor | Was stumm bleibt |
| --- | --- | --- |
| `vater/VaterExerciseCreate.tsx:297` | `{error && …}` | Fehlermeldung beim Anlegen einer Übung |
| `vater/VaterExerciseCreate.tsx:298` | `{okMsg && …}` | Erfolgsmeldung |
| `vater/VaterMedia.tsx:250` | `{msg && …}` | Rückmeldung nach dem Bild-Upload |
| `vater/VaterPlanCreate.tsx:81` | `{error && …}` | Fehler beim Plan-Anlegen |
| `vater/VaterWizard.tsx:397` | `{error && …}` | Fehler im Lehrplan-Assistenten |
| `sohn/SohnPractice.tsx:360` | `{toast && …}` | Toast während des Übens |
| `sohn/SohnShop.tsx:178` | `{msg && …}` | Toast im Shop |
| `vater/VaterVocab.tsx:465` | `{action.message && !action.message.ok && …}` | Fehler an der Vokabelzeile |
| `vater/VaterVocab.tsx:489` | dieselbe Bedingung | Fehler an der zweiten Zeile |
| `vater/VaterLogin.tsx:45` | `{registeredId !== null && …}` | „Konto angelegt, deine Id ist …" |
| `vater/exerciseConfig.tsx:619` | `{choicesMissAnswer(r) && …}` | Validierung „Antwort fehlt in der Auswahl" |
| `components/ListControls.tsx:61` | `if (total <= shown) return null;` | Hinweis auf abgeschnittene Treffer |
| `vater/VaterLehrwerke.tsx:177` | `{action.message && <tr>…}` | Rückmeldung beim Löschen einer Reihe |

Richtig gemacht (Region dauerhaft, Inhalt wechselt): `StatusBanner.tsx:16`, `SohnApp.tsx:94-96`
(Münz-/Gem-/Streak-Chips), `ListControls.tsx:46` (Seitenanzeige) und seit B-132
`VaterFachlehrer.tsx:128-144`.

## Die echte Lücke

Nicht „Barrierefreiheit wurde vergessen" — das Gegenteil: an allen dreizehn Stellen hat jemand `role`
und `aria-live` bewusst gesetzt. Die Lücke ist, dass **die Attribute allein die Zusicherung nicht
tragen** und die dafür nötige zweite Hälfte (die Region muss die Bedingung überleben) nur an einer
Stelle dokumentiert ist — im Kommentar von `StatusBanner`, den niemand liest, der gerade ein `{error &&
…}` tippt.

Das ist damit die Sorte Regel, die dieses Repo sonst mechanisch hält („mechanische Tore statt
Disziplin"). Ein Sweep ohne Wächter stellt denselben Zustand in drei Monaten wieder her.

## Offene Punkte

1. ~~**Sweep plus Wächter, oder nur der Sweep?**~~ → Entscheidung 1. Empfehlung: beides, und der Wächter ist der eigentliche
   Wert — ein Vitest, der über die `.tsx`-Quellen im Frontend läuft und `role="status"`/`aria-live`
   direkt hinter `&&` oder in einem früh-`return null`-Zweig meldet, mit Ausnahmeliste und Begründung
   je Eintrag (Muster: die Ausnahmelisten der `ConventionGuardTests`). Kosten: ein quellentext-lesender
   Test ist ein grober Parser und wird Fehlalarme haben — das Repo hat damit Erfahrung
   (`B-40`, „halbe Parser brauchen eine Rot-Liste").
2. ~~**Ist `role="status"` überall überhaupt richtig?**~~ → Entscheidung 2. Mindestens zwei Fälle sind zweifelhaft:
   `ListControls.tsx:61` ist ein statischer Hinweis über den Filterstand, keine Rückmeldung auf eine
   Handlung, und `exerciseConfig.tsx:619` ist eine Validierungsmeldung, für die `role="alert"` die
   passendere Rolle wäre. Empfehlung: beim Sweep je Fundstelle entscheiden statt mechanisch
   umzuformen — sonst wird aus dreizehn richtigen Attributen dreizehn falsche Dauer-Regionen.
3. ~~**Reicht `StatusBanner` als Ersatz für die meisten?**~~ → Entscheidung 3. Acht der dreizehn sind schlichte
   Banner/Toasts, für die es die Komponente längst gibt. Empfehlung: dort ersetzen statt reparieren —
   das entfernt die Fehlerquelle, statt sie an dreizehn Stellen einzeln zu umgehen. Kosten: die Optik der
   Toasts (`className="toast"` in der Sohn-Arcade) unterscheidet sich vom Banner; das muss angesehen
   werden, bevor es getauscht wird.

## Entscheidungen

Autonom gegrillt im fünften Nachtlauf (`Defekt`, Freigabe 1), gegen HEAD `515022c` gemessen.

1. **Sweep und Wächter.** Der Wächter ist ein Vitest über alle `.tsx` (`import.meta.glob(..., { query: "?raw" })`,
   kein `node:fs`), der ein öffnendes Tag mit `role="status"`/`aria-live` meldet, wenn es (a) direkt hinter `&&`,
   `?` oder `:` steht oder (b) in einer Funktion nach einem `return null` liegt. `role="alert"` meldet er nicht
   (eingefügtes `alert` wird angesagt, B-188). Ausnahmeliste je Eintrag mit Datei, Tag-Ausschnitt (überlebt
   Zeilenverschiebungen) und Grund. Kosten: ein halber Parser – Fehlalarme gehören in die Ausnahmeliste.
   Gemessen am Ausgangsstand: **13 Funde** – die zwölf noch offenen der Tabelle plus die Seitenanzeige
   `ListControls.tsx:46` (Pager, entsteht mit `return null` davor) → einzige Ausnahme, mit Grund: sie soll den
   Seitenwechsel ansagen, ihr stilles erstes Erscheinen ist gewollt.
2. **Rolle bleibt je Fundstelle `status`.** Auch `exerciseConfig.tsx:643` (Validierung) und `TruncationHint`
   bekommen eine dauerhafte, höfliche Region statt eines lauten `alert`: die Meldung entsteht beim Tippen, ein
   `alert` unterbräche jede Eingabe. Kosten: keine sichtbaren.
3. **Nicht `StatusBanner` einsetzen, sondern die Region selbst zum Kasten machen.** Die Region steht immer im DOM
   und trägt ihre sichtbare Klasse nur, solange es etwas zu melden gibt
   (`<div className={`live-region${error ? " banner err" : ""}`} role="status" aria-live="polite">{error}</div>`),
   dazu eine Regel `.live-region:empty { position: absolute; }` – eine leere Region nimmt so keinen `gap` und
   keinen Platz ein. Begründung: AK 3 („die sichtbare Darstellung ändert sich nirgends") – ein Wrapper um den
   Kasten verschöbe Abstände in Flex-Spalten, `StatusBanner` brächte `marginTop: 8` mit, und die Toasts der
   Arcade (`position: fixed`) bleiben so, wie sie sind (die Animation `pop` startet beim Setzen der Klasse).
   Kosten: zwölf kleine Umbauten; die Klassen-Weiche steht an jeder Stelle einmal.
4. **Die sieben rollenlosen Fehlerbanner aus B-172** (Nachtrag 2026-09-01) gehören **nicht** in diesen Sweep: sie
   sind Lesefehler, und für Lesefehler hat der Nutzer am 2026-10-23 `ListenAusfall` zur Hausregel gemacht
   (B-183, Paket NP-15). Zweimal angefasst würden sie dort ohnehin ersetzt.

## Akzeptanzkriterien

1. Jede der dreizehn Fundstellen ist entweder auf eine dauerhaft gerenderte Region umgestellt, durch
   `StatusBanner` ersetzt, oder mit begründetem Kommentar als bewusste Ausnahme markiert.
2. Ein Wächter meldet eine neu hinzukommende bedingte Live-Region; seine Ausnahmeliste trägt je Eintrag
   einen Grund.
3. Die Frontend-Suite bleibt grün, die sichtbare Darstellung ändert sich nirgends.

## Schätzung

**M**, `wo: frontend`, keine Migration, kein Vertragsbruch. Risiken: Komponententests, die heute
`queryByRole("status")` als „keine Meldung" lesen, sehen künftig eine leere Region (beim Bau die Vitest-Suite
ganz laufen lassen); `:empty` greift nur, wenn wirklich nichts gerendert wird (`{error}` mit `null`/`""` ist leer).
**Angriffsplan:** (1) Wächter rot sehen (13 Funde, gemessen), (2) die CSS-Regel, (3) die zwölf Stellen umbauen,
(4) Wächter grün, Vitest und `tsc -b` voll, (5) `frontend-reviewer`. **Testweg:** der Wächter mit sieben
Erkennungsfällen (je Form, die ihn rot macht) plus Playwright voll (sichtbar unverändert).

## Nachtrag 2026-09-04 (B-188)

`ListenAusfall` (`VaterWizard.tsx`) trägt sein `role="alert"` **gemeinsam mit dem Text**, an drei
Einsetzungen. Das ist bewusst und folgt dem direkten Nachbarn in derselben Datei (B-162, Trefferbereich),
nicht dieser Story: Ein *eingefügtes* `role="alert"` wird deutlich zuverlässiger angesagt als eine
`role="status"`-Region, die mit ihrem Text entsteht. Die Tabelle unten wächst damit um drei Einträge —
hier vermerkt, damit sie nicht unbemerkt wachsen.

## Verlauf

- **2026-08-09** — angelegt und zugleich ausformuliert im Nachtlauf (Sprint 1, beim Bau von B-132). Der
  Ist-Stand ist gemessen, nicht geschätzt: zwölf Fundstellen einzeln klassifiziert, dazu die vier
  Gegenbeispiele, die es richtig machen. **Bewusst nicht in B-132 mitgenommen:** dessen Ziel — die drei
  Hinweise im Fachlehrer-Formular — ist ohne diesen Sweep erfüllt, und ein Wächter über neun Dateien ist
  eine eigene Arbeit mit eigenen Entscheidungen (Punkt 2 zeigt, dass er nicht mechanisch ist).
- **2026-08-10** — dreizehnte Fundstelle ergänzt (`VaterLehrwerke.tsx:177`, Rückmeldung beim Löschen
  einer Reihe), gefunden vom `frontend-reviewer` beim Review von B-123. Sie ist der lehrreichste der
  Fälle: dort wird ein `StatusBanner` **bedingt** gerendert, also genau die Komponente ausgehebelt,
  deren Kopfkommentar die Regel begründet. Zahlen im Text nachgezogen (zwölf → dreizehn, neun → zehn
  Dateien).
- **2026-08-10** — Zeilenangaben zu `VaterExerciseCreate.tsx` nachgezogen (270/272 → 297/298): B-11 hat
  darüber ein Formularfeld eingefügt. Nur die Belege wandern, der Befund und die Stufe bleiben.
- **2026-09-01** — **Sieben Kandidaten für denselben Sweep** dazugekommen, aus B-172: die neuen
  Fehlerbanner zu den Optionslisten (`VaterLehrwerke.tsx` 2×, `VaterKind.tsx`, `ChildMaterialSection.tsx`
  2×, `VaterFachlehrer.tsx` 2×). Sie tragen **gar keine** Rolle, gehören also streng genommen nicht in die
  Tabelle oben — aber sie sind das *einzige* Signal eines gescheiterten Abrufs und damit Status-Messages
  im Sinne von SC 4.1.3. Der `frontend-reviewer` hatte für B-172 ein `role="alert"` angeregt; das wurde
  dort **abgelehnt**, weil ein `role` auf einem `{x.error && …}` der vierzehnte Fall genau dieses Defekts
  wäre. Die Behebung ist dieselbe wie hier — eine Region, die schon im DOM steht — und darum dieselbe
  Arbeit. Der Befund und die Stufe bleiben.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht – 12 von 13 Stellen weiter bedingt; behoben ist nur `VaterLehrwerke.tsx` (dauerhafter `StatusBanner`). Neue Zeilen u. a. `VaterExerciseCreate.tsx:309,311`, `VaterWizard.tsx:640`. Die Rollenwahl je Fundstelle verengt `StatusBanner.tsx:9-16`; nur der Tausch der Toast-Optik in der Arcade (Punkt 3) braucht einen Blick. Nachtlauffähig nach autonomem Grillen, M.
- **2026-10-24** — fünfter Nachtlauf, Sprint 10 (NP-5, [Protokoll](../pm-sitzung-2026-10-24-nacht.md)): autonom gegrillt (Entscheidungen 1–4) und geschätzt. Wächter `frontend/src/liveRegions.test.ts` geschrieben; rote Probe am Ausgangsstand (Auflage 5): erwartet 12 offene Stellen der Tabelle → gemessen **13 Funde** (dazu `ListControls.tsx:46`, Pager → Ausnahme mit Grund), danach mit Ausnahme **12**; die sieben Erkennungsfälle grün. **Unterbrochen (Rechner aus), nicht gebaut:** der Wächter liegt im Stash `nachtlauf-2026-10-24 NP-5 wip` (er ist rot, bis die zwölf Stellen umgebaut sind), die Umbauten fehlen.
- **2026-10-25** — fortgesetzt am 2026-10-25 (Wiederaufnahme des fünften Nachtlaufs, Sprint 10 / NP-5, Zähler weiter bei 5; [Protokoll](../pm-sitzung-2026-10-25-nacht.md)).
- **2026-10-25** — gebaut nach Entscheidungen 1–3: Wächter aus dem Stash zurückgeholt, Regel `.live-region:empty { position: absolute; }` in `index.css`, die zwölf Stellen auf „Region immer da, sichtbare Klasse nur mit Meldung" umgebaut (`VaterExerciseCreate.tsx` 2×, `VaterMedia.tsx`, `VaterPlanCreate.tsx`, `VaterWizard.tsx`, `SohnPractice.tsx`, `SohnShop.tsx`, `VaterVocab.tsx` 2× über eine gemeinsame `failure`-Variable, `VaterLogin.tsx`, `exerciseConfig.tsx` „Antwort fehlt in der Auswahl", `TruncationHint` ohne `return null`). Rote Probe (Auflage 5), am wiederhergestellten Stand vor dem Umbau: erwartet 12 Funde → gemessen **12** (Liste im Testausgang, 1 failed / 8 passed); nach dem Umbau **9/9**. `tsc -b` sauber, Vitest **418/418** (409 + 9 Wächter-Fälle). AK 3 („sichtbar unverändert") hält Playwright voll im Sprint-Abschluss; strukturelle Selektoren (`:last-child`, `+`, `~`), die eine leere Region verschieben könnte, gibt es in `index.css` nicht (gesucht).
- **2026-10-25** — Lauf an Freigabe 3 beendet (Fund 6 im Delta-Review von B-263, nicht in dieser Story), bevor der `frontend-reviewer` für B-134/B-236 lief. Stand gebaut, grün, committet; Stufe bleibt `in-arbeit`.
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Sprint 10 wird fortgesetzt, die Nacharbeit (inkl. des nicht gelaufenen `frontend-reviewer`) läuft mit **neuem Zähler (0)**. `wartet_auf` geleert.
