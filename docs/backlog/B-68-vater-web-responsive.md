---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [Vater-Web responsive, Responsive Polish]
status: ausformuliert
prio: P3
art: Wunsch
quelle: remark #1
---

# B-68 · Das Vater-Web hat keinen einzigen eigenen Breakpoint

## User Story

Als Vater möchte ich das Vater-Web auch auf einem schmalen Bildschirm bedienen können, weil ich den
Lernstand meines Kindes selten am Schreibtisch nachsehe.

## Ist-Stand am Code

[frontend/src/index.css](../../frontend/src/index.css) ist die **einzige** Stylesheet-Datei des Projekts.
Sie enthält drei `@media`-Regeln:

- `:234` und `:311` — `prefers-reduced-motion`, kein Layout.
- `:399` — die einzige `max-width`-Query (`760px`), und sie gilt ausschließlich der **Produktseite**:
  betroffen sind `.lp-hero`, `.lp-orb`, `.lp-grid2`, `.lp-grid3`, `.lp-loop` (`:400-403`).

Für das Vater-Web gibt es also **keinen** Breakpoint. Was heute trägt und was nicht:

- `.form-grid` fließt von selbst: `repeat(auto-fit, minmax(180px, 1fr))` (`:259`) — Formulare brechen um.
- `.vater-main` ist `max-width: 1000px` mit `padding: 22px` (`:227`) — schrumpft, aber ohne Anpassung.
- `.table` ist `width: 100%; border-collapse: collapse; font-size: 14px` (`:237`) — **ohne
  Overflow-Container**. Breite Listen laufen über: `/vater/lehrwerke` hat fünf Spalten
  ([VaterLehrwerke.tsx:56](../../frontend/src/vater/VaterLehrwerke.tsx)), andere mehr.
- Die Sohn-Arcade ist davon nicht betroffen: `.sohn-*` und `.center-col` sind auf `max-width: 480px`
  gebaut (`:85,110`), also von vornherein schmal.

## Die echte Lücke

Nicht „es fehlt Feinschliff", sondern konkret: **Tabellen sind die tragende Darstellung des Vater-Webs und
haben keine Antwort auf schmale Schirme.** Alles andere fließt bereits oder ist unkritisch.

Das ist eine Sammel-Story, kein Einzelfix — sie berührt jede Liste. Der Zuschnitt sollte vor dem Bauen
fallen, sonst wird es ein unbegrenztes „Polish".

## Offene Punkte

1. **Was ist die Zielbreite?** Telefon (≈390 px) oder Tablet hochkant (≈768 px)? *Empfehlung: 768 px* — der
   Vater bedient das Web, das Telefon ist die Rolle der Sohn-PWA.
2. **Tabellen: horizontal scrollen oder auf Karten umbrechen?** *Empfehlung: `overflow-x: auto` als
   generische Regel auf `.table`* — eine Zeile CSS für alle Listen, gegenüber einer Karten-Variante je
   Bildschirm. Kosten: Scrollen ist unschöner als Umbrechen.
3. **Gehört die Navigation dazu?** Die Perspektiven-Kopfzeile
   ([navigation.ts](../../frontend/src/vater/navigation.ts)) trägt drei Perspektiven mit ihren Einträgen.
   *Empfehlung: getrennt entscheiden* — sonst wächst die Story unbegrenzt.
4. **Wie wird das geprüft?** *Empfehlung: ein Playwright-Lauf mit schmalem Viewport, der auf horizontales
   Überlaufen des `body` prüft* — sonst ist „responsive" eine Geschmacksfrage ohne Abnahme.

## Akzeptanzkriterien

> Entwurf — Punkt 1 und 2 entscheiden über den Zuschnitt.

1. Bei der festgelegten Zielbreite scrollt keine Seite des Vater-Webs horizontal im `body`.
2. Breite Listen bleiben vollständig erreichbar (Scrollen im eigenen Container oder Umbruch).
3. Formulare und Knöpfe bleiben bedienbar; nichts überlappt.
4. Die Sohn-Arcade und die Produktseite bleiben unverändert.
5. Ein E2E-Fall mit schmalem Viewport, der vorher rot war.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #1; Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#g--responsive-polish-vater-web-1).
