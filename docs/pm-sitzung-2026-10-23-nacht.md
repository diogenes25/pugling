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
  (B-236, `idee`). Retro-Mechanismus nur **vorgeschlagen** (Freigabe 3). Commit `7844894` + Nachtrag `738ef99`.
- **Sprint 2 abgenommen** (B-190, B-131, B-166 – Werkzeug und Doku sagen, was wahr ist): Harness-Proben P1–P3,
  Index idempotent, Backend **877/877**, Vitest **395/395**, Lint 0, Reviewer (allgemeiner Agent) ohne
  Blocker; **drei Review-Funde mit Code-/Testwirkung** (abgeschnittener Sweep, hohle 400-Probe, literaler
  Zeilenumbruch). Nachschau Sprint 1: **kein Defekt**, eine Test-Lücke geschlossen (Mutante 1 rot von 10), zwei
  Berichtigungen. Drei neue Stories aus Funden: B-237, B-238, B-239. Retro schlägt ein **Tor** vor (400 ohne
  `code`), nicht gelandet. Commit `95b0624` + Nachtrag `1372d0e`.
- **Sprint 3 abgenommen** (B-158, B-179 – eine Stelle folgt wieder dem Muster der Nachbarn): rote Probe
  PATCH-Leername 200 → 400, Absichtserklärung unter Mutante rot, Backend **879/879**, `pugling-reviewer`
  ohne Blocker, **drei Funde mit Codewirkung** (verwaister Doc-Block, fehlender 400 in Swagger, `Create` als
  zweite Projektion). Nachschau Sprint 2: **zwei Defekte** (B-240: der Index meldet `groesse`/`wo` nie und
  B-131 war mit ⚠ abgenommen; B-241: falscher Cascade-Kommentar – korrigiert – und sechs Doku-Stellen).
  **Der Lauf endet hier** (Limit 2). Retro schlägt ein Tor für den Wächter selbst vor, nicht gelandet.
  Commit `98c8431` + Nachtrag `03cb16e`.
- **Zweiter Lauf desselben Tages (ab 13:17, Auftrag „starte einen nachtlauf"):** HEAD `9f45907`, 18 Commits
  ungepusht. Die Chrome-Extension ist diesmal **verbunden** (Freigabe 6, erster Fall). Sprint 4 angesetzt: B-240,
  B-241, B-238 – die drei `Defekt`-Stories, die die Nachschau des ersten Laufs hinterlassen hat (alle `wo: doku`).
  Erste Pflichthandlung der Retro: Nachschau zu Sprint 3 (B-158, B-179, Commit `98c8431`).
- **Sprint 4 abgenommen** (B-240, B-241, B-238 – Werkzeug und Doku behaupten keine Ebene mehr, die es nicht gibt):
  Harness P4–P6 rot→grün mit Zahl, curl-Durchlauf der `Creator.http` 13/24 rot → 23/23 2xx, Kapitel-Sweep 16 → 1
  (gewollt), Backend **879/879**, markdownlint 0, Reviewer (allgemeiner Agent) ohne Blocker, **null Funde mit
  Code-/Testwirkung** (vier mit Doku-Wirkung, behoben). **Rollengang im Browser** (Vater auf `/vater/exercises`
  und `/neu`), Creator per curl-Flow, PM am Index. Nachschau Sprint 3 des ersten Laufs: **ein Kommentar-Defekt**
  (B-243, korrigiert) + zwei Funde derselben Familie (B-244, B-245). Vier neue Stories (B-242–B-245). Retro
  schlägt ein **Tor** vor (Routen in `.http`-Dateien gegen OpenAPI), nicht gelandet. Commit `7c33765` + Nachtrag.
- **Sprint 5 abgenommen** (B-243, B-244, B-237 – Kopien verschwinden, Hilfe und Nachbar sagen dasselbe wie der
  Code): Mutante 1 rot von 5, Hilfe-Zählung 1/0 → 0/1, Grep `IsAdmin = true` 2 → 1 Dateien, Backend
  **879/879**, `pugling-reviewer` ohne Blocker, **zwei Funde mit Codewirkung** (Hilfetext, Zähler 2 von 5).
  Nachschau Sprint 4: **drei Defekte in B-241** (zwei Sätze beim Wortwechsel falsch, Sweep vor der letzten
  Änderung gemessen) → **B-246**, im Sprint behoben. **Der Lauf endet hier** (Limit 2). Retro schlägt vor, den
  Sweep an das Ende des Sprints zu binden, nicht an den Fix – kein Tor, nur Reihenfolge. Commit `4954bd2` + Nachtrag.
- **Dritter Lauf (ab 20:03, Paket-Auftrag NP-1, NP-2, NP-3):** HEAD `896d1b2`, Browser verbunden.
- **Sprint 6 abgenommen** (NP-1: B-36, B-35 – der Sohn sieht und hört Teilziele, die Karte dreht sich): Vitest
  **402/402**, Playwright **81 passed**, Backend **879/879**, `frontend-reviewer` + Delta ohne Restfund, **fünf
  Funde mit Code-/Testwirkung** (Grenze erreicht, nicht überschritten). Rollengang im Browser. Nachschau Sprint 5:
  drei Entgleitungen → **B-256**, behoben. Commit `39cf33b` + Nachtrag. Zwei neue `idee`-Stories (B-254, B-255). Beide Stories `wartet_auf`
  einen Menschen-Check (Gefühl der Animation).
- **Sprint 7 angehalten** (NP-2: B-15, B-17, B-21): gebaut und grün (Backend **885/885**, Vitest **406/406**,
  `uebungstypen` 1 passed, zwölf Mutanten am richtigen Ort), aber **sechs Review-Funde mit Code-/Testwirkung**
  (kein Blocker) – über der Grenze von Freigabe 3. **Der Lauf endet hier**, NP-3 nicht angefangen. Stories auf
  `in-arbeit` mit `wartet_auf`, kein Rollengang, keine Retro. Stand committet (`93f227a`).

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

## Sprint 2 — Ziel & Umfang

**Sprint-Ziel, aus der PM-Sicht (und der des Lesers der Wissenskarte):**

> Der **PM** kann den Backlog-Index aus jedem Verzeichnis anstoßen und bekommt entweder den richtigen Index
> oder einen Abbruch mit Grund – nie einen leeren Index mit Erfolgsmeldung; eine leere Story-Datei steht in der
> ⚠-Tabelle statt im Nichts. Wer der Root-`CLAUDE.md` zur Wissenskarte folgt, liest dort keine Route und
> keine Katalogebene mehr, die B-106 entfernt hat.

Dieselbe Familie wie Sprint 1 – **ein Fehlschlag gibt sich als gültiges Ergebnis aus** –, nur am Werkzeug und
an der Doku statt an der Oberfläche.

**Umfang** (alle `art: Defekt`, in dieser Sitzung autonom gegrillt und geschätzt, Freigabe 1):

| Story | Richtung derselben Wurzel |
| --- | --- |
| [B-190](backlog/B-190-backlog-index-schreibt-leeren-index.md) | Aus einem Unterverzeichnis aufgerufen, schreibt der Index-Generator einen leeren Index – mit „Erfolg" |
| [B-131](backlog/B-131-leere-story-faellt-aus-dem-index.md) | Eine leere Story-Datei fällt aus beiden Tabellen, auch aus der ⚠-Liste |
| [B-166](backlog/B-166-wiki-nennt-die-entfernte-kapitel-route.md) | Wiki, Tutorials und Wissenskarte nennen die entfernte Kapitel-Ebene |

**Draußen, mit Namen:** B-181 (Punkt 2 ist eine Verhaltensfrage im Produktionspfad – nicht nachts), B-134
(Sweep über 20 Stellen mit Einzelentscheidung – zu groß neben drei anderen), B-138/B-156/B-158/B-179
(`Aufräumen`, andere Familie), sowie die zwei Funde aus dem Sweep zu B-166, die eigene Stories wurden:
**B-237** (KI-Creator-Hilfe nennt `--chapter`) und **B-238** (`docs/REST/Creator.http` fährt die tote Route
in ausführbaren Requests).

**Refinement, getrennt benannt:** B-190s Ist-Stand war präziser zu fassen (die Existenzprüfung gab es, sie
prüfte vor dem `cd`), B-131s Punkt 3 war nachzustellen statt zu vermuten, B-166s Ist-Stand war zu zwei
Dritteln durch Commits vom 2026-10-17 überholt – das Refinement hat jede der drei Stories am Code neu
vermessen, und das war die größere Hälfte.

### Entwickler-Brief

- **B-190/B-131 (ein Diff in `.claude/scripts/backlog-index.sh`):** awk-Pfad absolut (`cd … && pwd`) vor dem
  `cd "$root"`; leere Dateien vor awk aussortieren und als `| B-nnn | \`?\` | Datei ist leer |` melden; awk in
  eine Datei statt per Prozess-Substitution, Exit ≠ 0 oder null Datensätze bei nicht-leeren Dateien → Exit 1
  ohne Schreiben. **Beleg:** Harness im Scratchpad (Wegwerf-Root mit Marker-README, zwei echten Stories,
  leerer Datei, Datei ohne Frontmatter, kaputter awk-Kopie), drei Proben P1–P3 vor und nach dem Fix; echter
  Index aus Root **und** Unterverzeichnis mit leerem Diff.
- **B-166:** Grep-Sweep (`chapters/`, `chapterId`, `Kapitel`), Fundstellen einzeln umschreiben (Liste mit
  `Datei:Zeile` in der Story); fünf englische Code-Kommentare, darunter der Swagger-sichtbare
  `AuthoringRoute`-Text; `dotnet test` zieht `docs/openapi/v1.json` nach.
- **Reihenfolge:** Proben rot → Skript-Fix → Proben grün → Idempotenz → Doku/Kommentare → `dotnet test` →
  Lint → Reviewer (allgemeiner Agent: Skript + Doku; kein C#-Verhalten) → Commit → Nachschau Sprint 1 →
  Retro (Vorschlag).

## Iteration 2 — umgesetzt

**Werkzeug (`.claude/scripts/backlog-index.sh`, B-190 + B-131, ein Diff):** awk-Pfad absolut vor dem `cd`;
leere Story-Dateien werden vor awk aussortiert und als ⚠-Zeile „Datei ist leer" gemeldet; awk schreibt in
eine Temp-Datei, Exit ≠ 0 oder null Datensätze bei nicht-leeren Dateien → Exit 1 ohne Schreiben.

**Doku und Kommentare (B-166):** elf Doku-Stellen in sieben Dateien und sechs englische Code-Kommentare
(darunter der Swagger-sichtbare `AuthoringRoute`-Text; `docs/openapi/v1.json` aus dem Testlauf nachgezogen,
eine Zeile), Liste mit `Datei:Zeile` in der Story. Zwei Funde aus demselben Sweep als eigene Stories:
**B-237** (KI-Creator-Hilfe `--chapter`), **B-238** (`docs/REST/Creator.http` fährt die tote Route in
ausführbaren Requests – ab Zeile 62 liefert das Tutorial 404).

**Nachschau-Arbeit an Sprint 1 (im selben Diff):** vierter Vitest-Fall zu B-235 (Entscheidung 1 hielt kein
Test), Docblock in `ReferenceSelect.tsx` umformuliert, zwei Story-Sätze berichtigt – siehe Retrospektive.

**Rote Proben (erwartet/gemessen), im Wegwerf-Root (`scratchpad/index-probe.sh`), vor dem Fix:**

| Probe | Erwartet | Gemessen |
| --- | --- | --- |
| P1 relativer Aufruf aus `docs/backlog` | Index 2 offen / 1 abgenommen | `awk: fatal: cannot open source file`, „Index geschrieben: 0 offen, 0 abgenommen", **Exit 0**, README leer überschrieben |
| P2 awk mit Syntaxfehler | Abbruch ohne Schreiben | awk-Fehler, „Index geschrieben: 0 offen", **Exit 0** |
| P3 leere Datei `B-900` | ⚠-Zeile | **keine Zeile** – in keiner Tabelle (B-901 ohne Frontmatter: `?` in beiden, Punkt 3 damit beantwortet) |
| Grep AK 1 (B-166) | 0 Treffer in lebender Doku | `wiki/08-erweitern.md:135-136`, `docs/tutorial-supervisor.md:307,316` |
| Vitest B-235, vierter Fall (Nachschau) | Option „Unit 7 (lädt…)" | unter dem Mutanten `showPlaceholder` **1 rot von 10**, keine Option |

**Nach dem Fix:** P1 „2 offen, 1 abgenommen", Index vollständig · P2 „awk ist gescheitert – Index nicht
geschrieben.", **Exit 1**, README unverändert · P3 `| [B-900](B-900-leer.md) | \`?\` | Datei ist leer |` ·
Grep 0 · Vitest 10/10 in der Datei.

**Verifikation:** echter Index aus Root **und** per `cd docs/backlog && bash ../../…` je „65 offen, 157
abgenommen, 13 verworfen", `git diff` leer (idempotent), Laufzeit ~1,6 s · `dotnet test Pugling.sln -c
Release` **877/877** (45 s; `docs/openapi/v1.json` eine Zeile nachgezogen) · `tsc -b` sauber · Vitest
**395/395** · Markdown-Lint **0 Funde in 345 Dateien**.

### Review-Funde (Zähler: 3 von 5)

Reviewer: ein allgemeiner Agent mit Review-Brief (Skript + Doku + Kommentare; kein C#-Verhalten im Diff) –
der `pugling-reviewer` ist für Backend-Verhalten gedacht, der `frontend-reviewer` für `frontend/`.

| # | Quelle | Fund | Behebung |
| --- | --- | --- | --- |
| 1 | Reviewer | Mein Kommentar-Sweep war **abgeschnitten** (`grep … \| head -8`): elf weitere `chapter`-Kommentare in acht Dateien (`Program.cs:211`, `ChildLearnProgressService.cs` ×6, `KeyResultsController.cs:10`, `ObjectiveEntities.cs:47`, `PuglingDbContext.cs:941`, `AdultLifecycleTests.cs:15`, `Seed.cs:430/558`, `DraftRules.cs:45`) – AK 2 von B-166 war behauptet, nicht erfüllt | alle umgeschrieben; Liste in B-166, Entscheidung 2 |
| 2 | Reviewer (aus demselben Grep) | **Hohle Probe** `ObjectiveTests.cs:167-173`: „ClassTestGrade mit Kapitel-Scope → 400" schickte `chapterId = 1` – kein Vertragsfeld, also `unknown_field` (400) **vor** der Scope-Regel; grün unter jeder Regel (vorbestehend, nicht aus diesem Diff – aber die einzige Stelle, an der die tote Ebene noch änderte, was ein Test beweist) | echte Unit per `TestApi.CreateSeriesAndUnitAsync`, Feld `seriesUnitId`, Zusicherung auf `code == validation_error`; **Mutante** ohne `seriesUnitId is not null` in `ObjectiveService.cs:79`: **1 rot von 1**, Original 1/1 |
| 3 | Reviewer (Nit) | Der Zeilenumbruch der neuen ⚠-Zeile stand als literaler Umbruch in `$'…'`, die Nachbarzeilen schreiben `$'\n'` – gleichbedeutend, aber zerbrechlich unter Editoren, die Zeilenenden anfassen | auf `$'\n'` gezogen, Proben P1–P3 erneut grün |

Ohne Zähler: der Index in `docs/backlog/README.md` war gegen den Arbeitsbaum veraltet (B-237/B-238 fehlten,
drei Stufen alt) – er wird vor dem Commit regeneriert, wie immer; die „git diff leer"-Zeile in B-190 meint
Root gegen Unterverzeichnis und ist so präzisiert. Nits benannt, nicht behoben: `for f in "${files[@]}"`
ohne Guard (nur bash < 4.4 betroffen, Git-Bash 5.x und CI sind ≥ 5), ⚠-Zeilen leerer Dateien stehen immer
oben (kosmetisch), kein `trap` für die Temp-Datei (vorbestehend, in B-190s Schätzung benannt), Laufzeit auf
dem Reviewer-Rechner 14 s (Prozessstart, kein Regressionssignal). `wiki/08-erweitern.md:41` zeigt deutsche
`/// <summary>` gegen die Englisch-Regel → **B-239** (`idee`, `Aufräumen`).

## Runde 2 — Re-Review / Abnahme

- **PM (Werkzeug) – signed off:** „Ich rufe den Index aus `docs/backlog` mit relativem Pfad auf und bekomme
  denselben Index wie aus dem Root; mit kaputtem awk bekomme ich ‚awk ist gescheitert – Index nicht
  geschrieben.' und Exit 1, die README bleibt; eine leere Story steht in der ⚠-Tabelle." (Harness P1–P3,
  echter Index idempotent.) „Die Wissenskarte, das Wiki-Beispiel und das Supervisor-Tutorial nennen keine
  Kapitel-Route und kein `chapterId` mehr" (Grep AK 1: 0; Swagger-Text in `v1.json` nachgezogen).
- **Creator, Vater, Sohn – Regressionszeugen:** kein Produktverhalten im Diff. Backend **877/877** (die
  reparierte Probe inklusive), Vitest **395/395**, `tsc -b` sauber. Die Playwright-Suite lief in diesem Sprint
  nicht erneut – seit dem 79/79-Lauf änderten sich unter `frontend/` nur ein Docblock, ein Spec-Kommentar
  mit Textabfrage (danach 3/3), eine neue Spec (1/1) und ein Vitest-Fall; kein Pfad einer Rolle berührt.
  Kein O-Ton erfunden.
- **Was ein Mensch noch sieht:** nichts Sinnliches in diesem Sprint; B-238 (`Creator.http`) wartet auf einen
  REST-Client-Durchlauf von Hand.

## Retrospektive Sprint 2

**Nachschau:** B-235, B-159, B-176 (Sprint 1 dieses Laufs, Commit `7844894`) – frischer `frontend-reviewer`,
von mir gegengelesen; Prüfpunkte je Story im Feld `nachgeschaut` (sechs Zustände des `laedt`-Felds, alle
sieben anderen `ReferenceSelect`-Stellen am speisenden `useAsync`, Grep über die Eigentümer-Sätze, alle
`group-label`-Stellen und die gekürzten `CLAUDE.md`-Zeilen gegen Code und Config). **Kein Defekt** – aber
eine **Test-Lücke**: B-235s Entscheidung 1 (`loading` statt `showPlaceholder`) hielt kein Test, der Mutant
überlebte 42/42 + E2E; vierter Vitest-Fall ergänzt, **unter dem Mutanten 1 rot von 10**. Dazu zwei
Berichtigungen: ein Docblock in `ReferenceSelect.tsx` behauptete einen Mechanismus („bevor das Formular
aufgeht"), den keine Seite hat; und die Story-Zeile „ein Vitest müsste den Zwischen-Render nachspielen" war
falsch (drei kontrollierte Promises reichen). · Index: **Nachgeschaut 144 von 160**.

**Was die eigenen Tore durchgelassen haben – und was von außen kam:** nichts von außen. Innen drei Funde, alle
vor dem Commit, zwei vom Reviewer, einer aus der Nachschau:

1. **Ein abgeschnittener Sweep als erfülltes Kriterium.** AK 2 von B-166 („in keinem Code-Kommentar mehr")
   stand auf einem Grep mit `| head -8` – elf Treffer dahinter hat niemand gesehen. Das ist die Familie
   „gezählt statt nachgesehen" der Vornacht, in neuer Gestalt: die Liste *war* `Datei:Zeile`, nur nicht
   vollständig. Der Vorschlag der Vornacht hätte es nicht gefangen.
2. **Eine 400-Zusicherung, die unter jeder Regel grün ist.** `ObjectiveTests.cs:167-173` schickte ein Feld,
   das der Vertrag nicht kennt; `UnmappedMemberHandling.Disallow` antwortet darauf mit 400, bevor die geprüfte
   Regel läuft. Vorbestehend, aber die Fehlerfamilie ist die der Root-`CLAUDE.md` („welche Änderung macht ihn
   rot? – keine"), und sie hat hier eine **mechanisch fassbare** Form: ein `BadRequest` ohne Blick auf den
   `code`. **Gemessen:** 97 `HttpStatusCode.BadRequest`-Zusicherungen in `Pugling.Api.Tests`, in der
   Umgebung von 123 Zeilen ein `code`-Lesen – die Norm ist also, den Code zu lesen; dieser Fall tat es nicht.
3. **Eine Entscheidung ohne Test, mit einer falschen Begründung, warum er fehlt** (B-235, Entscheidung 1).
   Gefangen von der Nachschau, nicht vom Reviewer des eigenen Sprints – der hatte „kein unbenannter
   Fehlermodus" bestätigt, aber nicht gefragt, ob die Entscheidung *gehalten* wird.

**Prämisse geprüft:** `Program.cs:43` setzt `UnmappedMemberHandling.Disallow` global (nachgelesen); der
`ConventionGuardTests`-Katalog kennt keinen Wächter über Test-Zusicherungen (gegrept: keine Regel mit
`BadRequest`).

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Ein Tor, kein Satz:** ein `ConventionGuardTest` über die Quellen von `Pugling.Api.Tests` – jede
`HttpStatusCode.BadRequest`-Zusicherung liest im selben Testfall den `code` der `ProblemDetails`-Antwort
(`GetProperty("code")` o. ä.), Ausnahmeliste mit Begründung je Eintrag wie bei den anderen Toren. Beleg: Fund 2
wäre rot gewesen, und die Messung (97 zu 123) sagt, dass die Regel schon gelebt wird – das Tor friert sie ein,
statt sie neu zu verlangen. **Kosten:** ein quellentext-lesender Wächter ist ein halber Parser und braucht
seine Rot-Liste (B-40); und „liest den `code`" ist noch nicht „liest den *richtigen* `code`" – das Tor fängt
den Tippfehler-400, nicht die falsche Regel. **Nicht vorgeschlagen:** ein Verbot von `head` in Sweeps (Fund 1)
– das ist eine Handlung, kein Tor; die Lehre steht als Satz in B-166s Verlauf und gehört, wenn überhaupt, als
halbe Zeile zum Vorschlag der Vornacht („mit `Datei:Zeile` belegen statt zählen – **und die Liste nicht
abschneiden**"). **Bewusst ungedeckt:** Fund 3 – ob ein Reviewer fragt „wird die Entscheidung gehalten?", ist
eine Frage an den Brief, nicht an ein Tor; der Nachschau-Brief dieses Laufs hat sie gestellt, der
Sprint-Brief nicht. Das ist eine Zeile im Brief des nächsten Sprints, kein Mechanismus.

**Warum der Lauf weitergeht:** kein Tor gelandet (nur vorgeschlagen), Zähler 3 von 5, Nachschau ohne Defekt.

## Sprint 3 — Ziel & Umfang

**Sprint-Ziel, aus der Entwickler-Sicht (und der des Creators am Katalog):**

> Der **Creator** bekommt auf `PATCH …/subjects/{id}` dieselbe Antwort wie auf `GET` und kann ein Fach nicht
> mehr auf einen leeren Namen umbenennen; der **Entwickler** findet den Admin-Testhelfer an genau einer
> Stelle, mit seinem Merksatz, statt in drei Dateien mit je einer Kopie.

Dieselbe Familie wie die zwei Sprints davor, in der Kleinform: **eine Stelle weicht vom Muster ab, das die
Nachbarn vorgeben** – im Controller (`SubjectsController` gegen `TextbookSeriesController`) und in den Tests
(drei Fassungen eines Helfers).

**Umfang** (beide `art: Aufräumen`, autonom gegrillt und geschätzt, Freigabe 1):

| Story | Richtung |
| --- | --- |
| [B-158](backlog/B-158-subjectscontroller-drei-kleine-reste.md) | `Update` über `Project`, Leer-Guard, Kommentar an den Code angeglichen |
| [B-179](backlog/B-179-drei-fassungen-des-admin-testhelfers.md) | `TestApi.MakeAdmin` + `TestApi.AdminAdultAsync`, drei Fassungen weg |

**Draußen, mit Namen:** B-156 (`isMine` → `isOwn`: `vertragsbruch: ja` – ein Feldname im Vertrag ist
sichtbares Verhalten für jeden Client; die Story selbst sagt „Umbenennen oder stehen lassen?" – das
entscheidet der Nutzer, nicht der Lauf), B-230 (Schema-Entscheidung „eine Quelle für den PIN-Hash", in der
Story ausdrücklich „noch nicht entschieden"), B-138 (70 Stellen mit Einzelurteil über Betonung plus ein neuer
Wächter – nachts zu groß), B-134 (wie oben). Die zwei Aufräumen-Stories hier haben keine offene
Produktentscheidung; B-158s einzige Abweichung von der eigenen Empfehlung (Punkt 3) ist technisch begründet
und steht in Entscheidung 4.

**Refinement, getrennt benannt:** B-179 brauchte User Story und AK (fehlten), B-158 drei Entscheidungen am
Code (`TokenService.cs:43-44` nachgelesen, `TextbookSeriesController.cs:211/249` als Muster) – wieder die
größere Hälfte.

### Entwickler-Brief

- **B-158:** `Update` → `await Project(db.Subjects.AsNoTracking().Where(…), fid).FirstAsync(ct)`; Leer-Guard
  mit demselben Code/Wortlaut wie die Reihe; `Create`-Kommentar nennt, warum kein `fid`-Guard dasteht. Zwei
  Tests in `FachEigentumTests` – die rote Probe **vor** dem Umbau messen, die Absichtserklärung unter einer
  Mutante der alten Hand-Antwort.
- **B-179:** Helfer in `TestApi`, Merksatz einmal; drei Fassungen entfernen, fünf Aufrufe umstellen; keine
  rote Probe (Aufräumen), Suite ist der Beleg.
- **Reihenfolge:** Tests → Proben → Umbau → `dotnet build`/`test` → `dotnet format --verify-no-changes` →
  `pugling-reviewer` → Commit → Nachschau Sprint 2 (läuft parallel als frischer Agent) → Retro (Vorschlag).
- **Brief-Zeile aus der Retro von Sprint 2:** der Reviewer soll auch fragen, ob jede `## Entscheidung` von
  einem Test **gehalten** wird – nicht nur, ob sie richtig ist.

## Iteration 3 — umgesetzt

**Backend (B-158, `SubjectsController.cs`):** `Update` antwortet über `Project` (eine Projektion, keine
`CountAsync`-Runde); Leer-Guard `validation_error` „Name must not be empty." wie bei der Reihe; der
`Create`-Kommentar nennt, warum kein `fid`-Guard dasteht (Rolle impliziert Profil, `AccountService.cs:46-47` – der Zeiger stand hier bis zur Nachschau im zweiten Lauf auf `TokenService.cs:43-44`).
**Tests (B-179):** `TestApi.MakeAdmin` + `TestApi.AdminAdultAsync`, drei private Fassungen entfernt, fünf
Aufrufe umgestellt, zwei `using`s weg. **Neue Fälle:** `FachEigentumTests.PatchMitLeeremNamen_WirdAbgelehnt`,
`…PatchAntwort_IstDieselbeProjektionWieGet`.

**Rote Proben (erwartet/gemessen), auf dem alten Code:**

| Probe | Erwartet | Gemessen |
| --- | --- | --- |
| `PatchMitLeeremNamen_WirdAbgelehnt` | 400 `validation_error` | **200** (1 rot von 2) |
| `PatchAntwort_IstDieselbeProjektionWieGet` | – | grün (Absichtserklärung); unter der Mutante „Hand-Antwort mit `ownerAdultId = null`" **1 rot von 1**, danach `git checkout` |
| B-179 | – | keine (Aufräumen; Suite vorher 877/877, nachher 879/879) |

**Verifikation:** `dotnet build Pugling.sln` 0 Warnungen · `dotnet test Pugling.sln -c Release` **879/879**
(53 s) · `dotnet format Pugling.sln --verify-no-changes` meldet drei WHITESPACE-Zeilen in
`ObjectiveService.cs:57-59` – **außerhalb des Diffs und identisch mit HEAD** (Blob-Hash verglichen,
`26c61c0d`; Stand seit `7884456`), also das bekannte Plattform-Artefakt (Memory „Lokal Windows, CI Linux"),
nicht angefasst.

### Review-Funde (Zähler: 3 von 5)

`pugling-reviewer`, mit der Brief-Zeile aus der Retro von Sprint 2 („wird jede Entscheidung von einem Test
gehalten?"): keine Blocker, 879/879 selbst nachgefahren.

| # | Quelle | Fund | Behebung |
| --- | --- | --- | --- |
| 1 | Reviewer | Verwaister XML-Doc-Block in `FachEigentumTests.cs:325-334` – der Rumpf von `AdminCreatorAsync` war entfernt, seine `/// <summary>` stand noch über dem nächsten Test; kein Tor meldet das (Roslyn hängt beides als Trivia an) | Block gelöscht; der Merksatz lebt in `TestApi.MakeAdmin`/`AdminAdultAsync` |
| 2 | Reviewer | `Update` liefert neu 400, deklariert es nicht – Swagger kennt den `validation_error`-Fall nicht (Geschwister: `TextbookSeriesController.cs:194`) | `[ProducesResponseType(400)]` |
| 3 | Reviewer | `Create` war nach dem Umbau die **verbliebene** zweite Hand-Antwort (eigene Kopie des Eigentums-Prädikats, `0` als Count) – Kommentar „one projection" und Test-Summary „no second projection left" waren zu stark | `Create` liest wie `TextbookSeriesController.cs:184-185` über `Project` zurück; Kommentar und Summary berichtigt (B-158, Entscheidung 6) |

Ohne Zähler (Kommentar/Story): der Zeiger der Entscheidung 4 zeigte auf `TokenService.cs:43`, das `fid` nur
durchreicht – die Invariante „Creator-Profil ⇒ `AdultId`" hält `AccountService.cs:46-47` (Entscheidung 8);
Inventar der `Update`-Actions ohne Leer-Guard (`ChildrenController.cs:105`, `AdultsController.cs:87`) in
Entscheidung 9 festgehalten. Prüffrage (2) „`Project(…).FirstAsync` nach `SaveChangesAsync`, Stale-Risiko?"
– verneint, mit Begründung (impliziter Commit, frischer SELECT, Record-Projektion nie getrackt).

## Runde 3 — Re-Review / Abnahme

- **Creator – signed off (über HTTP):** „`PATCH …/subjects/{id}` mit einem Namen aus Leerzeichen bekommt
  400 `validation_error`, und das Fach heißt danach noch wie vorher; die PATCH-Antwort ist Feld für Feld die
  GET-Antwort." (`FachEigentumTests`, zwei Fälle, gegen die laufende Test-API.) Kein Browser nötig – die
  Fläche ist die API; der Katalog-Editor (`CatalogAdmin.tsx`) sendet nie einen leeren Namen (OK-Knopf nur bei
  echter Änderung), seine Specs bleiben Regressionszeugen.
- **Entwickler – signed off:** ein Admin-Helfer, ein Merksatz; fünf Aufrufe, drei Dateien; Suite grün.
- **Vater, Sohn – Regressionszeugen:** kein Pfad berührt; Backend-Suite nach den Review-Korrekturen
  **879/879** (51 s), `docs/openapi/v1.json` +10 Zeilen (der deklarierte 400), kein O-Ton erfunden.

## Retrospektive Sprint 3

**Nachschau:** B-190, B-131, B-166 (Sprint 2 dieses Laufs, Commit `95b0624`) – frischer allgemeiner Agent
mit eigenem Harness, von mir gegengelesen und am Code nachgeprüft (SetNull-Konfiguration `PuglingDbContext.cs:245/471`,
Reihenfolge `backlog-index.sh:192-193` vor `:212`, B-131-Frontmatter, die sechs Doku-Zeilen). **Zwei Defekte:**

1. **B-131 wurde mit vier fehlenden Pflichtfeldern abgenommen** und stand im committeten Index selbst in der
   ⚠-Tabelle – mit zwei gemeldeten Feldern, weil `backlog-index.sh` `groesse`/`wo` auf `—` setzt, **bevor**
   `belege` prüft (vorbestehend seit `41f21eb`); dazu verschluckt `:265` die ⚠-Tabelle, wenn nur leere
   Dateien da sind → **B-240** (`ausformuliert`, `entgangen_bei: B-131`). Das Frontmatter ist in diesem Sprint
   nachgetragen.
2. **B-166 hat einen Kommentar falsch gemacht und sechs Doku-Stellen übersehen:** `AdultLifecycleTests.cs:14-15`
   behauptete nach dem Wortwechsel einen Katalog-Cascade beim Löschen eines Adults – wahr ist `SetNull`
   (in diesem Sprint korrigiert, ein Satz von mir vom selben Tag); `tutorial-supervisor.md:340-341`,
   `tutorial-student.md:31/320`, `tutorial.md:20`, `vater-informationsarchitektur-plan.md:121-125`,
   `uebergabe-vokabeln.md:59` nennen die Ebene weiter → **B-241** (`ausformuliert`, `entgangen_bei: B-166`).
   ObjectiveTests-Reparatur: kein Defekt (zwei Mutanten am Code nachvollzogen, Kollision ausgeschlossen).

**Was die eigenen Tore durchgelassen haben:** drei Dinge, und zwei davon sind dieselbe Lehre zum dritten Mal:

- **Der zweite abgeschnittene Sweep im selben Sprint.** Nach dem `head -8` des Reviewers lief mein Grep
  über `chapters/|chapterId` – nicht über das deutsche Wort, obwohl die User Story die Tutorials nennt. AK 2
  war zweimal behauptet und zweimal nicht erfüllt. Die Vornacht hatte „mit `Datei:Zeile` belegen statt
  zählen" vorgeschlagen; dieser Lauf zeigt, dass eine **unvollständige** `Datei:Zeile`-Liste dasselbe Loch
  ist.
- **Der eigene Wächter hat sein eigenes Loch verdeckt.** B-131 reparierte die ⚠-Tabelle und stand mit ⚠
  im Index – gemeldet wurden zwei von vier Feldern, und zwei gemeldete Felder sehen aus wie „alle". Die
  Nachschau fand es, weil der Agent eine Story-Kopie **ohne** die vier Felder in den Harness legte und vier
  erwartete. Kein Tor dieses Repos prüft, ob der Index-Wächter vollständig meldet.
- **Ein Wort ersetzt, den Satz nicht gelesen** (Fehlerklasse B-112), in einem Kommentar, den der Build nie
  liest.

**Prämisse geprüft:** `backlog-index.sh:192-193` steht wirklich vor `:212` (nachgelesen); die
SetNull-Konfiguration an `:245/:471` ebenso; `tutorial-supervisor.md:340` steht 30 Zeilen unter der von
B-166 geänderten Zeile `:316`.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Ein Tor für den Wächter selbst:** ein fester Probe-Lauf des Index-Generators gegen einen Wegwerf-Root mit
bekannten Lücken (eine Story ohne `groesse`/`wo`/`migration`/`vertragsbruch`, eine leere Datei, eine ohne
Frontmatter), der die **vollständige** erwartete ⚠-Tabelle vergleicht – als Skript neben
`backlog-index.sh`, von CI gefahren wie der Markdown-Lint. Der Harness dieses Laufs (`index-probe.sh`) ist
der Prototyp; B-240 ist der erste Fall, den er rot gezeigt hätte. **Kosten:** ein weiteres Bash-Artefakt mit
eigener Pflege; und die Versuchung, bei jeder Index-Änderung die Erwartung mitzuändern, statt zu lesen.
**Nicht vorgeschlagen:** ein weiterer Satz über Sweeps – die Vornacht hat einen, dieser Lauf hat ihn zweimal
widerlegt gesehen; was fehlt, ist kein Satz, sondern die Gewohnheit, die **Ausschlussliste** eines Greps
aufzuschreiben, bevor man „0 Treffer" meldet (B-241 tut das in Punkt 2). **Bewusst ungedeckt:** B-112-Fehler
in Kommentaren – kein Tor liest Prosa.

**Warum der Lauf hier endet:** Limit 2 des Backlog-Laufs – die Nachschau fand **Defekte im Increment eines
Sprints dieser Nacht** (B-240, B-241 zu Sprint 2), dieselbe Lage wie B-227 und B-235 in der Vornacht. Der
Zähler (3 von 5) und das Tor (nur vorgeschlagen) hätten nicht gestoppt.

## Zweiter Lauf desselben Tages (ab 13:17)

Der Nutzer hat um 13:17 einen neuen Lauf beauftragt („starte einen nachtlauf"); die acht Freigaben aus
[nachtlauf.md](nachtlauf.md) gelten erneut. Der erste Lauf endete an Limit 2, weil seine Nachschau Defekte im
eigenen Increment fand – ein neuer Auftrag hebt den Halt auf, nicht der Lauf selbst. Diesmal meldet
`tabs_context` eine Tab-Gruppe: der Rollengang kann im Browser laufen, wo eine Rolle etwas zu klicken hat.

### Rollen-Sicht auf diesen Lauf (Runde 4)

Kein neuer `anmerkungen`-Befund (`docs/anmerkungen/` zuletzt 2026-08-14 geändert). Die Rollen sprechen aus
dem Stand, den der erste Lauf hinterlassen hat:

- **Creator (O-Ton):** „Ich öffne `docs/REST/Creator.http`, schicke die Requests von oben nach unten – und ab
  Schritt 4 (`…/chapters`) kommt 404, alles danach auch. Das ausführbare Tutorial ist das einzige Dokument,
  das beim *Ausführen* sofort auffällt." (B-238) · „Die Einstiegsseite sagt, ich baue ‚Fächer, Kapitel,
  typisierte Übungen'. Kapitel gibt es seit B-106 nicht." (B-241) · **Top-3:** 1. B-238, 2. B-241, 3. B-237
  (`idee`, bleibt liegen – Freigabe 8).
- **Vater (O-Ton):** „Das Supervisor-Tutorial erklärt mir, dass eine Etappe ‚auf einem Kapitel' das Löschen
  hindert. Ich finde im Vater-Web kein Kapitel – nur Reihen und Units." (B-241) · **Top-3:** 1. B-241,
  2. B-201 (XS, hängt an der offenen Frage B-186 – nicht in diesem Sprint), 3. B-164 (Entscheidung beim Nutzer).
- **Sohn (O-Ton):** kein Kontakt mit diesem Sprint – kein Student-Pfad wird angefasst. Er bleibt
  Regressionszeuge.
- **PM/Entwickler:** „Der Index-Wächter hat B-131 mit ⚠ durchgelassen und dabei zwei von vier Feldern
  gemeldet. Ein Wächter, der unvollständig meldet, wirkt vollständig." (B-240)

**Synthese:** alle drei Kandidaten sind `Defekt`, `ausformuliert`, `wo: doku`, Fix benannt – autonom grillbar
(Freigabe 1). Gemeinsamer Faden: *Werkzeug und Doku behaupten eine Ebene oder einen Zustand, den es nicht
gibt.* Nicht aufgenommen: B-181 (`Defekt`, aber Punkt 2 ist eine Verhaltensfrage im Produktionspfad, die die
Story selbst dem Dialog vorbehält), B-201 (wartet auf B-186), B-134/B-164 (Architektur-Entscheidungen beim
Nutzer). Refinement dieses Sprints: drei Stories von `ausformuliert` nach `geschaetzt` – benannt, nicht in die
Sprintgröße gefaltet.

## Sprint 4 — Ziel & Umfang

**Sprint-Ziel (aus dem Creator-Sitz):** *Was ich im Repo über den Katalog lese oder ausführe – Tutorial,
REST-Flow, Index-Wächter – sagt dasselbe wie die laufende App: Fach → Reihe → Unit → Übung, und ein Wächter,
der etwas vermisst, nennt alles, was fehlt.*

**Umfang:** B-240, B-241, B-238 – alle `art: Defekt`, alle in diesem Lauf von `ausformuliert` nach
`geschaetzt` gebracht (Refinement: drei Stories, je vier Entscheidungen; beim Grillen von B-238 gegen die DTOs
wuchs die Story von drei auf fünf tote Requests und von XS auf S, beim Grillen von B-240 kam ein drittes Feld
derselben Familie dazu – `art`). **Draußen geblieben, mit Namen:** B-181 (Punkt 2 ist eine Verhaltensfrage im
Produktionspfad – die Story selbst behält sie dem Dialog vor), B-201 (hängt an der offenen Frage B-186),
B-134/B-164 (Architektur-Entscheidungen beim Nutzer), B-07 (wartet auf Azure).

### Entwickler-Brief

- **Ziel:** drei Stellen, die eine nicht existierende Ebene oder einen nicht existierenden Zustand behaupten,
  sagen die Wahrheit – mit roter Zahl vorher.
- **Quelle der Wahrheit:** für B-238 die Controller-Routen (`ExerciseRoutes.Base`) und die DTOs in
  `Pugling.Contracts/Creator`; für B-241 der Frontend-Code (`VaterExercises.tsx`, `VaterExerciseCreate.tsx`)
  und die Controller hinter den Tutorial-Aussagen; für B-240 die Funktion `belege` selbst.
- **Guards:** keine neuen – kein Produktcode wird angefasst. Der eine `.cs`-Eingriff (TestApi-Kommentar) kommt
  aus der Nachschau, nicht aus dem Sprint.
- **Migration:** keine. **Testweg:** Harness-Proben (Index), curl-Sequenz (REST), zwei Greps mit
  Ausschlussliste (Doku), markdownlint, Suite.
- **Reihenfolge:** erst alle drei roten Proben, dann die drei Fixes, dann Reviewer und Nachschau parallel,
  dann der Rollengang gegen den **frisch gestarteten** Server.

## Iteration 4 — umgesetzt

**Werkzeug (B-240, `backlog-index.sh`):** `belege` sieht die Rohwerte (vor den Platzhaltern); die
„Noch keine Story angelegt"-Verzweigung prüft `unbelegt` mit. Zwei englische Kommentare nennen den Grund.

**REST (B-238, `docs/REST/Creator.http`):** Verlag → Reihe → Unit stehen vor der Übung; alle Übungs-Routen auf
`…/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary`; `createSeries` mit `publisherId` statt
Freitext, `createUnit` mit `topics` als Liste; `{{$timestamp}}` in Fach, Reihe, Profil; „verifiziert am" im Kopf.
`docs/REST/README.md:15` nachgezogen.

**Doku (B-241):** sieben Stellen (Student-, Supervisor-, Einstiegs-Tutorial, Übergabe-Notiz, IA-Plan) und zwei
Nits (`frontend/CLAUDE.md:82`, `endpunkt-beziehungen.md:418`); der IA-Plan trägt eine Kopfnotiz und – nach dem
Review – an fünf Stellen „(heute: …)"-Zusätze statt einer bloßen Wortersetzung.

**Verifikation:**

| Probe | erwartet | gemessen vorher | gemessen nachher |
| --- | --- | --- | --- |
| P4 Index: `abgenommen` ohne vier Felder | vier Felder gemeldet | **zwei** (`migration`, `vertragsbruch`) | vier |
| P6 Index: `abgenommen` ohne `art` | „Feld `art`" | „`art` ist kein bekannter Wert" | „Feld `art`" |
| P5 Index: Root mit nur leerer Datei | ⚠-Tabelle „Datei ist leer" | „*Noch keine Story angelegt.*" | ⚠-Tabelle |
| `Creator.http` per curl (`:5280`) | 2xx durchgehend | **13 von 24 rot** (405/404/400 ab Schritt 6) | **23/23 2xx** |
| Kapitel-Sweep (Ausschlussliste in B-241) | 0 | 16 | 1 gemessen – **4 im Commit** (Kopfnotiz, zwei Heute-Klammern, `Creator.http:11`; Nachschau Sprint 5, B-246) |
| `frontend/CLAUDE.md` | nicht wachsen | 10.223 B | 10.208 B (Altschuld +1.208 bleibt) |
| Backend-Suite (nach dem TestApi-Kommentar) | grün | – | **879/879** |
| markdownlint-cli2 | 0 | – | 0 (352 Dateien) |

Echter Index zweimal: byte-gleich; „66 offen, 165 abgenommen, 13 verworfen". Die ⚠-Tabelle ist nach dem Fix
**nicht** gewachsen (21 → 21 Zeilen): keine abgenommene Story fehlt heute `groesse`/`wo` – der Wächter
meldete bisher zu wenig, aber es gab nichts Unentdecktes mehr zu melden.

### Review-Funde (Zähler: 0 von 5)

Reviewer: allgemeiner Agent (Skript, Doku, REST-Datei, Stories), eigener Wegwerf-Root und eigener Live-Lauf.
**Keine Blocker, kein Fund mit Code-/Testwirkung.** Vier Funde mit Doku-Wirkung, alle behoben:

1. Index nicht regeneriert (B-242-Zeile fehlte) → Lauf vor dem Commit.
2. **Fehlerklasse B-112 in eigener Sache:** meine Wortersetzung Kapitel→Unit machte im IA-Plan vier Sätze
   falsch (Anlegen „im Katalog" ist heute Fach+Art, Reihe/Unit liegen unter `/vater/lehrwerke`; der Link
   bei „Unit fehlt" geht dorthin; die E2E legt die Unit dort an) – an einer Story, die diese Fehlerklasse
   selbst benennt. Umformuliert mit „(heute: …)".
3. `/vater/exercises/neu`: nicht „Fach & Unit", sondern Fach (Filter), Reihe & Unit (Auswahl).
4. B-238s Verlauf nannte den curl-Lauf noch nicht (AK 2, zweiter Fall) → nachgetragen.

Nits übernommen: `DraftRules.cs:51`, Byte-Zahl (−15, nicht +11), `@seriesUnitId`, B-242-Zeile `:93`,
`skip=0` in `Creator.http`, `docs/anmerkungen/*` in die Ausschlussliste des Routen-Sweeps.

**Ein Werkzeug-Fallstrick, gemessen:** der Reviewer hat am Ende seines Laufs `pugling_smoke.db*` gelöscht –
während mein Server auf `:5200` genau diese Datei für den Browser-Rollengang benutzte. Folge: `POST auth/adult`
**500**, im Browser „Id oder PIN falsch". Zwei Agenten, eine Wegwerf-Datei, keine Absprache. Neu gestartet,
dann grün. Der Brief an einen Reviewer muss sagen, welche Ressourcen gerade in Gebrauch sind.

## Runde 4 — Re-Review / Abnahme

**Browser verbunden (Freigabe 6, erster Fall).** Server und Frontend wurden **nach** dem letzten Edit gestartet
(Wegwerf-DB, `:5200`/`:5173`).

- **Creator – signed off (per Flow):** „Ich schicke die Requests von oben nach unten: Fach, Verlag, Reihe, Unit,
  Vokabeln, Übung, Items, Preview, Tag, Rechte, Profil, Lehrbuch, Match – 23 Antworten, alle 2xx. Vorher war ab
  Schritt 6 Schluss. Die Einstiegsseite sagt jetzt ‚Lehrwerk-Reihen mit Units'." Der Rollengang ist der
  curl-Durchlauf derselben Requests in derselben Reihenfolge; den REST-Client im Editor fährt der Nutzer
  (Datum im Kopf der Datei dafür offen). **Ausdrücklich zurückgestellt:** `docs/REST/Creator/
  VocabularyExecises.http` (B-242) – „die Nebendatei fährt noch die toten Routen, mit eingebranntem Token".
- **Vater – signed off (im Browser):** Login Vater 1 → `/vater/exercises` zeigt **Fach · Reihe (– alle –) ·
  Unit (– alle –)**, `/vater/exercises/neu` zeigt **Fach (filtert nur die Reihen) · Reihe · Unit** und
  „Fehlt eine Reihe oder Unit? … unter 📕 Lehrwerke". „Das Supervisor-Tutorial sagt jetzt ‚Etappe auf einer
  Unit' – das finde ich auch im Web." Screenshot in der Sitzung gesehen; kein Produktpfad des Vaters wurde
  geändert, der Walk ist Regressionszeuge **und** Prüfung der neuen Doku-Behauptungen.
- **Sohn – Regressionszeuge:** kein Student-Pfad im Diff; Suite 879/879. Kein O-Ton erfunden.
- **PM – Index:** Harness P4–P6 grün, echter Index byte-gleich.

## Retrospektive Sprint 4

**Nachschau:** B-158, B-179 (Sprint 3 des ersten Laufs, Commit `98c8431`) – frischer allgemeiner Agent mit
elf benannten Prüfpunkten, von mir gegengelesen (`SubjectsController.cs:69-70/:99/:86-93/:75`,
`TestApi.cs:53/:67`, `RemarkTests.cs:659-668`, `PuglingDbContext.cs:245/471/515`). **Ein Defekt:** die
Summary von `TestApi.AdminAdultAsync` (`:63-65`) behauptete, ein Admin-Flag am geseedeten Adult mache die
„foreign creator"-Fälle **grün** – gemessen (Diff von `98c8431`, `FachEigentumTests.cs:305-320`) werden sie
**rot** (403 erwartet, Break-Glass liefert 200). Fehlerklasse B-112, ein Satz von heute Mittag. Korrigiert
in diesem Sprint, Story **B-243** (`entgangen_bei: B-179`) trägt ihn und die **vierte** Admin-Helfer-Fassung
(`RemarkTests.RegisterAdminFatherAsync`), die B-179s Grep über drei Namen nicht traf. **Kein Defekt** an
B-158: alle vier AK am Code erfüllt, rote Zahlen im Verlauf; zwei Funde derselben Familie außerhalb des Diffs:
`PublishersController.Create` baut die Antwort von Hand (**B-244**, Aufräumen) und ein Token mit `fid` auf
eine gelöschte Adult-Zeile liefe in die FK (**B-245**, `idee`, unverifiziert). `nachgeschaut` auf beiden
Stories gesetzt, je mit Prüfpunkten. **Index: Nachgeschaut 149 von 165.**

**Prämisse geprüft:** `TestApi.cs:63-65` am Diff nachgelesen (die alte Summary in `FachEigentumTests` sagte
„makes … fail"); `PuglingDbContext.cs:245-246/471-472/515-516` sind `SetNull`; `VaterExerciseCreate.tsx:196/:120/:121`
(Fach filtert, Reihe und Unit Pflicht) gegen die IA-Plan-Zeile `:121` gelesen.

**Was die eigenen Tore durchgelassen haben – drei Dinge, zwei davon dieselbe Lehre zum vierten Mal:**

- **Wortersetzung ohne Satzlesen, diesmal von mir, in der Story, die die Klasse benennt.** Kapitel→Unit an
  neun Zeilen des IA-Plans; der Reviewer las fünf davon im Kontext und fand vier falsch. Ein `replace` über
  neun Zeilen erzeugt neun Sätze, die gelesen werden müssen – ich habe neun Wörter ersetzt.
- **Der Sweep der Story war selbst abgeschnitten.** B-241 listete sechs Stellen; der erste Grep dieses Sprints
  fand die siebte (`tutorial-supervisor.md:477`). Und die Ausschlussliste des Routen-Sweeps kannte
  `docs/anmerkungen/` nicht. Die Vornacht hatte „Ausschlussliste aufschreiben" vorgeschlagen – richtig, aber
  die Liste muss vor dem ersten Grep stehen, nicht nach dem Fund.
- **Zwei Agenten, eine Wegwerf-Ressource.** Der Reviewer räumte `pugling_smoke.db` auf, mein Server lief
  darauf; der Browser-Walk zeigte 500 und sah aus wie ein Produktfehler. Kein Tor dieses Repos kennt
  „welche Datei gehört gerade wem".

**Warum der Lauf hier nicht endet (Limit 2, argumentiert, nicht entschieden):** Limit 2 greift bei einem
Defekt „im Increment eines Sprints derselben Nacht". Der eine Nachschau-Defekt (B-243, Punkt 1) liegt im
Increment von Sprint 3 des **ersten** Laufs – ein eigener Auftrag, bereits an Limit 2 geendet. Er ist ein
Kommentar ohne Verhalten, korrigiert, mit Story. Der Reviewer dieses Sprints fand **keinen** Fund mit Code-
oder Testwirkung im Increment (Zähler 0 von 5). Ich lese das als: die Qualitätsschwelle dieses Laufs ist
nicht gerutscht – und schreibe es hierher, damit der Nutzer am Morgen das Gegenteil entscheiden kann.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Ein Tor für ausführbare Doku:** ein Guard-Test in `Pugling.Api.Tests`, der jede Request-Zeile in
`docs/REST/**/*.http` (`METHOD {{baseUrl}}/pfad`) gegen die Pfade des erzeugten OpenAPI-Dokuments prüft –
`{{variable}}`-Segmente werden zu `{x}` normalisiert, Query-Strings abgeschnitten. B-238 **und** B-242 wären
rot gewesen, und zwar seit B-106, nicht erst beim Ausführen. **Kosten:** ein Test, der eine Doku-Datei liest
(Vorbild: `DocsCaptureTests` liest `docs/`), eine kleine Pfad-Normalisierung mit Sonderfällen (`~/`-Routen,
Query-Parameter), und die Versuchung, eine tote Datei einfach aus dem Glob zu nehmen. **Nicht vorgeschlagen:**
ein vierter Satz über Sweeps – drei Läufe haben gezeigt, dass der Satz nicht fehlt, sondern die Reihenfolge
(Liste vor Grep). **Bewusst ungedeckt:** B-112-Fehler in Prosa – kein Tor liest Sätze; der einzige Schutz
ist, den Reviewer-Brief auf „±5 Zeilen Kontext je geänderter Zeile" festzulegen, und der stand in diesem
Brief drin – deshalb wurde es gefunden.

## Sprint 5 — Ziel & Umfang

**Sprint-Ziel (aus dem Creator-Sitz):** *Wenn ich den KI-Creator um Hilfe bitte, einen Verlag anlege oder
den vierten Admin-Test schreibe, finde ich keine Kopie und keine Zeile mehr, die etwas anderes sagt als der
Code daneben.*

**Umfang:** B-243 (`Defekt`, Rest aus der Nachschau zu B-179), B-244 (`Aufräumen`, Nachbar von B-158), B-237
(`Defekt`, `idee` → in diesem Sprint ausformuliert, gegrillt, geschätzt – `--chapter` wird nicht geparst,
`--series`/`--series-unit` fehlten in der Hilfe). Alle drei unter Freigabe 1 autonom gegrillt. **Draußen
geblieben:** B-242 (Streich-Entscheidung – löschen oder nachziehen – gehört dem Nutzer), B-239 (Wiki-Beispiele:
anderer Grep, anderes Thema), B-245 (unverifiziert, erst messen).

### Entwickler-Brief

- **Ziel:** drei Stellen folgen dem Muster ihres Nachbarn – `TestApi.MakeAdmin` statt vierter Kopie, `Project`
  statt Hand-Antwort, Hilfe statt Erinnerung an eine Ebene, die es nicht gibt.
- **Quelle der Wahrheit:** `TestApi.cs:53-59` (Flag vor Login), `SubjectsController.cs:69-70` (Vorbild für
  Publishers), `ResolveLocationAsync` (`AgentCommands.cs:363-395`) für jeden Satz der Hilfe.
- **Guards:** keine neuen; `Create` der Verlage behält Slug-/Namensprüfung, nur die Antwort kommt aus der
  Projektion. **Migration:** keine. **Vertrag:** unverändert (gleiche Antwortform).
- **Testweg:** Mutante für B-244 fahren (nicht nur benennen), Hilfe-Zählung für B-237 vor/nach, Grep für B-243,
  Suite; `pugling-reviewer`; Nachschau zu Sprint 4 parallel.

## Iteration 5 — umgesetzt

**Tests (B-243):** `RemarkTests.RegisterAdminFatherAsync` ruft `RegisterFatherAsync` + `TestApi.MakeAdmin`;
drei tote `using`s weg. **Backend (B-244):** `PublishersController.Create` antwortet über `Project(…)`;
`PublishersTests` vergleicht `201`-Body mit `GET`-Body. **KI-Creator (B-237):** Hilfe nennt `--series`/
`--series-unit` mit dem Standard aus `ResolveLocationAsync`, `--subject` als `profiles`-Filter, `--chapter`
ist weg; README `:50`/`:146`; Strings „in dieser Unit"/„in der Unit"; `FreshChapterAsync` → `FreshUnitAsync`
(33 Stellen), Test `Eine_unbekannte_Unit_…`, ein Kommentar.

**Nachschau-Reparatur (B-246, Sprint 4):** `uebergabe-vokabeln.md:57-60` (`VocabRefPicker` liegt in
`VaterExerciseCreate.tsx`; `VaterExercises.tsx` hat eine Übungs-, keine Unit-Liste), IA-Plan `:99` (die zwei
Links gehen auf Katalog und Lückentexte, der Lehrwerke-Link steht auf der Anlege-Seite); Sweep wiederholt.

**Verifikation:**

| Probe | erwartet | gemessen vorher | gemessen nachher |
| --- | --- | --- | --- |
| B-244 Mutante (`Create` von Hand, `ForeignSeriesCount = -1`) | neuer Fall rot | **1 rot / 4 grün** in `PublishersTests` | Mutante zurück, `Project`: grün |
| B-237 `pugling-creator help` | `--chapter` 0 · `--series-unit` 1 | **1 · 0** | **0 · 1** (auch nach den Reviewer-Korrekturen) |
| B-243 `rg -c "IsAdmin = true" Api.Tests/*.cs` | 1 Datei | **2** (`TestApi.cs`, `RemarkTests.cs`) | **1** |
| `rg "new PublisherResponse\("` | nur Projektion | 2 Stellen | **1** (`:35`) |
| Kapitel-Sweep (Liste B-241 + B-246) nach der letzten Änderung | nur benannte Ausnahmen | – | **4 + 1**: IA-Plan `:9` (Kopfnotiz), `:93`/`:130` (Heute-Klammern), `Creator.http:11`, `uebergabe-vokabeln.md:92` (Tag-Beispiel, Liste war `:87-91` vor dem Zeilenzuwachs) |
| Backend-Suite (nach dem letzten `.cs`-Edit) | grün | – | **879/879** |

### Review-Funde (Zähler: 2 von 5)

`pugling-reviewer` (Diff + Stories): **keine Blocker.** **Zwei Funde mit Codewirkung**, beide im neuen
Hilfetext von B-237 und beide Fehlerklasse B-112 – in der Story, die die Klasse benennt:

1. „das Fach kommt aus der Reihe" – der Code nimmt `profile?.SubjectId ?? ResolveSubjectIdAsync(seriesId)`
   (`AgentCommands.cs:320`); die alte Zeile hatte diese Hälfte sogar richtig. Jetzt: „aus dem Profil, sonst aus
   der Reihe".
2. „`--series-unit` allein bestimmt auch die Reihe" – nur ohne Profil-Reihe; mit `--profile` muss die Unit in
   dessen Reihe liegen (`:366/:372/:389-392`, Test `Eine_Unit_aus_einer_fremden_Reihe_wird_gemeldet`). Jetzt mit
   Halbsatz, README `:146` mit.

Ohne Zähler: B-237 AK 4 nannte `CurrentChapter` (lebendes Vertragsfeld) nicht als Ausnahme – Story-Text; zwei
Nits (Mutanten-Kommentar „1 failed / 4 passed in this class", Zeilenzeiger `:172`).

## Runde 5 — Re-Review / Abnahme

Kein Produkt-UI berührt; der Browser blieb geschlossen – ehrlich benannt, nicht ersetzt.

- **Creator – signed off (Konsole + API):** „`pugling-creator help` sagt mir jetzt `--series` und
  `--series-unit`, und was passiert, wenn ich nur die Unit angebe – und es sagt nicht mehr `--chapter`, das
  der Agent still ignoriert hat." Verlag anlegen und sofort lesen: identischer Body (Test gegen den
  In-Process-Server). **Zurückgestellt:** `CommandLine` meldet unbekannte Optionen nicht (`--serie` läuft
  still in den Standard) – Nebenfund in B-237s Verlauf, keine Story ohne den Nutzer.
- **Vater / Sohn – Regressionszeugen:** kein Supervisor-/Student-Pfad im Diff; Suite 879/879.
- **Entwickler:** Grep `IsAdmin = true` eine Datei; Merksatz einmal.

## Retrospektive Sprint 5

**Nachschau:** B-240, B-241, B-238 (Sprint 4 dieses Laufs, Commit `7c33765`) – frischer Agent mit neun
Prüfpunkten, Harness im Wegwerf-Root (AK 1–3 von B-240 plus `verworfen` ohne `grund`, `art: Tippfehler`,
`groesse: ""`), alle 23 Requests der `Creator.http` gegen Route/DTO, jede Doku-Zeile gegen den Code. **Kein
Defekt** an B-240 und B-238. **Drei Defekte an B-241** → **B-246** (`entgangen_bei: B-241`, im Sprint behoben):
`uebergabe-vokabeln.md:57-59` behauptete eine Unit-Liste und den `VocabRefPicker` in `VaterExercises.tsx`
(beides falsch: Filter-Select `:133-137`, Picker in `VaterExerciseCreate.tsx`); IA-Plan `:99` behauptete
zwei Links von der Übungen-Seite zu den Lehrwerken (null; `:106-109` gehen auf Katalog/Lückentexte); und der
Kapitel-Sweep lief **vor** den Reviewer-Umformulierungen – im Commit standen vier Treffer, der Verlauf sagte
einer. Nebenbefund des Prüfers: `backlog-index.sh:230/:239` vergleichen gegen `'""'`, was awk seit dem
Quote-Strip nie liefert (toter Zweig, harmlos). `nachgeschaut` auf B-240/B-241/B-238 gesetzt.
**Index: Nachgeschaut 152 von 169.**

**Prämisse geprüft:** `grep -c lehrwerke VaterExercises.tsx` = 0 selbst gemessen; `VocabRefPicker` nur in
`VaterExerciseCreate.tsx` (ein Treffer); Sweep nach der letzten Änderung selbst wiederholt (Tabelle oben).
Der Prüfer hatte den Git-Index mit `--work-tree`-Checkout berührt und wiederhergestellt – `git status`
(elf Sprint-5-Dateien) und `git diff --cached` (leer) von mir nachgeprüft.

**Was die eigenen Tore durchgelassen haben – und zwar dieselbe Klasse zum dritten Mal in zwei Sprints:**

- **Wortersetzung ohne Satzlesen, an zwei Sätzen, die der Reviewer nicht las.** Der Reviewer-Brief verlangte
  ±5 Zeilen Kontext je geänderter Zeile und fand so fünf Sätze; die zwei übrigen lagen in Dateien, die er
  nur stichprobenweise las. Ein Wortwechsel über sieben Dateien braucht sieben Leser, nicht fünf.
- **Der Sweep lief vor der letzten Änderung.** Reihenfolge im Sprint: Fix → Sweep → Review → Reviewer ändert
  Sätze → Commit. Der Sweep hätte **nach** dem Reviewer laufen müssen – dieselbe Lehre wie B-166 („ein Grep zur
  falschen Zeit gilt als Sweep"), diesmal nicht wegen Reichweite, sondern wegen Zeitpunkt.
- **Die eigene Hilfe-Umformulierung trug zwei neue Halbwahrheiten** (Fach aus der Reihe; Unit bestimmt die
  Reihe) – gefunden vom Reviewer, der `ResolveLocationAsync` Zeile für Zeile gegen den Text las. Der Brief
  hatte genau das verlangt; das Tor hat gehalten.

**Warum der Lauf hier endet (Limit 2):** die Nachschau fand **Defekte im Increment von Sprint 4 dieses
Laufs** (B-246) – dieselbe Lage wie B-240/B-241 im ersten Lauf. Dass sie Doku sind und behoben, ändert die
Regel nicht: die Qualitätsschwelle des Laufs ist gerutscht, und das will der Nutzer sehen, bevor weitere
Sprints auf ihr stehen. Der Zähler (2 von 5) hätte nicht gestoppt.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Kein Tor, eine Reihenfolge:** jeder Sweep, der ein Akzeptanzkriterium belegt, läuft als **letzter Schritt vor
dem Commit** – nach dem Reviewer, nach jedem Nachtrag – und seine Ausgabe (Kommando, Ausschlussliste, Treffer
mit Zeile) steht im Verlauf **mit Zeitstempel relativ zum letzten Edit** („nach dem letzten Edit um …"). Als
Satz in `pm-loop` Step 5 („Verify for real"), nicht im Root-`CLAUDE.md`. **Kosten:** ein Satz im Verfahrenstext,
der heute schon über Budget liegt (`backlog/README.md` +114 B, `pm-loop/SKILL.md` ungemessen) – wer ihn landet,
streicht einen anderen. **Warum kein Tor:** ein Guard-Test, der „Kapitel" in `docs/` verbietet, bräuchte dieselbe
Ausschlussliste als Code und würde jede historische Erzählung zum Pflegefall machen; der Fehler ist ein
Zeitpunkt, kein fehlender Prüfer. **Bewusst ungedeckt:** B-112 in Sätzen, die kein Reviewer liest – drei
Treffer in zwei Sprints sagen, dass „±5 Zeilen" nicht reicht, wenn die Ersetzung sieben Dateien trifft; die
ehrliche Alternative ist, bei einem Wortwechsel über mehr als drei Dateien **jeden** Treffer im Brief zu
listen, nicht nur die Dateien.

## Offene Roadmap · Änderungsstand

Die Reihenfolge lebt in [docs/backlog/](backlog/README.md); hier nur die Begründung des Stands nach diesem Tag:
**zwei Läufe, fünf Sprints, vierzehn Stories abgenommen** (erster Lauf: B-235, B-159, B-176 · B-190, B-131, B-166 ·
B-158, B-179; zweiter Lauf: B-240, B-241, B-238 · B-243, B-244, B-237 – dazu B-246 als im Sprint behobener
Nachschau-Defekt), alle derselben Familie: eine Stelle behauptet etwas, das nicht stimmt – ein Ladefenster, ein
fremder Ersteller, eine Ebene, die es nicht gibt, ein Wächter, der zwei von vier Feldern nennt, eine Hilfe mit
einer Option, die niemand liest.

**Neue Stories des zweiten Laufs:** B-242 (zweite `.http`-Datei mit toten Routen und eingebranntem Token – Löschen
ist eine Streich-Entscheidung), B-243 (abgenommen), B-244 (abgenommen), B-245 (`idee`, unverifiziert: Token mit
`fid` auf gelöschten Adult), B-246 (abgenommen, `entgangen_bei: B-241`).

**Commits (nichts gepusht):** Sprint 4 `7c33765` + Nachtrag `228e095` · Sprint 5 `4954bd2` + Nachtrag.

## Für den Morgen

1. **Fünf Retro-Vorschläge dieses Tages** landen oder verwerfen – erster Lauf: Auflage 5 auf Absichtserklärungen
   ausdehnen (Mutante benennen **und** fahren – in Sprint 5 bereits so praktiziert, B-244); **Tor** „jede
   400-Zusicherung liest den `code`"; **Tor** für den Index-Wächter (Probe-Lauf gegen Wegwerf-Root). Zweiter
   Lauf: **Tor** „Routen in `docs/REST/**/*.http` gegen OpenAPI" (hätte B-238 und B-242 seit B-106 rot gezeigt);
   **Reihenfolge** „Sweep als letzter Schritt vor dem Commit, mit Zeitstempel". Dazu die zehn älteren.
2. **Zwei Entscheidungen, die der Lauf nicht treffen durfte:** B-242 (Datei löschen?), und der Nebenfund aus
   B-237 (`CommandLine` meldet unbekannte Optionen nicht – Story anlegen?). Weiter offen: B-156, B-230,
   B-141/B-145, B-164, B-181 Punkt 2.
3. **Ein Blick des Menschen:** `docs/REST/Creator.http` einmal im REST-Client fahren und das Datum im Kopf
   eintragen (curl sagt 23/23, die Variablensyntax des Clients hat niemand ausgeführt); `/vater/exercises/neu`
   (Überschriften/Versalien aus dem ersten Lauf, B-176).
4. **Ein Werkzeug-Fallstrick für den nächsten Lauf:** ein Reviewer-Agent hat die Wegwerf-DB gelöscht, auf der
   mein Server lief – der Brief muss sagen, was gerade in Gebrauch ist; und ein Nachschau-Agent hat den Git-Index
   per `--work-tree`-Checkout berührt (wiederhergestellt, von mir geprüft) – `git archive … | tar -x` ist der
   richtige Griff.
5. `git log --oneline` gegen `9f45907`: vier Commits dieses Laufs; **nichts ist gepusht.**

## Dritter Lauf desselben Tages (ab 20:03)

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9 plus der Paket-Satz *„Fahre
die Pakete NP-1, NP-2, NP-3 aus [der Backlog-Sichtung](pm-sitzung-2026-10-23-backlog-sichtung.md), in dieser
Reihenfolge."* Ausgangslage: HEAD `896d1b2`, `git status` sauber. Die Chrome-Extension ist **verbunden**
(`list_connected_browsers`: ein Browser, lokal) – Freigabe 6, erster Fall. Die Pakete sind je ein Sprint;
die Sprints laufen weiter durchnummeriert (6, 7, 8).

**Auslegung von Limit 2 für diesen Lauf, ausdrücklich:** die Nachschau von Sprint 6 trifft Sprint 5, also
das Increment des **zweiten** Laufs, das der Nutzer danach gesehen hat (er hat ab 17 Uhr im Dialog gegrillt).
Ein Fund dort ist eine Entgleitung nach der Abnahme (eigene `Defekt`-Story mit `entgangen_bei`), aber kein
Defekt im Increment **dieses** Laufs – er beendet ihn nicht. Erst ab Sprint 7 trifft die Nachschau Arbeit
dieser Nacht, und dann gilt Limit 2 wie in den beiden Vorläufen.

## Sprint 6 — Ziel & Umfang (NP-1)

**Sprint-Ziel (aus dem Sitz des Sohns):** *Erreiche ich ein Teilziel meines großen Ziels, feiert die App es
einmal mit Konfetti und Ton, und wenn ich eine Karte aufdecke, dreht sie sich um, statt dass die Lösung
einfach dasteht.*

**Umfang:** B-36, B-35 (beide `Wunsch`, `geschaetzt` → baubar nach Freigabe 8; Nachprüfungs-Zeilen der Sichtung
gelesen und befolgt: Status-Werte `"Achieved"`, `MyObjectives` hängt in `SohnProgress.tsx`, Helfer exportieren
statt duplizieren; ShowBoth hat keine Rückseite mehr, der Klausur-Reveal sitzt in `SelfAssessAnswer`,
`RevealAlternatives` gehört mit auf die Rückseite, B-109-Klickkette). Nichts sonst.

**Entwickler-Brief:** reines Frontend, kein Vertrag, keine Migration. **Quelle der Wahrheit:** `ObjectiveResponse`
/`KeyResultResponse` (Status live gerechnet, `ObjectiveEvaluationService`), Auszahlung einmalig beim
Kinder-Login (`ObjectiveRewardService.SettleAsync`, `AuthController.cs:83`). **Guards:** die Feier läuft nur über
`celebrate(...)` (Reduce-Motion zentral), die Wende nur über CSS (globales Reduce-Motion-Gate). **Testweg:**
Komponententest mit Mutanten je Fall, eigene E2E-Spec für die Wende, Browser-Rollengang.

## Iteration 6 — umgesetzt

- **B-36:** `frontend/src/sohn/seenIds.ts` (neu: `isFirstLoad`/`readSeen`/`writeSeen` aus `GamificationPanels.tsx`
  plus `takeFreshAchieved`), `MyObjectives.tsx` feiert Etappe `medium` und Ziel `big` (zuletzt), kein „+0".
  Test `MyObjectives.test.tsx` (7 Fälle).
- **B-35:** `index.css` (`.fcard.flipped, .card.flipped` + zwei Keyframes), Klasse in `SohnPractice.tsx` an
  `phase === "back"`, in `SohnTest.tsx` an `!typed && revealed`. Neue Spec `e2e/karten-umdrehen.spec.ts`.
- **B-256 (Nachschau-Defekt, Sprint 5):** Hilfe und README des KI-Creators, Kommentar in `PublishersTests.cs`.

**Verifikation:**

| Probe | erwartet | gemessen |
| --- | --- | --- |
| `MyObjectives.test.tsx` gegen HEAD-`MyObjectives.tsx` | Feier-Fälle rot | **4 rot / 2 grün** (6 Fälle, vor Fall 7) |
| acht Mutanten (Skript) | jeder Fall fällt unter ≥ 1 | ja – 1 bis 4 von 7 je Mutante |
| `karten-umdrehen.spec.ts` × 4 Proben | rot an der zugehörigen Zeile | `:77`, `:63`, `:64`, `:64` (erste Runde zu früh an `:25` → Locator-Rennen behoben, B-255) |
| `karten-umdrehen.spec.ts` dreimal hintereinander | grün | 3 × 1 passed |
| Vitest / `tsc -b` | grün | **402/402** / sauber |
| Playwright voll (nach dem letzten Frontend-Edit) | grün | **81 passed** |
| Backend-Suite (nach B-256) | grün | **879/879** |
| B-256: alte Hand-Zeile in `PublishersController.Create` | Behauptung „grün (0 = 0)" prüfen | **1 rot / 4 grün** an `createdAt` – Behauptung falsch |

### Review-Funde (Zähler: 5 von 5)

`frontend-reviewer`, dann Delta-Review (keine Funde). Gezählt wird, was Code oder Tests ändert:

1. **Blocker:** eine Etappe/ein Ziel, das um die Schwelle schwankt, feierte erneut „+50 🪙", obwohl der Server
   nur einmal zahlt → `takeFreshAchieved` vergisst nie.
2. „+0 🪙" bei einem Ziel ohne Belohnung → Bedingung + Fall 7.
3. Die Wende-Zusicherungen standen im Durchstich `full-flow.spec.ts` → eigene Spec (Hausregel „eine Fläche je
   E2E-Spec").
4. `.flipped` global → auf `.fcard.flipped, .card.flipped` eingeschränkt.
5. **Eigener Fund der roten Probe:** die erste Probenrunde fiel an `:25`, nicht an der Wende – zwei Tabellen
   tragen den Kindnamen → Link-Filter am Locator.

Der Zähler steht damit **an** der Grenze, nicht darüber (Freigabe 3: „überschreitet er 5"). Ohne Zähler:
ein Kommentar-Satz, B-256 (außerhalb des Diffs).

## Runde 6 — Re-Review / Abnahme

**Rollengang im Browser** (Freigabe 6) gegen `pugling_walk.db` (Wegwerf, danach gelöscht), Server nach dem
letzten Code-Edit gestartet (danach nur noch ein Kommentar in `seenIds.ts` berichtigt – Vite liefert vom
Datenträger, das Laufzeitverhalten ist unverändert).

- **Sohn – signed off, mit benanntem Menschen-Check:** „Ich mach ‚Weg' auf, da steht mein Französisch-Ziel – nix
  passiert, richtig, ist ja noch offen. Papa legt mir eins an, das ich schon geschafft hab, ich geh wieder auf
  ‚Weg': 🏆 ZIEL GESCHAFFT! +25 🪙. Nochmal drauf: nichts mehr, gut, sonst wär's Fake. Und beim nächsten Login
  hab ich wirklich 105 statt 50." Der Overlay-Text ist per DOM-Mitschnitt belegt, auf keinem Screenshot (Banner
  blendet nach 1,5 s aus, die Screenshot-Latenz ist länger). Übung: „la ville" → „Umdrehen": die Karte dreht
  sich (bei 110 ms angehalten: schräg, Lösung noch verborgen, Knöpfe schon da), danach „→ die Stadt".
  **Zurückgestellt:** ein sechstes Ziel jenseits der ersten fünf wird nie gefeiert (gesehen, Risiko der
  Schätzung). **Menschen-Check:** Klang, Gefühl und Tempo von Feier und Wende am echten Gerät (`wartet_auf` an
  B-36 und B-35).
- **Vater – Regressionszeuge:** kein Supervisor-Pfad im Diff; Objective-API im Rollengang benutzt, Suite grün.
- **Creator – Regressionszeuge:** Diff berührt nur die Hilfe des Agenten (B-256); `pugling-creator help` neu
  gelesen, Zeilen 22–25.
- **Nebenfund im Rollengang:** der Tab „Weg" zeigte vor dem Anlegen eines Plans die rohe englische Meldung
  „Study plan not found." → **B-254** (`idee`, `Defekt`, außerhalb des Diffs).

## Retrospektive Sprint 6

**Nachschau:** Sprint 5 des zweiten Laufs (B-243, B-244, B-237, B-246; Commits `4954bd2`, `67ccf42`) – frischer
Agent, read-only über `git show HEAD:`, fünf benannte Prüfpunkte. **B-243:** kein Defekt (`IsAdmin = true` nur
`TestApi.cs:57`; `MakeAdmin` vor dem Login, Rolle als Claim `AuthController.cs:60`); ein falscher Testname in
Entscheidung 3, berichtigt. **Drei Entgleitungen** → **B-256** (`entgangen_bei: B-237, B-244, B-246`), im
Sprint behoben: die Hilfe versprach die Reihe des *gewählten* Profils (`AgentCommands.cs:318` lädt nur ein
genanntes); B-244s „im Ausgangszustand grün (0 = 0)" ist gemessen falsch (1 rot / 4 an `createdAt`); B-246s
Sweep-Zahl 4 statt 5. `nachgeschaut` auf allen vier gesetzt. Index: **Nachgeschaut 156 von 172**.

**Prämisse geprüft:** die Hilfe-Aussage selbst gegen `BuildRequestAsync` (`:312-320`) und
`BriefingBuilder.ResolveProfileAsync` (`:128-147`) gelesen; die `createdAt`-Aussage selbst gemessen, nicht vom
Prüfer übernommen.

**Was die eigenen Tore durchgelassen haben:**

- **Eine Begründung, die eine Zahl voraussagt, ohne sie zu messen.** B-244 sagte „grün (0 = 0)" voraus und fuhr
  dann eine Mutante – die Mutante war rot, aber aus einem anderen Grund als behauptet. Die Mutante bewies „der
  Test kann fallen", nicht „er fällt *an diesem Feld*". Dieselbe Lehre wie bei B-202 („nicht ob der Test fällt, sondern wo"): in
  diesem Sprint passierte es mir selbst bei B-35 (rot an `:25`) und wurde nur gefangen, weil die Zeile im
  Ausgabe-Parser stand.
- **Eine Hilfe-Zeile, die eine Standard-Kaskade beschreibt, ohne den Fall „nicht angegeben" zu lesen.** Der
  Reviewer von Sprint 5 las `ResolveLocationAsync` Zeile für Zeile – aber nicht `BuildRequestAsync`, das
  entscheidet, *ob* ein Profil hineingeht.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Kein Tor, ein Satz in der Vorgabe der roten Probe (Auflage 5):** jede rote Probe nennt neben der Zahl auch
**die Zeile bzw. die Meldung**, an der sie fällt, und der Verlauf vergleicht sie mit der erwarteten. Für
Playwright die `> NN |`-Zeile, für xUnit die `Expected/Actual`-Zeile. Kosten: ein Halbsatz in `nachtlauf.md`
Auflage 5 (keine resident geladene Datei); die Mutanten-Skripte dieses Laufs liefern das schon mit. **Warum kein
Tor:** welche Zeile die richtige ist, weiß nur, wer die Probe entworfen hat – das lässt sich nicht maschinell
prüfen, nur sichtbar machen.

**Bewusst ungedeckt:** dass Hand-Antworten `createdAt` mit `Z` serialisieren und Projektionen ohne – eine echte
Uneinheitlichkeit im Vertrag (`Kind=Utc` gegen `Kind=Unspecified`), aber kein Fund dieses Sprints und ohne
Entscheidung des Nutzers nicht anzufassen. Vermerkt in B-256.

## Sprint 7 — Ziel & Umfang (NP-2)

**Sprint-Ziel (aus dem Creator-Sitz):** *Ich kann jede Übung vor dem Zuweisen sinnvoll ausprobieren – auch einen
Aufsatz –, eine Birkenbihl-Übung bekommt nur Sprachcodes, die der Vokabelspeicher kennt, und der Agent zieht
im Fördermodus nur Wörter der Sprache meiner Übung heran.*

**Umfang:** B-15 (`Wunsch`, `geschaetzt`), B-17 (`Frage`, `geschaetzt`, Entscheidungen in G-0 vom Nutzer
bestätigt), B-21 (`Wunsch`, `geschaetzt`). Nachprüfungs-Zeilen gelesen: Zeilen verrutscht (B-15), der neue
Test hängt neben `exerciseConfig.test.ts` (B-17), der Guard deckt auch `plan` (B-21).

**Entwickler-Brief:** Backend zuerst. **Quelle der Wahrheit:** `IExerciseType` (ein Typ = eine Klasse) für die
Ansicht, `Vocabulary.SourceLanguage/TargetLanguage` für den Sprachfilter, `LANGUAGES` für die Codes im
Frontend. **Guards:** `CheckAsync` bleibt bei `no_checkable_content`; der Filter läuft vor dem Gruppieren;
`--use-weak` ohne Kind bricht ab. **Vertrag:** additiv (`gradable`, zwei Query-Parameter). **Migration:** keine.

## Iteration 7 — umgesetzt, nicht abgenommen

Was gebaut ist und welche Proben gemessen sind, steht je Story im `## Verlauf` (B-15, B-17, B-21). Zusammen:
Backend **885/885** (879 + 6 neue Fälle), Vitest **406/406**, `tsc -b` sauber, `uebungstypen.spec.ts`
**1 passed**; zwölf Mutanten, jede an der erwarteten Zeile rot.

### Review-Funde (Zähler: 6 – Grenze überschritten)

`pugling-reviewer` und `frontend-reviewer` liefen parallel und fanden **keinen Blocker**, aber diese Funde,
deren Behebung Code oder Tests ändert:

1. B-17: `contentProblem` prüft die Sprachen nicht – „bitte wählen" lässt sich speichern (Code + Test).
2. B-15: die Meldung zu `no_checkable_content` nennt noch den Aufsatz, der dort nie mehr ankommt (Code).
3. B-15: der Satz unter der Ansicht nennt den Typ, obwohl sie typneutral ist (Code).
4. B-17: Testfall 2 prüft nur eines der beiden Felder (Test).
5. B-21: der Profil-Zweig der Sprachkaskade ist ungetestet (Test).
6. B-21: das Entfernen des `targetLang`-Filters bliebe grün (Test).

Dazu ohne Zähler bzw. als Nit: Whitespace-Prüfung im Filter, Hilfe/README zur Pflicht `--child`, `exam` als
`InlineData`, zwei Test-Kommentare ohne „was macht ihn rot", der Platzhalter verschweigt den Altwert, und ein
vorbestehender Fund (`--general --use-weak` zieht die Wörter trotzdem in den Pflicht-Wortschatz).

## Warum der Lauf hier endet

**Freigabe 3:** der Zähler je Sprint steht bei **6**, also über 5 – das beendet den **gesamten** Lauf sofort,
nicht nur diesen Sprint. Gezählt ist nach der Regel des Auftrags (die Behebung änderte Code oder Tests), nicht
nach Gewicht. Ehrlich dazu: es war **keine** Endlosschleife im Sinne der Begründung – alle sechs kamen aus
**einer** Review-Runde zweier paralleler Reviewer, keiner davon ist ein Blocker, und vier sind Testlücken.
Die Regel unterscheidet das nicht, und der Lauf hält sich an die Regel statt an seine eigene Einschätzung.

Folgen, ausdrücklich:

- **Nichts davon ist behoben.** B-15, B-17, B-21 stehen auf `in-arbeit` mit `wartet_auf`, die offenen Funde
  stehen je Story im Verlauf.
- **Sprint 7 ist nicht geschlossen:** kein Rollengang, keine Retrospektive, keine Nachschau zu Sprint 6. Die
  Nachschau zu Sprint 6 (B-36, B-35, B-256, Commit `39cf33b`) ist die erste Pflichthandlung des nächsten Laufs.
- **NP-3 (B-28, B-245, B-230) ist nicht angefangen.**
- **Der Stand ist trotzdem committet**, damit der Baum sauber ist und nichts verloren geht: Suite grün, Vertrag
  additiv, Commit-Text sagt „angehalten, nicht abgenommen". Wer ihn nicht will, nimmt **einen** Commit zurück.

### Vorschlag zur Regel — nicht gelandet (Freigabe 3)

Der Zähler kann eine **parallele** Review-Runde nicht von einer **Schleife** unterscheiden: zwei Reviewer über
drei Stories liefern in einem Schritt mehr als fünf Funde, ohne dass irgendetwas nicht konvergiert. Vorschlag:
gezählt wird je **Behebungsrunde** (ein Fix-Durchgang nach einem Review) statt je Fund, und die Grenze greift,
wenn die **dritte** Runde desselben Sprints noch Funde mit Codewirkung liefert. Kosten: die Grenze wird weicher
und braucht eine Definition von „Runde" im Verlauf. Alternative ohne Regeländerung: in einem Sprint mit drei
Stories in zwei Bereichen die Reviewer **nacheinander** fahren und je Bereich beheben – das ändert die Zählung
nicht, macht aber sichtbar, wo die Funde liegen. Entscheidung beim Nutzer.

## Für den Morgen (dritter Lauf)

1. **Ein Commit zur Entscheidung:** Sprint 7 ist als angehaltener Stand committet (siehe oben) – behalten und
   die sechs Funde nacharbeiten lassen, oder zurücknehmen.
2. **Menschen-Checks:** die Feier auf „Weg" und die Kartenwende einmal am echten Gerät ansehen und hören
   (`wartet_auf` an B-36/B-35).
3. **Retro-Vorschläge:** Sprint 6 (rote Probe nennt Zeile/Meldung, nicht nur die Zahl) und die Zähler-Frage aus
   Sprint 7. Beide nicht gelandet.
4. **Entscheidungen, die der Lauf nicht treffen durfte:** soll das zum Kind gewählte Profil die Reihe des
   KI-Creators setzen (B-256)? Eigene Story für `createdAt` mit/ohne `Z` (B-256)? Eigene Story für
   `--general --use-weak` (B-21)? `Translation`/`ClozeTexts` auf `LanguageSelect` umstellen (B-17)?
5. **Neue Ideen:** B-254 (Weg zeigt „Study plan not found."), B-255 (Kinderzeile per Namen in E2E-Specs).
6. `git log --oneline 896d1b2..`: vier Commits dieses Laufs; **nichts ist gepusht.**

## Vierter Lauf desselben Tages (ab 22:02)

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9 plus *„Fahre die Pakete NP-4, NP-12
aus [der Backlog-Sichtung](pm-sitzung-2026-10-23-backlog-sichtung.md), in dieser Reihenfolge."* Ausgangslage: HEAD
`3cebfd3`, `git status` sauber. Ausdrücklich ausgenommen: B-15, B-17, B-21 (Sprint 7, angehalten) und NP-3. Die
Chrome-Extension ist verbunden (ein Browser, lokal) – Freigabe 6. Sprints laufen weiter durchnummeriert (8, 9).
Die Uhr überschreitet im Lauf lokal Mitternacht; die Verlauf-Daten stammen aus `date -u` und bleiben 2026-10-23.

**Nachschau zu Sprint 6 (erste Pflichthandlung, vor dem ersten Bau):** frischer Prüfer, read-only über
`39cf33b`/`fe94ab0`, fünf benannte Prüfpunkte (je Story im `## Verlauf`). **B-35, B-256:** kein Befund, mit Prüfpunkten
(Wende-Klasse gegen Entscheidungen und Kartenwechsel, Reduce-Motion-Gate; Hilfe gegen `BuildRequestAsync`,
Kommentar gegen `CreatedAt`). **B-36:** eine Entgleitung → **B-257** (`entgangen_bei: B-36`): der Erst-Stand der
gesehenen Ziele entsteht nur aus `take: 5`, ein schon erreichtes sechstes Ziel wird später, wenn es nach vorn
rutscht, verspätet gefeiert. Dazu eine Testlücke (Währungsweiche Münze/Gem), in B-257 vermerkt. Nach der Auslegung
von Limit 2 aus dem dritten Lauf beendet ein Fund im Sprint eines früheren Laufs diesen nicht. `nachgeschaut` auf
allen drei gesetzt.

## Sprint 8 — Ziel & Umfang (NP-4)

**Sprint-Ziel (aus dem Creator-Sitz, als Leser des Vertrags):** *Wer die API liest, findet jede Beschreibung als
Fließtext – ohne Einrückung, ohne rohe Tags, mit Absätzen –, dasselbe Eigentums-Flag überall unter einem Namen, und
nach dem Umbenennen eines Fachs überall den neuen Namen.*

**Umfang:** B-147 (`Defekt`, `ausformuliert`), B-138 (`Aufräumen`, `ausformuliert`), B-156 (`Aufräumen`,
`ausformuliert`), B-145 (`Defekt`, `idee`) – alle vier autonom grillbar (Freigabe 1); Nachprüfungs-Zeilen gelesen und
am Code bestätigt. Reihenfolge B-147 vor B-138 (der Normalisierer entscheidet, was von Hand bleibt).

**Entwickler-Brief:** Backend zuerst. **Quelle der Wahrheit:** das erzeugte `docs/openapi/v1.json` (gemessen am
geparsten JSON, nicht per `grep` – `<` steht dort als `<`). **Guards:** der Normalisierer schreibt bei `$ref`
nur die eigene Beschreibung; das Umbenennen gleicht über die Id ab, nie über den Namen. **Vertrag:** ein Bruch
(B-156, `isMine` → `isOwn`), Frontend und Fixtures ziehen im selben Commit mit. **Migration:** keine.
**Testweg:** Dokument-Test, Einheitstests mit Mutanten, Integrationstest, Browser-Rollengang.

## Iteration 8 — umgesetzt

- **B-147/B-138:** `backend/Pugling.Api/OpenApi/OpenApiProseNormalizer.cs` (neu), als letzter Dokument-Transformer
  in `Program.cs`. Zwei Messungen haben den Plan verändert: Swagger UI zeigt eine Summary als **Klartext** (`<b>all</b>`
  steht dort wörtlich), und der Generator behandelt `<para>` je nach Deklaration verschieden – in Typen auf oberster
  Ebene klebt er Absätze ohne Trenner zusammen und verwirft `<b>`/`<c>`. Darum zusätzlich ein mechanischer Tausch:
  alle eigenständigen `<para>`/`</para>`-Zeilen in `<summary>`-Blöcken gegen eine leere `///`-Zeile (204 Zeilen,
  Skript; nach dem Review vier Inline-Fälle von Hand). Fundstellen (entfernte Zeilen) unter `Pugling.Api/Controllers/`:
  AuthController.cs (8), Creator/ExerciseBrief.cs (2), Creator/ExerciseCatalogController.cs (6),
  Creator/ExerciseCategoriesController.cs (8), Creator/ExerciseControllerBase.cs (2), Creator/ExercisePreviewController.cs (2),
  Creator/InterestTagsController.cs (6), Creator/MediaAssetsController.cs (4), Creator/MediaUploadForm.cs (2),
  Creator/PublishersController.cs (12), Creator/SeriesUnitsController.cs (2), Creator/SubjectsController.cs (6),
  Creator/TagsController.cs (6), Creator/TeacherAccountsController.cs (4), RemarksController.cs (20),
  Student/ChildMediaPicksController.cs (2), Student/InterestOptionsController.cs (2), Student/MyInterestsController.cs (4),
  Student/PositionPracticeController.cs (10), Student/PositionTestsController.cs (8), Supervisor/AdultsController.cs (4),
  Supervisor/ChildInterestsController.cs (2), Supervisor/PositionReportController.cs (2); unter `Pugling.Contracts/`:
  Auth/AuthDtos.cs (2), Common/MediaBaseTypes.cs (2), Common/PlanPositionBaseTypes.cs (6), Common/RemarkBaseTypes.cs (2),
  Creator/CreatorProfileDtos.cs (6), Creator/ExerciseAuthoringDtos.cs (6), Creator/ExerciseBrief.cs (2),
  Creator/ExerciseCatalogDtos.cs (6), Creator/InterestTagDtos.cs (2), Creator/PublisherDtos.cs (2),
  Shared/ProgressDtos.cs (2), Shared/RemarkDtos.cs (2), Student/PracticeDtos.cs (18), Student/TestDtos.cs (16),
  Supervisor/ProfileDtos.cs (4), Supervisor/StudyPlanDtos.cs (2). Neue Tests: `OpenApiProseNormalizerTests` (8),
  `ContractDocumentTests.Vertragsdokument_TraegtKeinQuelltextFormat`.
- **B-156:** `SubjectResponse.IsOwn`; Leser `CatalogAdmin.tsx:244`, Fixtures in vier Testdateien, Wiki,
  `docs/api-examples/catalog.md` und der Beispielkatalog von der Suite neu geschrieben.
- **B-145:** `SubjectNaming.PropagateRenameAsync`, aufgerufen in `SubjectsController.Update` in einer Transaktion.
  Abgetrennt: **B-258** (uneinheitlicher Fehlercode für eine unbekannte `SubjectId`).

**Verifikation:**

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Dokument, Einrückung (Ausgangsstand) | 31 Texte | **31** rot (`ContractDocumentTests`) |
| Dokument, Tags (Übersetzung aus) | ≥ 71 Texte | **104** rot an `:217` (Mehrung erklärt in B-138) |
| Dokument, Einrückung (Endstand, Transformer aus) | rot | **7** rot an `:213` – trägt weiter |
| neun Normalisierer-Mutanten | je der benannte Fall | ja; einer war zuerst **grün** (Test ohne registrierten Workspace – leere Zusicherung), behoben |
| sieben B-145-Mutanten | je die benannte Zeile | `:120`–`:124`, `:130` |
| B-156 Draht-Name zurück / Frontend-Leser zurück | 5 bzw. 1 rot | **5** (`:89, :109, :145, :186, :643`) / **1** (`CatalogAdmin.test.tsx:35`) |
| `v1.json` nachher | 0 eingerückt, 0 Tags | 0 / 0; Absatzumbrüche 41 → **152**; Texte 2487 → 2487 |
| Backend-Suite | grün | **895/895** |
| Vitest / `tsc -b` | grün | **406/406** / sauber |

### Review-Funde (Zähler: 3 von 5)

`pugling-reviewer`, danach `frontend-reviewer` – **nacheinander**, nicht parallel (Lehre aus Sprint 7). Gezählt:
(1) vier Inline-`<para>` verloren weiter Absatz und Backticks; (2) Betonung über einen Absatz; (3) ungetestete Aussage
„fremde Eigentümer werden mit umgeschrieben". Ohne Zähler: ein Kommentar, eine Messung (die Einrückungs-Zusicherung
trägt), vorbestehende Generator-Eigenheiten → **B-259**, ein Doku-Nit in `frontend/CLAUDE.md:112` (belassen, Budget).

## Runde 8 — Re-Review / Abnahme

**Rollengang im Browser** (Freigabe 6) gegen `pugling_walk4.db` (danach gelöscht), API und Vite nach dem letzten Edit
gestartet.

- **Creator – signed off:** „In Swagger steht beim Login jetzt ‚all roles' statt ‚<b>all</b>', und das Löschen eines
  Fachs liest sich in Absätzen. Ich leg ‚Franz Walk' an, kann es umbenennen und löschen, die mitgelieferten Fächer
  nicht – wie vorher, nur heißt das Feld jetzt wie überall. Ich benenne es um, und die Reihe auf ‚Lehrwerke' heißt
  sofort ‚Französisch Walk'." **Einschränkung, benannt:** nach einem Wiederverbinden der Extension kamen Klicks und
  Tastatur nicht an; die Eingaben liefen über die echten Handler der Seite (Value-Setter + Ereignis, `click()`), nicht
  über `computer.type`. Wie sich die Markdown-Beschreibungen in Scalar lesen, ist nicht angesehen.
- **Vater – signed off (Regressionsweg und Gewinn):** das Lehrbuch seines Kindes trägt nach dem Umbenennen den neuen
  Fachnamen (API-Lesung im Rollengang); Katalog- und Lehrwerk-Seite unverändert bedienbar.
- **Sohn – Regressionszeuge:** Startseite geladen („Noch keine Mission", Wegwerf-DB); kein Student-Pfad im Diff außer
  Kommentaren, Suite grün.

## Retrospektive Sprint 8

**Nachschau:** Sprint 6 (B-36, B-35, B-256) – siehe oben unter „Vierter Lauf"; eine Entgleitung → B-257. Index:
**Nachgeschaut 159 von 176**.

**Prämisse geprüft:** die Aussage „das Tor sieht die Inline-`<para>` nicht" selbst nachgemessen (die vier Texte
hatten weder Einrückung noch Tag, nur fehlende Backticks); die Aussage „die Einrückungs-Zusicherung trägt am Endstand
nicht mehr" des Reviewers war eine Vermutung – gemessen trägt sie (7 Texte).

**Was die eigenen Tore durchgelassen haben:**

- **Ein Tor, das Symptome zählt, nicht die Ursache.** Die Dokument-Zusicherung fragt „eingerückt?" und „rohes Tag?" –
  ein `<para>`, das Absatz und Backticks still verschluckt, erzeugt keins von beiden. Gefunden hat es der Reviewer.
- **Eine Zusicherung, die nicht fallen konnte** – diesmal selbst gefangen: der Referenz-Fall prüfte ohne registrierten
  Workspace, die Referenz löste nichts auf. Die Mutante je Fall (Auflage 5) hat es gezeigt, bevor ein Reviewer es sah.

### Vorgeschlagener Mechanismus — nicht gelandet (Freigabe 3)

**Ein Tor in `ConventionGuardTests`:** kein `<para>` in den XML-Docs von `Pugling.Contracts` und
`Pugling.Api/Controllers` (Absätze per leerer `///`-Zeile). Es ist die einzige Stelle, an der die Ursache sichtbar ist;
das Dokument sieht nur die Folgen, und nicht alle. Heute grün (0 Treffer nach diesem Sprint), Ausnahmeliste leer.
Kosten: ein Wächter mehr, und Entwickler, die `<para>` aus Gewohnheit schreiben, bekommen einen roten Build mit der
Begründung. **Bewusst ungedeckt:** die Leerzeichen, die der Generator zwischen zwei Elementen verschluckt (B-259).
