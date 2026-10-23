---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-23]
---

# PM-Sitzung: Nachtlauf 2026-10-23 — Formulare, die etwas Unwahres behaupten

**Datum:** 2026-10-23  ·  **Moderation:** PM (autonomer Backlog-Lauf nach [nachtlauf.md](nachtlauf.md))
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Nachschau-Schuld des letzten Laufs (B-235) abtragen und die verwandten kleinen Frontend-Defekte
derselben Familie mitnehmen — jede Story mit roter Probe, Rollengang-Ersatz und Nachschau, ohne eine
Produktentscheidung zu treffen.

**Freigaben:** die acht aus [nachtlauf.md](nachtlauf.md) („Starte einen Nachtlauf", 11:44 Uhr). Die
Chrome-Extension ist in dieser Sitzung **nicht verbunden** (`tabs_context` meldet „not connected") – der
Rollengang läuft darum als E2E-Spec bzw. Komponententest und steht so im `## Verlauf` jeder Story
(Freigabe 6, zweiter Fall).

## Stand — hier einsteigen

Wird je Sprint fortgeschrieben; die letzte Zeile ist der Zustand.

- Ausgangslage: HEAD `3b99bef`, 11 Commits ungepusht (seit zwei Nächten). Index: 67 offen, Nachgeschaut 138
  von 154. Sprint 4 der Vornacht (B-175, B-211, B-199) ist **noch nicht nachgeschaut** – das ist die erste
  Pflichthandlung der Retro dieses Sprints.
- Sprint 1 angesetzt: B-235, B-159, B-176 (alle `art: Defekt`, unter Freigabe 1 autonom gegrillt und geschätzt).
- **Sprint 1 abgenommen** (B-235, B-159, B-176): `tsc -b` sauber, Vitest **394/394**, Playwright **79/79** voll
  plus **3/3** nach den Review-Korrekturen, `frontend-reviewer` ohne Blocker, `web-design-guidelines` ohne
  Fund an der Typografie. **Drei Review-Funde mit Code-/Testwirkung** (Grenze fünf), einer davon aus der
  eigenen Mutanten-Messung. Rollengang als E2E (Browser nicht verbunden). Nachschau Sprint 4 der Vornacht:
  **kein Defekt**, drei `nachgeschaut`-Zeilen mit Prüfpunkt. Ein neuer Prüfauftrag aus dem Design-Skill
  (B-236, `idee`). Retro-Mechanismus nur **vorgeschlagen** (Freigabe 3). Commit: siehe „Änderungsstand".

## Runde 1 — Vorstellung & Feedback

Kein `anmerkungen`-Befund seit dem letzten Lauf (`docs/anmerkungen/` unverändert). Die Rollen sprechen darum
aus dem Backlog, nicht aus einem frischen Walk: Dieser Lauf beginnt mit dem Fund, den die Nachschau der
Vornacht hinterlassen hat, und sucht seine Nachbarn.

### Feedback Vater (O-Ton)

- **Stört:** „Ich klicke beim Lehrbuch meines Sohnes auf ‚Bearbeiten', und die Unit steht als ‚Unit 1 –
  Greetings (nicht geladen)' da – obwohl sie eine Sekunde später ganz normal erscheint. Beim Plan habt ihr
  das letzte Nacht repariert, hier nicht." (B-235, aus der Nachschau zu B-229)
- **Stört:** „Auf ‚Übung anlegen' sehen ‚Schularten' und ‚Lern-Standards' aus wie Überschriften, ‚Freigabe
  (Startzustand)' wie ein Satz im Fließtext. Ist das ein dritter Abschnitt oder eine Erklärung?" (B-176)
- **Top-3:** 1. B-235, 2. B-176, 3. B-164 (die Architekturfrage bleibt beim Nutzer, Freigabe 1).

### Feedback Creator (O-Ton)

- **Stört:** „Bei ‚Découvertes 1' und ‚Green Line 1' steht ‚Diese Reihe hat jemand anderes angelegt'. Wer?
  Die kamen mit der App. Beim Fach sagt ihr seit B-154 ‚gehört zum Grundbestand' – bei der Reihe nicht."
  (B-159; am Seed nachgesehen: **alle** geseedeten Reihen sind ownerlos, `Seed.cs:611`, `:1017`, `:1036-1037`)
- **Top-3:** 1. B-159, 2. B-141/B-145 (fachliche Fragen, gesperrt), 3. B-166 (Wiki nennt die Kapitel-Route).

### Feedback Sohn (O-Ton)

- Keine Story seiner Fläche ist in diesem Sprint baubar; die offenen Punkte (B-226 zwei Münzstände, B-220
  Learning Queue, B-218 Mischwiederholung) sind `Frage`/`Wunsch` und gesperrt. Er ist in diesem Sprint
  Regressionszeuge.

## PM-Synthese & Priorisierung (→ Entwickler)

Der Lauf darf nur `Defekt`/`Aufräumen` grillen und `geschaetzt`e Stories bauen. Baubar und noch nicht
geschätzt sind sechzehn Defekte und fünf Aufräum-Stories; davon gehören drei einer Familie an, die die letzten
drei Nächte geprägt hat: **die Oberfläche behauptet einen Zustand, den der Server nicht geliefert hat** – ein
Ladefenster als Ausfall (B-235), einen fremden Eigentümer, wo es keinen gibt (B-159), Fließtext, wo eine
Überschrift gemeint ist (B-176).

| Prio | Story | Art | Wo | Größe | Warum jetzt / warum nicht |
| --- | --- | --- | --- | --- | --- |
| P0 | B-235 | Defekt | frontend | XS | Nachschau-Schuld des letzten Laufs, Fix benannt |
| P1 | B-159 | Defekt | frontend | XS | Zwilling von B-154, Seed macht es zum Normalfall |
| P1 | B-176 | Defekt | frontend | XS | Review-Fix hat die Optik mitgenommen; Rolle statt Element |
| – | B-201 | Defekt | frontend | XS | **draußen:** wartet auf B-186 (`Frage`, gesperrt) |
| – | B-141, B-145 | Defekt | backend | – | **draußen:** die Story selbst nennt eine fachliche Frage („ist Eindeutigkeit gewollt?") – das ist Produktrichtung |
| – | B-134 | Defekt | frontend | M? | **draußen:** Sweep über 13+7 Stellen mit Einzelentscheidung je Fundstelle und einem neuen Wächter – zu groß für einen Sprint neben drei anderen; Kandidat für Sprint 2 |
| – | B-181, B-190, B-131, B-166 | Defekt | doku/scripts | – | andere Familie (Werkzeug/Doku sagt, was der Code tut); Kandidat für Sprint 2 |
| – | B-164 | Defekt | frontend | – | `wartet_auf` eine Entscheidung des Nutzers |

**Roter Faden:** *Drei Formulare des Vaters und des Creators sagen nur noch, was wahr ist.*

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel, aus zwei Rollensichten:**

> Der **Vater** öffnet das Lehrbuch seines Kindes und liest „(lädt…)", solange die Units unterwegs sind – nie
> „(nicht geladen)", wenn nichts ausgefallen ist; auf „Übung anlegen" erkennt er jede Gruppen-Überschrift als
> solche. Der **Creator** liest an einer Reihe aus dem Grundbestand, dass sie niemandem gehört – nicht, dass
> jemand anderes sie angelegt habe.

**Umfang** (alle `art: Defekt`, in dieser Sitzung von `ausformuliert` auf `geschaetzt` gebracht):

| Story | Richtung derselben Wurzel |
| --- | --- |
| [B-235](backlog/B-235-unit-feld-sagt-nicht-geladen-waehrend-des-ladens.md) | Das Unit-Feld sagt „(nicht geladen)", solange die Units erst laden |
| [B-159](backlog/B-159-reihe-ohne-owner-behauptet-fremden-ersteller.md) | Eine Reihe ohne Eigentümer behauptet einen fremden Ersteller |
| [B-176](backlog/B-176-gruppen-ueberschrift-verliert-die-feld-optik.md) | Eine Gruppen-Überschrift rendert als Fließtext |

**Draußen, mit Namen:** siehe Tabelle oben (B-201, B-141, B-145, B-134, B-164 und die Doku-Familie).

**Refinement, getrennt benannt:** drei Stories von `ausformuliert` nach `geschaetzt` – je Story die offenen
Punkte am Code entschieden (B-235: `loading` statt `showPlaceholder`, Begründung in der Story; B-159: Seed
nachgesehen, Wortlaut von B-154 übernommen, kein geteilter Baustein; B-176: sieben Fundstellen einzeln
klassifiziert, eine davon ausgeschlossen). Das Refinement war die größere Hälfte; der Bau ist je Story eine
Zeile bis eine Handvoll Zeilen.

### Entwickler-Brief

- **Ziel:** siehe Sprint-Ziel. Kein Backend berührt, kein Vertrag, keine Migration.
- **B-235:** `laedt={units.loading}` am Unit-`ReferenceSelect` in `ChildMaterialSection.tsx`; `loading`
  statt `showPlaceholder`, weil die Unit-Liste an einem Schlüssel hängt (Reihenwechsel lädt neu) und
  `showPlaceholder` nach dem ersten Abschluss dauerhaft falsch ist – nach einem Fehlschlag ist `loading`
  ebenfalls falsch, also bleibt dort „(nicht geladen)". Doc-Kommentar am `laedt`-Prop nachziehen.
- **B-159:** Zweiter Zweig am Satz in `SeriesRow` (`VaterLehrwerke.tsx`), `ownerAdultId == null` →
  „gehört zum Grundbestand"; als kleines exportiertes Bauteil, damit der Komponententest es ohne gefälschtes
  `fetch` rendern kann (Muster `SubjectRow`/`CatalogAdmin.test.tsx`).
- **B-176:** Klasse `group-label` für Überschriften über N Feldern; `index.css` bindet die Typografie an
  `.field label, .field .group-label`; `.info-hint-btn` bekommt `text-transform: none`, weil sein „i" sonst
  im Gruppenkopf zu „I" würde. Sieben Fundstellen umgestellt; `ClozeTexts.tsx:244` (im `<th>`) bleibt – die
  Tabellenkopf-Typografie trägt dort schon.
- **Reihenfolge:** rote Proben (Vitest B-235 + B-159, E2E B-176) → Fixes → `tsc -b`, `vitest run`,
  `playwright test` → `frontend-reviewer` (+ `web-design-guidelines`, Freigabe 7) → Commit → Nachschau
  Sprint 4 der Vornacht → Retro (Vorschlag, nicht landen).
- **Testweg:** je Story in `## Schätzung`.

## Iteration 1 — umgesetzt

**Frontend (kein Backend berührt, kein Vertragsbruch, keine Migration):**

- B-235: `laedt={units.loading}` am Unit-`ReferenceSelect` in `ChildMaterialSection.tsx`; der Doc-Kommentar des
  `laedt`-Props in `ReferenceSelect.tsx` sagt jetzt, wann `showPlaceholder` und wann `loading` hineingehört – und
  dass „lädt auf Seitenebene" am `useAsync` nachzusehen ist, nicht zu zählen.
- B-159: neues exportiertes Bauteil `SeriesEigentumsHinweis` (`VaterLehrwerke.tsx`), `SeriesRow` rendert es bei
  `!isOwn`; Wortlaut von B-154 („gehört zum Grundbestand"), `== null` für das optionale Vertragsfeld.
- B-176: `index.css` bindet die Feld-Typografie an `.field label, .field .group-label`; sieben Stellen tragen
  `label-row group-label` (Liste mit `Datei:Zeile` in der Story); `frontend/CLAUDE.md` eine halbe Zeile, das
  Budget im selben Durchgang bezahlt (10 239 → 10 221 B; die Datei war schon vorher 1 239 B über 9 000).
- Tests: `TextbookForm.test.tsx` +2 Fälle (`vi.spyOn(api, "seriesUnits")`), ein Altfall auf `findByRole`;
  `VaterLehrwerke.test.tsx` +3 Fälle; drei neue E2E-Specs (`lehrbuch-units-laden-noch`,
  `gruppenkopf-sieht-aus-wie-ueberschrift`, `reihe-aus-dem-grundbestand`).

**Verifikation:** `tsc -b` sauber · `vitest run` **394/394** (47 Dateien; vorher 389) · `playwright test`
**79/79** (4,0 min) gegen den Stand vor den Review-Korrekturen, danach die drei betroffenen Specs **3/3**
(`lehrbuch-units-laden-noch`, `reihe-aus-dem-grundbestand`, `lehrwerke`).

**Rote Proben (erwartet/gemessen), vor dem Fix in je einem Lauf gemessen:**

| Probe | Erwartet | Gemessen |
| --- | --- | --- |
| Vitest B-235, Fall „(lädt…)" | Option „Unit 7 (lädt…)" | keine; im DOM „Unit 7 (nicht geladen)" |
| Vitest B-235, Fall „nach Fehlschlag" | – | **grün vor dem Fix**, als Gegenprobe beschriftet |
| Vitest B-159, drei Fälle | Satz je Fall | „Element type is invalid … undefined" (Bauteil fehlt) |
| E2E B-235, Zeile 59 | `/\(lädt…\)$/` | „Unit 1 – Greetings (nicht geladen)" |
| E2E B-176, Fall 1 | `text-transform: uppercase` (vom Label abgelesen) | `none` |
| E2E B-176, Fall 2 (Hinweis-Knopf) | – | **grün vor dem Fix** und grün ohne die Gegenregel → Fund 1 |

Zusammen: Vitest **4 rot von 30** in den zwei Dateien, Playwright **2 rot von 3**.

### Review-Funde (Zähler: 3 von 5)

| # | Quelle | Fund | Behebung |
| --- | --- | --- | --- |
| 1 | eigene Mutanten-Messung | Die Gegenregel `.info-hint-btn { text-transform: none }` stand auf einer falschen Prämisse: Formular-Controls erben `text-transform` nicht, der UA-Stylesheet setzt es zurück – die Spec blieb ohne die Regel grün (2 passed) | Regel gestrichen, Kommentar nennt stattdessen, warum keine nötig ist; Entscheidung 3 in B-176 umgeschrieben; Spec-Fall 2 als Absichtserklärung beschriftet |
| 2 | `frontend-reviewer` | Die „kein Banner im Fenster"-Zeile in `lehrbuch-units-laden-noch` fragte `getByRole("alert")` wie das Plan-Vorbild – der Units-Banner trägt keine Rolle, die Abfrage zählte immer 0 | Abfrage per Text (`/^Units: /`); die fehlende Rolle bleibt B-134 |
| 3 | `frontend-reviewer` (Nice-to-have) | Die Verdrahtung von `SeriesEigentumsHinweis` in `SeriesRow` war unbelegt – Zeile streichbar bei 394/394 grün | eigene Spec `reihe-aus-dem-grundbestand.spec.ts`, zugleich der Creator-Rollengang |

Ohne Zähler (Kommentar/Story): Kommentar am Altfall in `TextbookForm.test.tsx` (warum er den echten Fehlschlag
nimmt); `web-design-guidelines`-Nit „Gruppe ohne `fieldset`/`legend`" → **B-236** (`idee`, `Defekt`,
`unverifiziert`).

## Runde 1 — Re-Review / Abnahme

Die Chrome-Extension ist nicht verbunden (`tabs_context`: „not connected") – der Rollengang läuft nach
Freigabe 6 als E2E-Spec und steht so im `## Verlauf` jeder Story.

- **Vater (Supervisor) – signed off, mit Vorbehalt Optik:** „Ich öffne das Lehrbuch, und solange die Units
  laden, steht ‚Unit 1 – Greetings (lädt…)', das Feld ist gesperrt, kein Banner – danach die Unit ganz
  normal" (`lehrbuch-units-laden-noch`, im 2-s-Fenster gemessen). „Auf ‚Übung anlegen' hat ‚Freigabe
  (Startzustand)' dieselben vier Stileigenschaften wie ‚Lern-Standards'" (`gruppenkopf-sieht-aus-wie-ueberschrift`).
  **Was er selbst noch sehen muss:** ob die Karte mit drei gleichen Überschriften und die sieben umgestellten
  Köpfe (Wortpool, Auch richtig, Editor-Zeilen – jetzt Versalien inklusive „(optional)") *gut* aussehen.
- **Creator – signed off:** „Ich klappe ‚Green Line 1' auf und lese ‚gehört zum Grundbestand', nicht ‚hat
  jemand anderes angelegt'" (`reihe-aus-dem-grundbestand`, 1/1; Komponententest drei Fälle). Keine
  Rechtefolge, die Knöpfe fehlen bei `!isOwn` wie vorher.
- **Sohn (Student) – Regressionszeuge:** keine Fläche seiner Seite im Diff; volle Suite 79/79, darunter seine
  Specs (`shop-verlauf`, `shop-verlauf-wiederholen`, `uebung-*`, `full-flow`). Kein O-Ton erfunden.

## Retrospektive

**Nachschau:** B-175, B-211, B-199 (Sprint 4 der Vornacht, Commit `49b1fbc`) – je Story der Prüfpunkt im Feld
`nachgeschaut`: B-175 die Verweise auf B-139 (dort „sechs Nächte" an drei Stellen, eine Quelle) und `e2e.yml`
ohne Testzahl; B-211 die Sperrbedingung `ExerciseEditModal.tsx:764` gegen die Reihenfolge Wählen → `rerender`
→ Zusicherung (Mutante ohne `disabled ||` bliebe freigegeben); B-199 die Behauptung „Erneut suchen ruft
`exercises.reload()`" gegen `VaterWizard.tsx:576/735` und die beiden `useAsync`-Behauptungen im Hook-Docblock
gegen `useAsync.ts`. **Kein Defekt.** · Index: **Nachgeschaut 141 von 157**.

**Was die eigenen Tore durchgelassen haben – und was von außen kam:** nichts von außen (keine neue
`anmerkungen`-Zeile seit dem 2026-08-14, kein manuelles Review). Innen drei Funde, alle **vor** dem Commit, und
sie haben eine Form:

1. **Eine Vererbungs-Prämisse als Regel gelandet, ohne sie zu messen** (Fund 1). „`text-transform` wird
   vererbt, also braucht der Knopf eine Gegenregel" – wahr für Elemente, falsch für Formular-Controls. Kein Tor
   kann eine CSS-Cascade-Behauptung prüfen; gefunden hat es die Mutante, die ich nur gefahren habe, weil der
   Spec-Kommentar „(gemessen)" behauptete und ich es dann auch tun musste.
2. **Eine Zusicherung aus der Nachbar-Spec kopiert, deren Voraussetzung hier nicht gilt** (Fund 2). Die
   Absichtserklärung war korrekt beschriftet („vorher grün") und trotzdem falsch in ihrem *Versprechen*, wann
   sie rot würde – weil der Banner dieser Fläche keine Rolle trägt. Die Regel aus `CLAUDE.md` („welche Änderung
   macht ihn rot?") war beantwortet, nur die Antwort stimmte nicht.
3. **Das Bauteil belegt, die Stelle nicht** (Fund 3) – dieselbe Familie wie B-229 (Regel am Bauteil, Stelle nicht
   angeschlossen) und B-216.

Gemeinsam ist 1 und 2: eine **Absichtserklärung**, deren „rot wenn …" eine ungeprüfte Behauptung war. Die
eine, die ich mutiert habe, fiel als falsch auf; die andere nicht, bis der Reviewer sie las.

**Prämisse geprüft:** `docs/nachtlauf.md`, Auflage 5, verlangt die Zahl „erwartet/gemessen" nur für die
**rote Probe**; für Absichtserklärungen verlangt die Root-`CLAUDE.md` („Eine Zusicherung, die den
Ausgangszustand prüft …") die *Benennung* der Änderung, die sie rot macht – nicht ihre Messung. Beide Stellen
nachgelesen, nicht erinnert.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Auflage 5 auf Absichtserklärungen ausdehnen** (Prosa in `docs/nachtlauf.md`, Auftragstext): *„Jede als
Absichtserklärung beschriftete Zusicherung nennt die Mutante, die sie rot macht – und die Mutante wird einmal
gefahren, mit Zahl."* Beleg aus diesem Sprint: eine gefahrene Mutante (Fund 1) deckte eine falsche Prämisse
auf, eine nicht gefahrene (Fund 2) blieb bis zum Reviewer unentdeckt. **Kosten:** ein Spec-Lauf je
Absichtserklärung (~1 min), und die Versuchung, Absichtserklärungen wegzulassen statt zu messen – das wäre der
schlechtere Zustand und muss im Text stehen. **Kein Tor:** eine Mutante ist nicht mechanisierbar, sie ist eine
Handlung. **Bewusst ungedeckt:** ob die Optik *gut* ist (Step 6, dritter Ausgang) – und Fund 3, für den die
Vornacht schon einen Vorschlag trägt („die anderen Stellen mit `Datei:Zeile` belegen"); er wird hier nicht
verdoppelt.

**Warum der Lauf weitergeht:** kein neues Tor, Zähler 3 von 5, Nachschau ohne Defekt – keine der drei
Halt-Bedingungen (`nachtlauf.md`, Freigabe 3) greift.
