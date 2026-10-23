---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [label-row ohne Typografie, Gruppen-Ueberschrift als Fliesstext]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-11
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-11]
---

# B-176 · Eine Gruppen-Überschrift verliert ihre Optik, sobald sie kein `<label>` mehr ist

Der a11y-Schritt war richtig, seine sichtbare Folge ist mitgekommen und niemandem aufgefallen.

## User Story

Als **Vater** möchte ich auf „Übung anlegen" jede Gruppen-Überschrift als Überschrift erkennen – „Freigabe
(Startzustand)" genauso wie „Schularten" daneben –, damit ich die Karte als drei Abschnitte lese und nicht als
zwei Abschnitte plus einen Satz.

## Ist-Stand am Code (selbst nachgeprüft)

`frontend/src/index.css:224` stylt die Feld-Überschrift ausschließlich über das Element:

```css
.field label { font: 700 12px/1 var(--font-body); color: var(--muted); text-transform: uppercase; … }
```

`.label-row` (`:229`) setzt **nur** `display:flex; align-items:center; gap:6px` — keine Typografie.

In `frontend/src/vater/VaterExerciseCreate.tsx:278` wurde die Gruppen-Überschrift im Review-Fix `2d42f13`
von `<label>` auf `<span className="label-row">` umgestellt, mit einer im Code stehenden und **korrekten**
Begründung: „Gruppen-Überschrift, darum `span` statt `label`: sie beschriftet kein Eingabefeld." Die Folge:
„Freigabe (Startzustand)" rendert als **Fließtext**, während „Schularten" und „Lern-Standards" in derselben
Karte als Überschrift erscheinen.

Dasselbe Muster steht an weiteren Stellen — ein `label-row`-`span`, der Text **direkt** enthält statt eines
`<label>`: `ClozeTexts.tsx:244,272`, `exerciseConfig.tsx:438,719`. Davon zu unterscheiden ist die in
`frontend/CLAUDE.md` **dokumentierte** Verwendung (`label-row` als Hülle um `<label className="checkline">` +
`<InfoHint>`) — dort trägt das innere `<label>` die Optik, und alles ist in Ordnung. Die Fundstellen sind
darum einzeln anzusehen und nicht pauschal umzustellen; eine Zahl schreibe ich hier bewusst nicht hin
(siehe [B-175](B-175-zwei-gerottete-zahlen-in-kommentaren.md)).

## Fehlerfamilie

Korrekt für den bedachten Fall — die Zugänglichkeit —, schmal beim Rest: die Optik hing am Elementnamen,
und mit dem Elementnamen ging sie weg. Kein Test deckt Typografie, und im Rollengang liest man eine
Überschrift, die wie Fließtext aussieht, als Fließtext.

## Angriffsplan (Vorschlag)

Eine Klasse für Gruppen-Überschriften, die dieselbe Typografie trägt (z. B. `.field > .label-row` oder eine
eigene `.group-label`), und die betroffenen Stellen darauf ziehen. Damit ist die Optik an die **Rolle**
gebunden statt an das Element — was der eigentliche Fehler war.

**Testweg**: keiner auf Testebene (Typografie ist nicht sinnvoll zusicherbar). Beleg ist der Blick auf die
Karte „Übung anlegen": drei Gruppen, drei gleich aussehende Überschriften.

## Akzeptanzkriterien

1. „Freigabe (Startzustand)" auf `/vater/exercises/neu` trägt dieselbe berechnete Typografie wie das
   `<label>` „Lern-Standards" derselben Karte (`text-transform`, `font-weight`, `font-size`). *Beleg:* E2E
   mit `toHaveCSS`, im Browser gegen den echten Stylesheet gemessen – vor dem Fix `none` statt `uppercase`.
2. Jede Gruppen-Überschrift, die Text direkt in einem `label-row`-`span` trägt, ist auf die neue Klasse
   umgestellt; die Fundstellen stehen in Entscheidung 2 mit `Datei:Zeile`.
3. Das „i" des Hinweis-Knopfs bleibt klein, auch innerhalb einer Gruppen-Überschrift.
4. Die umhüllende Verwendung (`label-row` um `<label className="checkline">` + `InfoHint`) bleibt unverändert.

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **Eine Klasse `group-label`, die Typografie hängt an `.field label, .field .group-label`.** Begründung:
   Das ist die im Angriffsplan vorgeschlagene Bindung an die **Rolle** statt ans Element; `.field > .label-row`
   wäre zu breit (es träfe auch die Hülle um `checkline`-Labels und würde deren Hinweis-Knopf mitformatieren).
   `label-row` bleibt an den Stellen stehen – es trägt die Flex-Ausrichtung für den Hinweis-Knopf. **Kosten:**
   eine Klasse mehr, die man kennen muss; der Hinweis dazu kommt in `frontend/CLAUDE.md` an die bestehende
   `label-row`-Zeile (eine halbe Zeile, Budget prüfen).
2. **Sieben Fundstellen, einzeln angesehen** (Freigabe-Auflage 4 bzw. Retro-Vorschlag Sprint 4 der Vornacht:
   `Datei:Zeile` statt zählen), alle innerhalb eines `.field`, alle mit Text direkt im `span`:
   `VaterExerciseCreate.tsx:290` (Freigabe), `ClozeTexts.tsx:272` (Wortpool), `exerciseConfig.tsx:438`
   (Wortpool), `exerciseConfig.tsx:694` (`RowRepeatedField`), `exerciseConfig.tsx:719`
   (`RowRepeatedPairField`), `VaterVocab.tsx:224` und `:600` (Auch richtig). **Ausgeschlossen:**
   `ClozeTexts.tsx:244` – der `span` sitzt in einem `<th>`, und `.table th` (`index.css:303`) trägt dort
   bereits dieselbe Typografie (12 px, uppercase, muted). **Unverändert**, weil sie ein `<label>` umhüllen:
   `InfoHint.tsx:63` (`FieldLabel`), `VaterExerciseCreate.tsx:291`, `PlanPositions.tsx:344,350`,
   `VaterWizard.tsx:608,612`. **Kosten:** keine.
3. **`.info-hint-btn` bekommt KEINE eigene `text-transform`-Gegenregel** – die erste Fassung dieser
   Entscheidung hatte eine, mit der Begründung, der Knopf stehe an fünf Stellen **innerhalb** des `span` und
   erbe dessen `text-transform`, sein „i" (`InfoHint.tsx:41`) würde zum „I". **Gemessen (Review-Fund 1 des
   Sprints):** Die Spec mit der Kopf-Regel und ohne die Gegenregel blieb grün (2 passed) – Formular-Controls
   erben `text-transform` nicht, der UA-Stylesheet von Chromium setzt es auf `button` zurück. Die Regel war
   redundant und ist gestrichen; der Kommentar an `.info-hint-btn` nennt stattdessen, warum keine nötig ist.
   Der zweite E2E-Fall bleibt als **Absichtserklärung** (rot nur bei einem ausdrücklichen
   `text-transform: inherit` am Knopf) und ist so beschriftet. **Kosten:** keine – aber ein Vorsatz: eine
   Vererbungs-Behauptung ist eine Prämisse und gehört gemessen, bevor eine Regel darauf steht.
4. **Die `muted`-Zusätze („(optional)") werden mit uppercase**, wie heute bei jedem `<label>` mit
   `muted`-Kind (`VaterExerciseCreate.tsx:255`, „Lern-Standards (Lehrplan-Positionen erben …)"). Begründung:
   Gleichbehandlung; eine Ausnahme nur für Gruppenköpfe wäre eine zweite Optik. **Kosten:** sichtbare
   Änderung an sieben Stellen – gewollt, das ist der Fix.
5. **Beleg per E2E mit berechnetem Stil statt „keiner".** Begründung: Die Story hielt Typografie für nicht
   zusicherbar; `toHaveCSS` misst im echten Browser gegen das echte Stylesheet, ist vor dem Fix rot
   (`none`/`uppercase`) und ist zugleich der **Rollengang-Ersatz** (Chrome-Extension nicht verbunden,
   Freigabe 6). Was er nicht misst – ob es *gut aussieht* –, bleibt ein benannter Blick des Menschen
   (Step 6, dritter Ausgang). **Kosten:** eine Spec (~10 s).

## Schätzung

**Größe XS** (Anker B-02). `wo: frontend`, keine Migration, kein Vertrag.

**Angriffsplan:** rote Probe (E2E `gruppenkopf-sieht-aus-wie-ueberschrift.spec.ts`) → CSS (`index.css`:
Regel erweitern, `.info-hint-btn`) → sieben Stellen `className="label-row group-label"` → `frontend/CLAUDE.md`
halbe Zeile + Budget-Skript → Suite → Reviewer + `web-design-guidelines` → Commit.

**Risiko:** Die Stelle in `exerciseConfig.tsx:694/719` ist ein wiederverwendeter Baustein (jede Zeile des
Übungs-Editors) – die Optik ändert sich an mehr Flächen, als die Liste nennt; gewollt, da dort überall eine
Gruppen-Überschrift gemeint ist.

**Testweg:** E2E (AK 1 und 3 messbar), `vitest run` als Regressionszeuge, Blick auf die Karte „Übung anlegen"
am Morgen (Mensch).

## Verlauf

- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 1). `frontend-reviewer`: keine Blocker; alle sieben
  Stellen und die fünf Hüllen einzeln bestätigt, kein weiterer `.field`-Erstkind-`span` ohne Klasse
  (Multiline-Grep); die UA-Stylesheet-Prämisse zu Entscheidung 3 unabhängig bestätigt (Chromium/WebKit/Gecko
  setzen auf `button` auch `letter-spacing: normal`). `web-design-guidelines` (Freigabe 7): kein Fund an der
  Typografie; ein Nit zur Semantik (Gruppe ohne `fieldset`/`legend`) → **B-236** (`idee`). Volle
  Playwright-Suite **79/79**, Vitest **394/394**, `tsc -b` sauber. **Offen für den Menschen (Step 6, dritter
  Ausgang):** ob die drei gleichen Überschriften auf „Übung anlegen" und die uppercase gewordenen
  Editor-Köpfe gut aussehen. Commit: siehe Nachtrag.
- **2026-10-23** — **gebaut** (Nachtlauf, Sprint 1): `index.css` bindet die Feld-Typografie an
  `.field label, .field .group-label`; sieben Stellen tragen `label-row group-label` (Entscheidung 2);
  `frontend/CLAUDE.md` eine halbe Zeile, Budget im selben Durchgang bezahlt (10 239 B vor dem Sprint →
  10 221 B danach, bei 9 000 B Budget – die Datei war schon vorher 1 239 B über; drei Link-Dopplungen und
  der B-25-Absatz gekürzt). **Rote Probe (erwartet/gemessen):** E2E `gruppenkopf-sieht-aus-wie-ueberschrift`
  Fall 1 rot an Zeile 36: `text-transform` „uppercase" (vom `<label>` „Lern-Standards" abgelesen) erwartet,
  „none" gemessen. Fall 2 (Hinweis-Knopf) grün vor dem Fix, **und grün ohne die Gegenregel** – siehe
  Entscheidung 3, die Regel ist darum gestrichen. Nach dem Fix: Playwright 5/5 (beide neuen Specs plus
  `kind-lehrbuch-fach`, `lehrwerke`), Vitest **394/394**, `tsc -b` sauber.
  **Rollengang:** Chrome-Extension nicht verbunden (Freigabe 6) → die E2E-Spec misst als Vater auf
  `/vater/exercises/neu` vier Stileigenschaften des Gruppenkopfs gegen das Label daneben; **was ein Mensch
  noch sieht:** ob die Karte „Übung anlegen" mit drei gleichen Überschriften und die sieben umgestellten
  Stellen (Wortpool, Auch richtig, Editor-Zeilen) auch *gut* aussehen – Step 6, dritter Ausgang.
- **2026-10-23** — gegrillt und geschätzt im Nachtlauf (Freigabe 1); User Story und Akzeptanzkriterien
  nachgetragen (der Index meldete sie als fehlend).
- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-11, dort als kosmetischer Fund gemeldet. Von mir
  gegengeprüft: `.label-row` trägt tatsächlich keine Typografie, und die betroffene Zeile stammt aus dem
  Review-Fix der Story selbst.
