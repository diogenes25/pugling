---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/supervisor]
aliases: [Vater-Web responsive, Responsive Polish]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: remark #1
---

# B-68 · Das Vater-Web hat keinen einzigen eigenen Breakpoint

## User Story

Als Vater möchte ich das Vater-Web auch auf einem schmalen Bildschirm bedienen können, weil ich den
Lernstand meines Kindes selten am Schreibtisch nachsehe.

## Ist-Stand am Code

[frontend/src/index.css](../../frontend/src/index.css) ist die **einzige** Stylesheet-Datei des Projekts.
Die Datei ist seit der Ausformulierung (2026-08-02) durch B-63/B-64/B-67 gewachsen (403 → 431 Zeilen);
die Zeilennummern der Belege haben sich verschoben, der Befund selbst bleibt unverändert gültig. Sie
enthält weiterhin genau drei `@media`-Regeln:

- `:260` und `:337` — `prefers-reduced-motion`, kein Layout.
- `:425` — die einzige `max-width`-Query (`760px`), und sie gilt ausschließlich der **Produktseite**:
  betroffen sind `.lp-hero`, `.lp-orb`, `.lp-grid2`, `.lp-grid3`, `.lp-loop` (`:426-429`).

Für das Vater-Web gibt es also weiterhin **keinen** Breakpoint. Was heute trägt und was nicht:

- `.form-grid` fließt von selbst: `repeat(auto-fit, minmax(180px, 1fr))` (`:285`) — Formulare brechen um.
- `.vater-main` ist `max-width: 1000px` mit `padding: 22px`, `display: flex; flex-direction: column`
  (`:253`) — schrumpft, aber ohne Anpassung, und **ohne** `min-width: 0`, also kann kein Kind-Element
  innerhalb dieses Flex-Containers unter seine Inhaltsbreite schrumpfen (Standardverhalten von
  Flex-Items).
- `.table` ist `width: 100%; border-collapse: collapse; font-size: 14px` (`:263`) — **ohne
  Overflow-Container**. `html, body, #root { height: 100%; margin: 0; }` (`:28`) setzt kein
  `overflow-x`, ein zu breites Kind läuft also direkt in den `body` durch.
- Die Fläche ist **größer als beim Ausformulieren angenommen**: `.table` wird heute in **21** von 38
  `.tsx`-Dateien unter [frontend/src/vater/](../../frontend/src/vater/) verwendet (Belege u. a.
  [VaterLehrwerke.tsx:55-56](../../frontend/src/vater/VaterLehrwerke.tsx) mit fünf Spalten,
  [VaterFachlehrer.tsx:55-56](../../frontend/src/vater/VaterFachlehrer.tsx) mit vier — beide erst nach
  dem Ausformulieren durch B-63/B-67 entstanden). Der Befund „Tabellen sind die tragende Darstellung"
  hat sich also **verstärkt**, nicht nur bestätigt.
- Kein Zeilenmuster (Wrapper-`<div>` mit `overflow-x`) existiert bereits irgendwo im Frontend — geprüft
  per Volltextsuche nach `overflow-x`/`table-wrap`/`table-scroll`, ohne Treffer.
- Es existiert **kein** Playwright-Spec mit `setViewportSize` — geprüft per Volltextsuche über
  [frontend/e2e/](../../frontend/e2e/), ohne Treffer. Akzeptanzkriterium 5 ist also tatsächlich neu, nicht
  nur behauptet.
- Die Sohn-Arcade ist davon nicht betroffen: `.sohn-*` und `.center-col` sind auf `max-width: 480px`
  gebaut (`:85,110`), also von vornherein schmal.

## Die echte Lücke

Nicht „es fehlt Feinschliff", sondern konkret: **Tabellen sind die tragende Darstellung des Vater-Webs und
haben keine Antwort auf schmale Schirme.** Alles andere fließt bereits oder ist unkritisch.

Das ist eine Sammel-Story, kein Einzelfix — sie berührt jede Liste. Der Zuschnitt sollte vor dem Bauen
fallen, sonst wird es ein unbegrenztes „Polish".

## Offene Punkte

1. ~~Was ist die Zielbreite?~~ → siehe Entscheidung 1.
2. ~~Tabellen: horizontal scrollen oder auf Karten umbrechen?~~ → siehe Entscheidung 2.
3. ~~Gehört die Navigation dazu?~~ → siehe Entscheidung 3.
4. ~~Wie wird das geprüft?~~ → siehe Entscheidung 4.

## Entscheidungen

1. **Zielbreite: 768 px (Tablet hochkant).** Begründung: Der Vater bedient das Web typischerweise am
   Tablet oder Laptop; das Telefon ist die eigenständige Rolle der Sohn-PWA (`.sohn-*`,
   `max-width: 480px`), nicht ein zweiter Zielrahmen fürs Vater-Web. Kosten: Ein Vater, der das Vater-Web
   trotzdem auf ≈390 px öffnet, bekommt keine geprüfte Darstellung — nur was ohnehin fließt (Formulare,
   Knöpfe) bleibt bedienbar, Tabellen bleiben scrollbar aber eng. Das ist bewusst akzeptiert, weil ein
   echter Telefon-Breakpoint eine zweite Zielbreite mit eigener Prüfung wäre und die Story sonst von M auf
   L wüchse.
2. **Scroll-Mechanik: `overflow-x: auto` auf dem gemeinsamen Seiten-Container `.vater-main`, nicht als
   Wrapper um jede einzelne Tabelle.** Begründung: `.table` ist ein nacktes `<table>`-Element ohne
   umschließendes `<div>`; `overflow-x` direkt am `<table>` ist browserübergreifend unzuverlässig
   (Tabellen wachsen im Zweifel über ihre Zeilen hinaus, statt zu scrollen). Ein zuverlässiger
   Scroll-Container bräuchte ein Wrapper-Element **je Fundstelle** — bei 21 betroffenen Dateien ein
   mechanischer, aber breiter Umbau. Die Alternative trifft stattdessen den einen Container, den jede
   Vater-Seite ohnehin durchläuft: `.vater-main` bekommt `overflow-x: auto` **und** `min-width: 0` (damit
   ein Flex-Kind unter seine Inhaltsbreite schrumpfen darf — sonst wandert der Überlauf einfach eine Ebene
   höher). Zusätzlich `overflow-x: hidden` auf `html, body` als Netz, damit ein noch nicht bedachter Fall
   nie im `body` landet, sondern sichtbar (weil abgeschnitten) auffällt. Kosten: Bei einer breiten Tabelle
   scrollt die **ganze Seite** (inklusive Überschrift und Formular darüber/darunter) horizontal mit, nicht
   nur der Tabellenstreifen — unschöner als ein Karten-Layout, aber ohne Änderung an den 21 Fundstellen.
   Diese Kosten sind niedriger als in der ursprünglichen Empfehlung angenommen: **die Lösung skaliert
   nicht mit der Seitenzahl** — dieselbe eine CSS-Regel deckt alle 21 Fundstellen ab, auch künftige. Genau
   deshalb entfällt eine Scope-Reduktion auf „die wichtigsten Seiten": Das Verkleinern des Scopes brächte
   hier keine Kostenersparnis, weil die Kosten schon jetzt nicht pro Seite anfallen — nur die *Prüfung*
   (Punkt 4) bleibt stichprobenhaft.
3. **Navigation bleibt außerhalb des Scopes.** Begründung: `.vater-nav` trägt bereits `flex-wrap: wrap`
   (`:247`), der `.perspective-switch` hat mit drei kurzen Einträgen bei 768 px reichlich Platz. Eine
   Sichtprüfung während des Angriffsplans (Schritt 3) bestätigt das oder widerlegt es empirisch — im
   zweiten Fall wird das als **eigene** Idee erfasst statt den Scope dieser Story rückwirkend zu erweitern.
   Kosten: keine, solange die Sichtprüfung grün bleibt.
4. **Prüfweg: ein neuer Playwright-Spec `frontend/e2e/responsive-vater.spec.ts`**, der bei 768 px über
   eine Auswahl repräsentativer Routen (`/vater`, `/vater/lehrwerke`, `/vater/fachlehrer`, `/vater/plaene`,
   eine Plan-Detailseite mit `PlanPositions`, `/vater/vocab`) prüft, dass
   `document.documentElement.scrollWidth <= window.innerWidth` gilt (kein horizontaler Überlauf). Kosten:
   ein neuer, laufender E2E-Fall (~6 Routen), der bei jedem künftigen CSS-Umbau grün bleiben muss.

## Akzeptanzkriterien

1. Bei 768 px Viewport-Breite scrollt keine Seite des Vater-Webs horizontal im `body` (geprüft: `html`,
   `body` tragen `overflow-x: hidden` als Netz, der eigentliche Scroll-Container ist `.vater-main`).
2. Breite Listen (alle 21 Fundstellen mit `.table`) bleiben vollständig erreichbar, indem `.vater-main`
   horizontal scrollt.
3. Formulare und Knöpfe bleiben bei 768 px bedienbar; nichts überlappt (Stichprobe: die sechs Routen aus
   Entscheidung 4 plus eine Sichtprüfung der Navigation, siehe Entscheidung 3).
4. Die Sohn-Arcade und die Produktseite bleiben unverändert (keine Änderung an `.sohn-*`, `.center-col`,
   `.lp-*`).
5. Der neue E2E-Fall `responsive-vater.spec.ts` ist vor dem Fix rot (Nachweis: `.table`-Seiten laufen bei
   768 px heute über) und danach grün.

## Schätzung

**Größe: M.** Der CSS-Kern ist klein (eine generische Regel auf `.vater-main` plus ein Sicherheitsnetz auf
`html, body`), aber die Story trägt zusätzlich einen neuen, mehrere Routen umspannenden E2E-Fall und eine
Sichtprüfung über die 21 Fundstellen hinweg — vergleichbar mit dem M-Anker (vokabel-basierter Batch-Pfad im
`MediaSelector`, B-03): der Eingriff selbst ist klein, die Fläche, die er berühren muss, um vertrauenswürdig
zu sein, ist es nicht. Kein Split nötig — die vier offenen Punkte hingen nicht voneinander ab und ließen
sich in einer Runde entscheiden.

- **`wo`**: `frontend` (reine CSS-/E2E-Änderung, kein Backend-Endpunkt betroffen) → Reviewer vor der
  Abnahme: `frontend-reviewer`.
- **`migration`**: nein — keine Schemaänderung.
- **`vertragsbruch`**: nein — `Pugling.Contracts` bleibt unberührt, reines CSS plus ein Test.

**Risiken:**

1. `overflow-x: auto` auf einem Flex-Column-Item wirkt nur zuverlässig mit `min-width: 0` auf demselben
   Element — ohne das wandert der Überlauf eine Ebene höher (zu `.app-vater` bzw. `body`). Mitigation: das
   Sicherheitsnetz `overflow-x: hidden` auf `html, body` (Entscheidung 2) fängt genau diesen Fall sichtbar
   ab (abgeschnittener statt scrollender Inhalt fällt in der Sichtprüfung auf), plus der neue E2E-Fall
   misst `scrollWidth` statt es anzunehmen.
2. Modale Dialoge (`ExerciseEditModal.tsx` u. a.) haben eigene Container-Breiten außerhalb von
   `.vater-main` — bleiben unberührt und werden nicht separat geprüft; falls sie bei 768 px überlaufen,
   ist das ein Folgefund, kein Bestandteil dieser Story.
3. Einzelne der 21 Fundstellen könnten zusätzliche breite Geschwister-Elemente (Badges, feste
   Button-Breiten) tragen, die die generische Regel nicht abdeckt — abgefangen durch die Stichprobe in
   Entscheidung 4, nicht durch eine Vollprüfung aller 21 Seiten.

**Angriffsplan:**

1. `frontend/src/index.css`: `overflow-x: hidden` auf `html, body` (Zeile 28-Regel erweitern oder eigene
   Regel), `overflow-x: auto; min-width: 0;` auf `.vater-main` (`:253`).
2. Optional eine `@media (max-width: 768px)`-Regel für `.vater-main` (kleineres `padding`) und
   `.vater-top`, falls die Sichtprüfung in Schritt 3 das nahelegt — kein Vorgriff ohne Befund.
3. Manuelle Sichtprüfung bei 768 px (Browser-DevTools oder `npm run dev`) über die sechs Routen aus
   Entscheidung 4 plus die Navigation (Entscheidung 3).
4. Neuer Playwright-Spec `frontend/e2e/responsive-vater.spec.ts` (Entscheidung 4) — erst schreiben und rot
   sehen (vor Schritt 1 committen oder lokal gegenprüfen), dann nach dem CSS-Fix grün.
5. `npm run build` (Typecheck) und `npm run test:e2e` vor der Abnahme.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #1; Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#g--responsive-polish-vater-web-1).
- **2026-08-04** — gegrillt: Belege gegen den heutigen Code nachgezogen (Zeilennummern verschoben durch
  B-63/B-64/B-67, Fläche von 1 auf 21 `.tsx`-Fundstellen mit `.table` gewachsen); alle vier offenen Punkte
  in nummerierte Entscheidungen überführt, keine Scope-Reduktion nötig, weil die gewählte Lösung
  (generische Regel auf `.vater-main`) nicht mit der Seitenzahl skaliert (autonom getroffen, Nutzerauftrag).
- **2026-08-04** — geschaetzt: `groesse: M`, `wo: frontend`, `migration: nein`, `vertragsbruch: nein`,
  Risiken und Angriffsplan ergänzt, Testweg auf einen neuen Playwright-Spec
  `responsive-vater.spec.ts` festgelegt; kein XL-Split erforderlich (autonom getroffen, Nutzerauftrag).
